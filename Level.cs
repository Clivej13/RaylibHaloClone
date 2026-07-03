using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed partial class Level
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

    public Vector3 PlayerSpawnPosition { get; }
    public Vector3 PlayerSpawnLookDirection { get; }
    public Vector3 ExitPosition { get; } = new(0f, 0.95f, 16f);
    public Vector3 ExitSize { get; } = new(3f, 2.1f, 3f);
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
    private readonly BoardingPodModule boardingPod;
    private readonly PerimeterCorridorModule perimeterCorridor;

    public Level()
    {
        boardingPod = new BoardingPodModule(new Vector3(-18f, 0f, -18f), ModuleFacing.North);
        perimeterCorridor = new PerimeterCorridorModule(new Vector3(-18f, 0f, -13.25f), ModuleFacing.North, sideDoorCount: 3, hasBreachGap: true);
        PlayerSpawnPosition = boardingPod.SpawnPosition;
        PlayerSpawnLookDirection = boardingPod.SpawnLookDirection;

        AddWall(new Vector3(0f, WallHeight / 2f, -ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(0f, WallHeight / 2f, ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(-ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));
        AddWall(new Vector3(ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));

        BuildPlatformingRoute();
        foreach (BoundingBox box in boardingPod.CollisionBoxes) collisionBoxes.Add(box);
        foreach (BoundingBox box in perimeterCorridor.CollisionBoxes) collisionBoxes.Add(box);
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
            case SwitchType.BoardingPodDoor:
                doors.FirstOrDefault(door => door.Name == "Boarding Pod Door")?.Open();
                break;
            case SwitchType.LinkedDoor:
                if (interactableSwitch.TargetDoorName is not null)
                {
                    doors.FirstOrDefault(door => door.Name == interactableSwitch.TargetDoorName)?.Open();
                }
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
        boardingPod.Render(wallColor, floorColor);
        perimeterCorridor.Render(wallColor, floorColor);

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

    public Vector3 ClampToArena(Vector3 position, float radius)
    {
        return MathUtils.ClampHorizontal(position, -ArenaHalfSize + radius, ArenaHalfSize - radius, -ArenaHalfSize + radius, ArenaHalfSize - radius);
    }

    private void BuildInteractiveObjects()
    {
        doors.Add(new Door("Security Door", new Vector3(-8f, 1.25f, -2f), new Vector3(4f, 2.5f, 0.45f), new Color(120, 76, 58, 255), new Color(72, 130, 84, 130)));
        doors.Add(new Door("Powered Door", new Vector3(8f, 1.25f, -2f), new Vector3(4f, 2.5f, 0.45f), new Color(92, 54, 54, 255), new Color(72, 130, 190, 130)));
        doors.Add(new Door("Boarding Pod Door", boardingPod.DoorPosition, boardingPod.DoorSize, new Color(84, 112, 132, 255), new Color(72, 170, 190, 130)));
        doors.AddRange(perimeterCorridor.Doors);

        switches.Add(new InteractableSwitch(SwitchType.BoardingPodDoor, boardingPod.SwitchPosition, boardingPod.SwitchSize, boardingPod.SwitchFaceDirection));
        switches.AddRange(perimeterCorridor.Switches);
        switches.Add(new InteractableSwitch(SwitchType.Lights, new Vector3(-15f, 0.55f, 8f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.DoorOpen, new Vector3(-11f, 0.55f, -2f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.PoweredDoor, new Vector3(11f, 0.55f, -2f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.ExitActivation, new Vector3(3.5f, 0.55f, 15f), new Vector3(0.8f, 1.1f, 0.45f)));
        AddInitialPickups();

        lightFixtures.AddRange([new Vector3(-10f, 4.6f, -10f), new Vector3(0f, 4.6f, 0f), new Vector3(10f, 4.6f, 10f)]);
    }


    public static BoundingBox ToBoundingBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size / 2f;
        return new BoundingBox(center - halfSize, center + halfSize);
    }
}