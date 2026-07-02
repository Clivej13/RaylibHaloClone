using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public enum SwitchType
{
    ExitActivation,
    DoorOpen,
    PoweredDoor,
    Lights
}

public enum SwitchState
{
    Off,
    On,
    Used
}

public sealed class Door
{
    public Door(string name, Vector3 closedPosition, Vector3 size, Color closedColor, Color openColor)
    {
        Name = name;
        ClosedPosition = closedPosition;
        Size = size;
        ClosedColor = closedColor;
        OpenColor = openColor;
    }

    public string Name { get; }
    public Vector3 ClosedPosition { get; }
    public Vector3 Size { get; }
    public Color ClosedColor { get; }
    public Color OpenColor { get; }
    public bool IsOpen { get; private set; }
    public BoundingBox CollisionBox => Level.ToBoundingBox(ClosedPosition, Size);

    public void Open() => IsOpen = true;
    public void Reset() => IsOpen = false;
}

public sealed class InteractableSwitch
{
    public InteractableSwitch(SwitchType type, Vector3 position, Vector3 size)
    {
        Type = type;
        Position = position;
        Size = size;
    }

    public SwitchType Type { get; }
    public Vector3 Position { get; }
    public Vector3 Size { get; }
    public SwitchState State { get; private set; } = SwitchState.Off;
    public bool CanUse => State == SwitchState.Off;

    public void Activate() => State = SwitchState.Used;
    public void Reset() => State = SwitchState.Off;
}

public sealed class Level
{
    public const float ArenaHalfSize = 24f;
    public const float WallHeight = 5f;
    public const float WallThickness = 1f;

    private static readonly Color DarkFloorColor = new(34, 39, 48, 255);
    private static readonly Color DarkWallColor = new(45, 52, 64, 255);
    private static readonly Color DarkCoverColor = new(54, 61, 72, 255);
    private static readonly Color LitFloorColor = new(64, 72, 82, 255);
    private static readonly Color LitWallColor = new(82, 92, 108, 255);
    private static readonly Color LitCoverColor = new(100, 111, 128, 255);
    private static readonly Color RoutePlatformColor = new(88, 132, 190, 255);
    private static readonly Color FinalPlatformColor = new(190, 142, 64, 255);
    private static readonly Color SpawnMarkerColor = new(88, 190, 118, 165);
    private static readonly Color InactiveExitFillColor = new(115, 72, 72, 55);
    private static readonly Color InactiveExitWireColor = new(170, 82, 82, 255);
    private static readonly Color ActiveExitFillColor = new(60, 220, 210, 75);
    private static readonly Color ActiveExitWireColor = new(100, 255, 220, 255);

    private const int PlatformCount = 7;
    private const float CentralPlatformSize = 2.8f;
    private const float HeightStep = 0.32f;
    private const float PreviousFirstPlatformHeight = 1.05f;
    private const float CentralPlatformHeight = PreviousFirstPlatformHeight;
    private const float BasePlatformHeight = PreviousFirstPlatformHeight + HeightStep;
    private const float PlatformSize = 2.4f;
    private const float StartAngleDegrees = -90f;
    private const float MaxReasonableJumpDistance = 5.25f;
    private const float PreviousCenterToFirstPlatformGap = 7.5f - (CentralPlatformSize / 2f) - (PlatformSize / 2f);
    private const float CenterJumpGapScale = 0.75f;
    private const float CenterToFirstPlatformGap = PreviousCenterToFirstPlatformGap * CenterJumpGapScale;
    private const float RingRadius = CenterToFirstPlatformGap + (CentralPlatformSize / 2f) + (PlatformSize / 2f);

    // Center-to-first horizontal spacing stays fixed at the tuned 75% value: 4.9m * 0.75 = 3.675m.
    // Heights are shifted up by one step: the center is the old first-platform height, and the new
    // first ring platform is the old second-platform height. Dropping the ring to 7 platforms increases
    // chord distance: 2 * 6.275 * sin(pi / 7) = ~5.45, so edge gap is ~3.05 after the 2.4m width.
    private const float ConsecutivePlatformGap = 2f * RingRadius * 0.4338837391f - PlatformSize;

    public Vector3 PlayerSpawnPosition { get; } = new(0f, 0f, 8f);
    public Vector3 ExitPosition { get; } = new(0f, 0.95f, 16f);
    public Vector3 ExitSize { get; } = new(3f, 2.1f, 3f);
    public BoundingBox ExitBox => ToBoundingBox(ExitPosition, ExitSize);
    public IReadOnlyList<InteractableSwitch> Switches => switches;
    public IReadOnlyList<Door> Doors => doors;
    public bool ExitSwitchActivated { get; private set; }
    public bool LightsOn { get; private set; }
    public IReadOnlyList<BoundingBox> CollisionBoxes => collisionBoxes.Concat(doors.Where(door => !door.IsOpen).Select(door => door.CollisionBox)).ToList();

