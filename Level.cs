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

public sealed class InteractableSwitch : IInteractable
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
    public float Radius => MathF.Max(Size.X, MathF.Max(Size.Y, Size.Z)) * 0.5f;
    public string DisplayName => Type switch
    {
        SwitchType.ExitActivation => "extraction switch",
        SwitchType.DoorOpen => "security door switch",
        SwitchType.PoweredDoor => "powered door switch",
        SwitchType.Lights => "light switch",
        _ => "switch"
    };
    public bool IsActive => CanUse;
    public BoundingBox Bounds => Level.ToBoundingBox(Position, Size);

    public string GetPrompt(Player player) => $"Press E to activate {DisplayName}";
    public bool CanInteract(Player player) => CanUse;
    public void Interact(Player player, Level level) => level.ActivateSwitch(this);

    public void Render()
    {
        Color bodyColor = State == SwitchState.Off ? new Color(76, 82, 92, 255) : new Color(48, 120, 74, 255);
        Color buttonColor = State == SwitchState.Off ? Color.Red : Color.Green;
        Raylib.DrawCubeV(Position, Size, bodyColor);
        Raylib.DrawCubeWiresV(Position, Size, Color.Black);
        Raylib.DrawCubeV(Position + new Vector3(0f, 0.15f, -0.27f), new Vector3(0.32f, 0.22f, 0.12f), buttonColor);
    }

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

    public Vector3 PlayerSpawnPosition { get; } = new(0f, 0f, 18f);
    public Vector3 ExitPosition { get; } = new(0f, 0.95f, 20f);
    public Vector3 ExitSize { get; } = new(4f, 2.1f, 2.6f);
    public BoundingBox ExitBox => ToBoundingBox(ExitPosition, ExitSize);
    public IReadOnlyList<InteractableSwitch> Switches => switches;
    public IEnumerable<IInteractable> Interactables => switches.Cast<IInteractable>().Concat(worldObjects.Where(obj => obj.IsActive));
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
    private readonly List<WorldInteractable> worldObjects = new();
    private readonly List<Vector3> lightFixtures = new();

    public Level()
    {
        BuildSpaceHulkInterior();
        BuildInteractiveObjects();
    }

    public Color BackgroundColor => LightsOn ? new Color(16, 18, 24, 255) : new Color(6, 8, 13, 255);

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
        worldObjects.Clear();
        AddInitialPickups();
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
        RenderWorldObjects();
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
        doors.Add(new Door("Security Door", new Vector3(0f, 1.35f, 0.2f), new Vector3(3.1f, 2.7f, 0.45f), new Color(120, 76, 58, 255), new Color(72, 130, 84, 130)));
        doors.Add(new Door("Engineering Door", new Vector3(0f, 1.35f, -11f), new Vector3(3.1f, 2.7f, 0.45f), new Color(92, 54, 54, 255), new Color(72, 130, 190, 130)));

        switches.Add(new InteractableSwitch(SwitchType.Lights, new Vector3(-1.55f, 0.65f, 5.8f), new Vector3(0.55f, 1.1f, 0.35f)));
        switches.Add(new InteractableSwitch(SwitchType.DoorOpen, new Vector3(1.55f, 0.65f, 1.45f), new Vector3(0.55f, 1.1f, 0.35f)));
        switches.Add(new InteractableSwitch(SwitchType.PoweredDoor, new Vector3(4.4f, 0.65f, -8.8f), new Vector3(0.55f, 1.1f, 0.35f)));
        switches.Add(new InteractableSwitch(SwitchType.ExitActivation, new Vector3(-2.8f, 0.65f, -17.8f), new Vector3(0.55f, 1.1f, 0.35f)));
        AddInitialPickups();

        lightFixtures.AddRange([
            new Vector3(0f, 4.35f, 17f), new Vector3(0f, 4.35f, 9f), new Vector3(0f, 4.35f, 2f),
            new Vector3(-5f, 4.35f, -5f), new Vector3(5f, 4.35f, -5f), new Vector3(0f, 4.35f, -16f)
        ]);
    }


    public void DropWeapon(Player player, Weapon? weapon)
    {
        if (weapon is null)
        {
            return;
        }

        Vector3 flatLook = MathUtils.Flatten(player.LookDirection);
        if (flatLook.LengthSquared() < 0.001f) flatLook = Vector3.UnitZ;
        Vector3 preferred = player.Position + Vector3.Normalize(flatLook) * 1.35f + new Vector3(0f, 0.35f, 0f);
        Vector3 clearPosition = FindClearObjectPosition(preferred, new Vector3(1.05f, 0.25f, 0.35f));
        worldObjects.Add(new WeaponPickup(clearPosition, weapon));
    }

    private void AddInitialPickups()
    {
        AddWorldObject(new HealthPackPickup(new Vector3(3.6f, 0.25f, -7.6f)));
        AddWorldObject(new HealthPackPickup(new Vector3(-3.1f, 0.25f, -16.4f)));
        AddWorldObject(new WeaponPickup(new Vector3(-5.4f, 0.35f, -4.8f), Weapon.CreateShotgun()));
        AddWorldObject(new WeaponPickup(new Vector3(2.9f, 0.35f, -15.2f), Weapon.CreateRifle()));
        AddWorldObject(new WeaponPickup(new Vector3(-1.7f, 0.35f, 16.2f), Weapon.CreatePistol()));
    }

    private void AddWorldObject(WorldInteractable worldObject)
    {
        Vector3 clearPosition = FindClearObjectPosition(worldObject.Position, worldObject.Size);
        if (clearPosition != worldObject.Position)
        {
            worldObject = worldObject switch
            {
                HealthPackPickup => new HealthPackPickup(clearPosition),
                WeaponPickup weaponPickup => new WeaponPickup(clearPosition, weaponPickup.Weapon),
                _ => worldObject
            };
        }

        worldObjects.Add(worldObject);
    }

    private Vector3 FindClearObjectPosition(Vector3 preferredPosition, Vector3 size)
    {
        Vector3[] offsets =
        [
            Vector3.Zero, new Vector3(0.8f, 0f, 0f), new Vector3(-0.8f, 0f, 0f),
            new Vector3(0f, 0f, 0.8f), new Vector3(0f, 0f, -0.8f),
            new Vector3(1.1f, 0f, 1.1f), new Vector3(-1.1f, 0f, 1.1f),
            new Vector3(1.1f, 0f, -1.1f), new Vector3(-1.1f, 0f, -1.1f)
        ];

        foreach (Vector3 offset in offsets)
        {
            Vector3 candidate = ClampToArena(preferredPosition + offset, MathF.Max(size.X, size.Z) * 0.5f);
            BoundingBox candidateBox = ToBoundingBox(candidate, size);
            if (CollisionBoxes.Any(box => Raylib.CheckCollisionBoxes(candidateBox, box))) continue;
            if (Interactables.Any(obj => Raylib.CheckCollisionBoxes(candidateBox, obj.Bounds))) continue;
            return candidate;
        }

        return ClampToArena(preferredPosition, MathF.Max(size.X, size.Z) * 0.5f);
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
            interactableSwitch.Render();
        }
    }

    private void RenderWorldObjects()
    {
        foreach (WorldInteractable worldObject in worldObjects)
        {
            worldObject.Render();
        }
    }

    private void RenderLightFixtures()
    {
        foreach (Vector3 position in lightFixtures)
        {
            Raylib.DrawCubeV(position, new Vector3(1.15f, 0.12f, 0.32f), LightsOn ? Color.Gold : Color.DarkGray);
            Raylib.DrawCubeWiresV(position, new Vector3(1.15f, 0.12f, 0.32f), LightsOn ? Color.Yellow : Color.Black);
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

    private void BuildSpaceHulkInterior()
    {
        // Outer hull and room shells. Openings are left in the wall segments for corridors and doors.
        AddRoomWalls(new Vector3(0f, 0f, 17f), new Vector2(6f, 8f), northOpeningWidth: 4.4f, southOpeningWidth: 3f);
        AddCorridor(new Vector3(0f, 0f, 8f), 4f, 12f);
        AddDoorBulkhead(0.2f);
        AddRoomWalls(new Vector3(0f, 0f, -5.5f), new Vector2(14f, 10f), northOpeningWidth: 3f, southOpeningWidth: 3f);
        AddDoorBulkhead(-11f);
        AddCorridor(new Vector3(0f, 0f, -13.5f), 4f, 5f);
        AddRoomWalls(new Vector3(0f, 0f, -17f), new Vector2(8f, 6f), northOpeningWidth: 3f);

        // Raised floor plates, barricades, crates, cable trays, and a damaged breach lip use only boxes.
        AddRoutePlatform(new Vector3(0f, 0.05f, 17f), new Vector3(5.2f, 0.1f, 6.8f));
        AddRoutePlatform(new Vector3(0f, 0.04f, 8f), new Vector3(3.2f, 0.08f, 10.8f));
        AddRoutePlatform(new Vector3(0f, 0.05f, -5.5f), new Vector3(12.5f, 0.1f, 8.5f));
        AddRoutePlatform(new Vector3(0f, 0.05f, -16.7f), new Vector3(6.6f, 0.1f, 4.6f), isFinal: true);

        AddCover(new Vector3(-1.15f, 0.55f, 12.2f), new Vector3(1.4f, 1.1f, 1f));
        AddCover(new Vector3(1.15f, 0.55f, 7.7f), new Vector3(1.4f, 1.1f, 1f));
        AddCover(new Vector3(-1.2f, 0.55f, 3.6f), new Vector3(1.2f, 1.1f, 0.8f));

        AddCover(new Vector3(-4.8f, 0.75f, -4.2f), new Vector3(2.2f, 1.5f, 1.6f));
        AddCover(new Vector3(0.1f, 0.55f, -6.4f), new Vector3(2.4f, 1.1f, 1.2f));
        AddCover(new Vector3(4.4f, 0.75f, -3.9f), new Vector3(2f, 1.5f, 1.6f));
        AddCover(new Vector3(-2.7f, 0.45f, -8.2f), new Vector3(1.8f, 0.9f, 1.2f));
        AddCover(new Vector3(3f, 0.45f, -8.1f), new Vector3(1.8f, 0.9f, 1.2f));

        AddCover(new Vector3(0f, 0.6f, -17.4f), new Vector3(2.2f, 1.2f, 0.9f));
        AddCover(new Vector3(2.8f, 0.4f, -18.4f), new Vector3(0.9f, 0.8f, 1.2f));
    }

    private void AddRoomWalls(Vector3 center, Vector2 size, float northOpeningWidth = 0f, float southOpeningWidth = 0f)
    {
        float halfWidth = size.X / 2f;
        float halfDepth = size.Y / 2f;
        AddWall(new Vector3(center.X - halfWidth, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, size.Y));
        AddWall(new Vector3(center.X + halfWidth, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, size.Y));
        AddSplitWall(center.Z - halfDepth, center.X, size.X, southOpeningWidth);
        AddSplitWall(center.Z + halfDepth, center.X, size.X, northOpeningWidth);
    }

    private void AddCorridor(Vector3 center, float width, float depth)
    {
        float halfWidth = width / 2f;
        AddWall(new Vector3(center.X - halfWidth, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, depth));
        AddWall(new Vector3(center.X + halfWidth, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, depth));
    }

    private void AddDoorBulkhead(float z)
    {
        AddSplitWall(z, 0f, 14f, 3.1f);
        AddWall(new Vector3(-2.05f, 2.95f, z), new Vector3(0.55f, WallHeight - 2.7f, WallThickness));
        AddWall(new Vector3(2.05f, 2.95f, z), new Vector3(0.55f, WallHeight - 2.7f, WallThickness));
        AddWall(new Vector3(0f, 4.05f, z), new Vector3(3.1f, 1.9f, WallThickness));
    }

    private void AddSplitWall(float z, float centerX, float totalWidth, float openingWidth)
    {
        if (openingWidth <= 0f)
        {
            AddWall(new Vector3(centerX, WallHeight / 2f, z), new Vector3(totalWidth, WallHeight, WallThickness));
            return;
        }

        float segmentWidth = (totalWidth - openingWidth) / 2f;
        if (segmentWidth <= 0f) return;
        float offset = openingWidth / 2f + segmentWidth / 2f;
        AddWall(new Vector3(centerX - offset, WallHeight / 2f, z), new Vector3(segmentWidth, WallHeight, WallThickness));
        AddWall(new Vector3(centerX + offset, WallHeight / 2f, z), new Vector3(segmentWidth, WallHeight, WallThickness));
    }

    public static BoundingBox ToBoundingBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size / 2f;
        return new BoundingBox(center - halfSize, center + halfSize);
    }
}