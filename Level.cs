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
    private static readonly Color SpawnMarkerColor = new(88, 190, 118, 165);
    private static readonly Color InactiveExitFillColor = new(115, 72, 72, 55);
    private static readonly Color InactiveExitWireColor = new(170, 82, 82, 255);
    private static readonly Color ActiveExitFillColor = new(60, 220, 210, 75);
    private static readonly Color ActiveExitWireColor = new(100, 255, 220, 255);

    public Vector3 PlayerSpawnPosition { get; } = new(0f, 0f, 18f);
    public Vector3 ExitPosition { get; } = new(0f, 0.95f, -20f);
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
    private readonly List<(Vector3 Position, Vector3 Size)> walls = new();
    private readonly List<Door> doors = new();
    private readonly List<InteractableSwitch> switches = new();
    private readonly List<WorldInteractable> worldObjects = new();
    private readonly List<Vector3> lightFixtures = new();

    public Level()
    {
        BuildShipInterior();
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


    private void BuildShipInterior()
    {
        AddOuterHull();
        AddRoom("Breached Entry Bay", new Vector3(0f, 0f, 17f), new Vector3(12f, 0f, 10f));
        AddRoom("Cargo Storage", new Vector3(0f, 0f, -2.5f), new Vector3(15f, 0f, 12.5f));
        AddRoom("Engineering Control", new Vector3(0f, 0f, -15f), new Vector3(12f, 0f, 10f));
        AddCorridor(new Vector3(0f, 0f, 7.5f), 5f, 9f);
        AddCorridor(new Vector3(0f, 0f, -8.75f), 5f, 5.5f);

        AddBreach(new Vector3(-4.9f, 1.2f, 20.6f), new Vector3(2.2f, 2.4f, 0.45f));
        AddLightFixture(new Vector3(-2.4f, 3.2f, 18.5f), new Vector3(1.1f, 0.18f, 0.55f));
        AddLightFixture(new Vector3(2.4f, 3.2f, 15.5f), new Vector3(1.1f, 0.18f, 0.55f));

        AddCrate(new Vector3(-1.9f, 0.65f, 10.8f), new Vector3(1.5f, 1.3f, 1.5f));
        AddCrate(new Vector3(2f, 0.45f, 8.3f), new Vector3(1.4f, 0.9f, 1.4f));
        AddCover(new Vector3(-3.4f, 0.75f, 2f), new Vector3(1.1f, 1.5f, 3.1f));
        AddCover(new Vector3(3.2f, 0.75f, -4.5f), new Vector3(1.1f, 1.5f, 3.4f));
        AddCrate(new Vector3(-1.2f, 0.55f, -1.8f), new Vector3(1.8f, 1.1f, 1.8f));
        AddCrate(new Vector3(2.3f, 0.55f, 0.8f), new Vector3(1.6f, 1.1f, 1.6f));
        AddCrate(new Vector3(-4.7f, 0.45f, -6.4f), new Vector3(1.5f, 0.9f, 1.5f));

        AddCover(new Vector3(-3.8f, 0.75f, -14.5f), new Vector3(1.2f, 1.5f, 2.8f));
        AddCover(new Vector3(3.8f, 0.75f, -16.8f), new Vector3(1.2f, 1.5f, 2.8f));
        AddCover(new Vector3(0f, 0.55f, -18.1f), new Vector3(2.4f, 1.1f, 1.1f));
    }

    private void AddOuterHull()
    {
        AddWall(new Vector3(0f, WallHeight / 2f, -ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(0f, WallHeight / 2f, ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(-ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));
        AddWall(new Vector3(ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));
    }

    private void AddRoom(string name, Vector3 center, Vector3 size)
    {
        _ = name;
        float minX = center.X - size.X / 2f;
        float maxX = center.X + size.X / 2f;
        AddWall(new Vector3(minX, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, size.Z));
        AddWall(new Vector3(maxX, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, size.Z));
    }

    private void AddCorridor(Vector3 center, float width, float length)
    {
        AddWall(new Vector3(-width / 2f, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, length));
        AddWall(new Vector3(width / 2f, WallHeight / 2f, center.Z), new Vector3(WallThickness, WallHeight, length));
    }

    private void AddCrate(Vector3 position, Vector3 size) => AddCover(position, size);

    private void AddBreach(Vector3 position, Vector3 size)
    {
        coverObjects.Add((position, size));
    }

    private void AddLightFixture(Vector3 position, Vector3 size)
    {
        coverObjects.Add((position, size));
    }

    private void BuildInteractiveObjects()
    {
        doors.Add(new Door("Security Door", new Vector3(0f, 1.45f, 4f), new Vector3(5f, 2.9f, 0.45f), new Color(120, 76, 58, 255), new Color(72, 130, 84, 130)));
        doors.Add(new Door("Engineering Door", new Vector3(0f, 1.45f, -9f), new Vector3(5f, 2.9f, 0.45f), new Color(92, 54, 54, 255), new Color(72, 130, 190, 130)));

        switches.Add(new InteractableSwitch(SwitchType.Lights, new Vector3(-5.2f, 0.65f, 15.2f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.DoorOpen, new Vector3(1.8f, 0.65f, 5.35f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.PoweredDoor, new Vector3(-1.8f, 0.65f, -7.8f), new Vector3(0.8f, 1.1f, 0.45f)));
        switches.Add(new InteractableSwitch(SwitchType.ExitActivation, new Vector3(4.8f, 0.65f, -14.5f), new Vector3(0.8f, 1.1f, 0.45f)));
        AddInitialPickups();

        lightFixtures.AddRange([
            new Vector3(0f, 4.35f, 17f),
            new Vector3(0f, 4.35f, 9f),
            new Vector3(0f, 4.35f, -2f),
            new Vector3(0f, 4.35f, -12f),
            new Vector3(0f, 4.35f, -19f)
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
        AddWorldObject(new HealthPackPickup(new Vector3(-4.8f, 0.25f, -5.6f)));
        AddWorldObject(new HealthPackPickup(new Vector3(4.8f, 0.25f, -13.2f)));
        AddWorldObject(new WeaponPickup(new Vector3(4.7f, 0.35f, -1.6f), Weapon.CreateRifle()));
        AddWorldObject(new WeaponPickup(new Vector3(-4.6f, 0.35f, -3.3f), Weapon.CreateShotgun()));
        AddWorldObject(new WeaponPickup(new Vector3(2.1f, 0.35f, -12.4f), Weapon.CreatePistol()));
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



    public static BoundingBox ToBoundingBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size / 2f;
        return new BoundingBox(center - halfSize, center + halfSize);
    }
}