    private readonly List<BoundingBox> collisionBoxes = new();
    private readonly List<(Vector3 Position, Vector3 Size)> coverObjects = new();
    private readonly List<(Vector3 Position, Vector3 Size, bool IsFinal)> routePlatforms = new();
    private readonly List<(Vector3 Position, Vector3 Size)> walls = new();
    private readonly List<Door> doors = new();
    private readonly List<InteractableSwitch> switches = new();
    private readonly List<Vector3> lightFixtures = new();

    public Level()
    {
        AddWall(new Vector3(0f, WallHeight / 2f, -ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(0f, WallHeight / 2f, ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(-ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));
        AddWall(new Vector3(ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));

        BuildPlatformingRoute();
        BuildInteractiveObjects();
    }

    public Color BackgroundColor => LightsOn ? new Color(16, 18, 24, 255) : new Color(6, 8, 13, 255);

    public InteractableSwitch? GetUsableSwitchNear(Vector3 playerPosition) =>
        switches.FirstOrDefault(sw => sw.CanUse && Vector3.Distance(MathUtils.Flatten(sw.Position), MathUtils.Flatten(playerPosition)) <= 2.1f);

    public void ActivateSwitch(InteractableSwitch interactableSwitch)
    {
        if (!interactableSwitch.CanUse)
        {
            return;
        }

        interactableSwitch.Activate();
        switch (interactableSwitch.Type)
        {
            case SwitchType.ExitActivation:
                ExitSwitchActivated = true;
                break;
            case SwitchType.DoorOpen:
                doors[0].Open();
                break;
            case SwitchType.PoweredDoor:
                doors[1].Open();
                break;
            case SwitchType.Lights:
                LightsOn = true;
                break;
        }
    }

    public void ResetInteractiveState()
    {
        ExitSwitchActivated = false;
        LightsOn = false;
        foreach (Door door in doors) door.Reset();
        foreach (InteractableSwitch interactableSwitch in switches) interactableSwitch.Reset();
    }

    public void Render(bool exitActive)
    {
        Color floorColor = LightsOn ? LitFloorColor : DarkFloorColor;
        Color wallColor = LightsOn ? LitWallColor : DarkWallColor;
        Color coverColor = LightsOn ? LitCoverColor : DarkCoverColor;

        Raylib.DrawPlane(Vector3.Zero, new Vector2(ArenaHalfSize * 2f, ArenaHalfSize * 2f), floorColor);
        RenderSpawnPoint();
        RenderExitZone(exitActive);

        foreach (var wall in walls)
        {
            Raylib.DrawCubeV(wall.Position, wall.Size, wallColor);
            Raylib.DrawCubeWiresV(wall.Position, wall.Size, Color.DarkGray);
        }

        foreach (var cover in coverObjects)
        {
            Raylib.DrawCubeV(cover.Position, cover.Size, coverColor);
            Raylib.DrawCubeWiresV(cover.Position, cover.Size, Color.Black);
        }

        foreach (var platform in routePlatforms)
        {
            Color color = platform.IsFinal ? FinalPlatformColor : RoutePlatformColor;
            Raylib.DrawCubeV(platform.Position, platform.Size, LightsOn ? color : new Color((byte)(color.R / 2), (byte)(color.G / 2), (byte)(color.B / 2), color.A));
            Raylib.DrawCubeWiresV(platform.Position, platform.Size, Color.Black);
        }

        RenderDoors();
        RenderSwitches();
        RenderLightFixtures();

        Raylib.DrawGrid((int)ArenaHalfSize * 2, 1f);
    }

    private void RenderSpawnPoint()
    {
        Raylib.DrawCylinder(PlayerSpawnPosition + new Vector3(0f, 0.025f, 0f), 0.65f, 0.65f, 0.05f, 24, SpawnMarkerColor);
        Raylib.DrawCylinderWires(PlayerSpawnPosition + new Vector3(0f, 0.035f, 0f), 0.65f, 0.65f, 0.07f, 24, Color.Green);
    }

    private void RenderExitZone(bool active)
    {
        Color fillColor = active ? ActiveExitFillColor : InactiveExitFillColor;
        Color wireColor = active ? ActiveExitWireColor : InactiveExitWireColor;

        Raylib.DrawCubeV(ExitPosition, ExitSize, fillColor);
        Raylib.DrawCubeWiresV(ExitPosition, ExitSize, wireColor);
        Raylib.DrawCubeWiresV(ExitPosition, ExitSize + new Vector3(0.08f), wireColor);
    }

    public Vector3 ClampToArena(Vector3 position, float radius)
    {
        return MathUtils.ClampHorizontal(position, -ArenaHalfSize + radius, ArenaHalfSize - radius, -ArenaHalfSize + radius, ArenaHalfSize - radius);
    }

    private void BuildInteractiveObjects()
    {
        doors.Add(new Door("Security Door", new Vector3(-8f, 1.25f, -2f), new Vector3(4f, 2.5f, 0.45f), new Color(120, 76, 58, 255), new Color(72, 130, 84, 130)));
        doors.Add(new Door("Powered Door", new Vector3(8f, 1.25f, -2f), new Vector3(4f, 2.5f, 0.45f), new Color(92, 54, 54, 255), new Color(72, 130, 190, 130)));

        switches.Add(new InteractableSwitch(SwitchType.Lights, new Vector3(-15f, 0.55f, 8f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.DoorOpen, new Vector3(-11f, 0.55f, -2f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.PoweredDoor, new Vector3(11f, 0.55f, -2f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.ExitActivation, new Vector3(3.5f, 0.55f, 15f), new Vector3(0.8f, 1.1f, 0.45f)));

        lightFixtures.AddRange([new Vector3(-10f, 4.6f, -10f), new Vector3(0f, 4.6f, 0f), new Vector3(10f, 4.6f, 10f)]);
    }

    private void RenderDoors()
    {
        foreach (Door door in doors)
        {
            Vector3 position = door.IsOpen ? door.ClosedPosition + new Vector3(0f, door.Size.Y + 0.25f, 0f) : door.ClosedPosition;
            Raylib.DrawCubeV(position, door.Size, door.IsOpen ? door.OpenColor : door.ClosedColor);
            Raylib.DrawCubeWiresV(position, door.Size, door.IsOpen ? Color.SkyBlue : Color.Black);
        }
    }

    private void RenderSwitches()
    {
        foreach (InteractableSwitch interactableSwitch in switches)
        {
            Color bodyColor = interactableSwitch.State == SwitchState.Off ? new Color(76, 82, 92, 255) : new Color(48, 120, 74, 255);
            Color buttonColor = interactableSwitch.State == SwitchState.Off ? Color.Red : Color.Green;
            Raylib.DrawCubeV(interactableSwitch.Position, interactableSwitch.Size, bodyColor);
            Raylib.DrawCubeWiresV(interactableSwitch.Position, interactableSwitch.Size, Color.Black);
            Raylib.DrawCubeV(interactableSwitch.Position + new Vector3(0f, 0.15f, -0.27f), new Vector3(0.32f, 0.22f, 0.12f), buttonColor);
        }
    }

    private void RenderLightFixtures()
    {
        foreach (Vector3 position in lightFixtures)
        {
            Raylib.DrawSphere(position, 0.22f, LightsOn ? Color.Gold : Color.DarkGray);
            Raylib.DrawSphereWires(position, 0.24f, 8, 8, LightsOn ? Color.Yellow : Color.Black);
        }
    }

    private void AddWall(Vector3 position, Vector3 size)
    {
        walls.Add((position, size));
        collisionBoxes.Add(ToBoundingBox(position, size));
    }

    private void AddCover(Vector3 position, Vector3 size)
    {
        coverObjects.Add((position, size));
        collisionBoxes.Add(ToBoundingBox(position, size));
    }

    private void AddRoutePlatform(Vector3 position, Vector3 size, bool isFinal = false)
    {
        routePlatforms.Add((position, size, isFinal));
        collisionBoxes.Add(ToBoundingBox(position, size));
    }

    private void BuildPlatformingRoute()
    {
        if (ConsecutivePlatformGap > MaxReasonableJumpDistance || CenterToFirstPlatformGap > MaxReasonableJumpDistance)
        {
            throw new InvalidOperationException("Platform route gap exceeds the configured sprint-jump distance.");
        }

        AddRoutePlatform(
            new Vector3(0f, CentralPlatformHeight / 2f, 0f),
            new Vector3(CentralPlatformSize, CentralPlatformHeight, CentralPlatformSize));

        float startAngleRadians = StartAngleDegrees * MathUtils.Deg2Rad;
        for (int i = 0; i < PlatformCount; i++)
        {
            float angle = startAngleRadians + i * MathF.Tau / PlatformCount;
            float height = BasePlatformHeight + i * HeightStep;
            Vector3 position = new(MathF.Cos(angle) * RingRadius, height / 2f, MathF.Sin(angle) * RingRadius);
            Vector3 size = new(PlatformSize, height, PlatformSize);

            AddRoutePlatform(position, size, i == PlatformCount - 1);
        }
    }

    public static BoundingBox ToBoundingBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size / 2f;
        return new BoundingBox(center - halfSize, center + halfSize);
    }
}