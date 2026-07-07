using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class PerimeterCorridorModule
{
    private const float Width = 4.5f;
    private const float Height = 3.2f;
    private const float WallThickness = 0.35f;
    private const float FloorThickness = 0.1f;
    private const float DoorWidth = 2.2f;
    private const float DoorHeight = 2.7f;
    private const float SwitchInset = 0.08f;
    private const float SwitchHeight = 1.15f;
    private const float DefaultSideDoorSpacing = 3f;
    private const float DefaultStartSideDoorMargin = 3f;
    private const float DefaultEndSideDoorMargin = 3f;

    private readonly List<ILevelObject> objects = new();
    private readonly List<Door> doors = new();
    private readonly List<InteractableSwitch> switches = new();

    public PerimeterCorridorModule(
        Vector3 origin,
        ModuleFacing facing,
        int sideDoorCount,
        bool hasBreachGap,
        float sideDoorSpacing = DefaultSideDoorSpacing,
        float startSideDoorMargin = DefaultStartSideDoorMargin,
        float endSideDoorMargin = DefaultEndSideDoorMargin)
    {
        Origin = origin;
        Facing = facing;
        SideDoorCount = Math.Max(0, sideDoorCount);
        HasBreachGap = hasBreachGap;
        SideDoorSpacing = sideDoorSpacing;
        StartSideDoorMargin = startSideDoorMargin;
        EndSideDoorMargin = endSideDoorMargin;
        Length = CalculateLength();
        BuildGeometry();
    }

    public Vector3 Origin { get; }
    public ModuleFacing Facing { get; }
    public int SideDoorCount { get; }
    public bool HasBreachGap { get; }
    public float SideDoorSpacing { get; }
    public float StartSideDoorMargin { get; }
    public float EndSideDoorMargin { get; }
    private float Length { get; }
    public IEnumerable<BoundingBox> CollisionBoxes => objects.OfType<ISolidLevelObject>().Select(obj => obj.CollisionBox);
    public IReadOnlyList<ILevelObject> Objects => objects;
    public IReadOnlyList<Door> Doors => doors;
    public IReadOnlyList<InteractableSwitch> Switches => switches;

    public Vector3 TransformPoint(Vector3 local) => Origin + TransformDirection(local);

    public Vector3 TransformDirection(Vector3 local)
    {
        return Facing switch
        {
            ModuleFacing.North => local,
            ModuleFacing.East => new Vector3(local.Z, local.Y, -local.X),
            ModuleFacing.South => new Vector3(-local.X, local.Y, -local.Z),
            ModuleFacing.West => new Vector3(-local.Z, local.Y, local.X),
            _ => local
        };
    }

    public Vector3 TransformSize(Vector3 localSize)
    {
        return Facing is ModuleFacing.East or ModuleFacing.West
            ? new Vector3(localSize.Z, localSize.Y, localSize.X)
            : localSize;
    }

    private void BuildGeometry()
    {
        AddDetail(new Vector3(0f, -FloorThickness / 2f, 0f), new Vector3(Length, FloorThickness, Width));
        AddSolid(new Vector3(0f, Height + WallThickness / 2f, 0f), new Vector3(Length, WallThickness, Width));
        AddWallWithOpenings(-Width / 2f + WallThickness / 2f, HasBreachGap ? new[] { 0f } : Array.Empty<float>());

        float[] sideDoorCenters = CalculateSideDoorCenters();
        AddWallWithOpenings(Width / 2f - WallThickness / 2f, sideDoorCenters);

        AddEndWall(-Length / 2f + WallThickness / 2f, "West End Door", 1f);
        AddEndWall(Length / 2f - WallThickness / 2f, "East End Door", -1f);

        for (int i = 0; i < sideDoorCenters.Length; i++)
        {
            string name = $"Perimeter Side Door {i + 1}";
            Vector3 doorPosition = TransformPoint(new Vector3(sideDoorCenters[i], DoorHeight / 2f, Width / 2f - WallThickness / 2f));
            Vector3 doorSize = TransformSize(new Vector3(DoorWidth, DoorHeight, WallThickness));
            AddDoorAndSwitch(name, doorPosition, doorSize,
                new Vector3(sideDoorCenters[i] - DoorWidth / 2f - 0.45f, SwitchHeight, Width / 2f - WallThickness - SwitchInset),
                new Vector3(0.35f, 0.8f, 0.65f), -Vector3.UnitZ);
        }
    }

    private float CalculateLength() => SideDoorCount == 0
        ? StartSideDoorMargin + EndSideDoorMargin
        : StartSideDoorMargin + EndSideDoorMargin + SideDoorSpacing * (SideDoorCount - 1);

    private float[] CalculateSideDoorCenters()
    {
        if (SideDoorCount == 0)
        {
            return Array.Empty<float>();
        }

        if (StartSideDoorMargin < DoorWidth / 2f || EndSideDoorMargin < DoorWidth / 2f)
        {
            throw new InvalidOperationException("Perimeter corridor side door margins must leave enough wall length for the side door openings.");
        }

        if (SideDoorCount > 1 && SideDoorSpacing < DoorWidth)
        {
            throw new InvalidOperationException("Perimeter corridor side door spacing must be at least one side door width to avoid overlapping side doors.");
        }

        return Enumerable.Range(0, SideDoorCount)
            .Select(i => -Length / 2f + StartSideDoorMargin + SideDoorSpacing * i)
            .ToArray();
    }

    private void AddWallWithOpenings(float localZ, IReadOnlyList<float> openingCenters)
    {
        float cursor = -Length / 2f;
        foreach (float center in openingCenters.OrderBy(x => x))
        {
            AddWallSegment(cursor, center - DoorWidth / 2f, localZ);
            cursor = center + DoorWidth / 2f;
        }
        AddWallSegment(cursor, Length / 2f, localZ);
    }

    private void AddWallSegment(float startX, float endX, float localZ)
    {
        float segmentLength = endX - startX;
        if (segmentLength <= 0.05f) return;
        AddSolid(new Vector3(startX + segmentLength / 2f, Height / 2f, localZ), new Vector3(segmentLength, Height, WallThickness));
    }

    private void AddEndWall(float localX, string doorName, float switchSide)
    {
        float sideSegmentWidth = (Width - DoorWidth) / 2f;
        AddSolid(new Vector3(localX, Height / 2f, -Width / 2f + sideSegmentWidth / 2f), new Vector3(WallThickness, Height, sideSegmentWidth));
        AddSolid(new Vector3(localX, Height / 2f, Width / 2f - sideSegmentWidth / 2f), new Vector3(WallThickness, Height, sideSegmentWidth));
        Vector3 doorPosition = TransformPoint(new Vector3(localX, DoorHeight / 2f, 0f));
        Vector3 doorSize = TransformSize(new Vector3(WallThickness, DoorHeight, DoorWidth));
        AddDoorAndSwitch(doorName, doorPosition, doorSize,
            new Vector3(localX + switchSide * (WallThickness + SwitchInset), SwitchHeight, -DoorWidth / 2f - 0.45f),
            new Vector3(0.65f, 0.8f, 0.35f), Vector3.UnitZ);
    }

    private void AddDoorAndSwitch(string name, Vector3 doorPosition, Vector3 doorSize, Vector3 localSwitchPosition, Vector3 localSwitchSize, Vector3 localSwitchFaceDirection)
    {
        doors.Add(new Door(name, doorPosition, doorSize, new Color(84, 92, 112, 255), new Color(72, 150, 190, 130)));
        switches.Add(new InteractableSwitch(SwitchType.LinkedDoor, TransformPoint(localSwitchPosition), TransformSize(localSwitchSize), TransformDirection(localSwitchFaceDirection), name));
    }

    private void AddSolid(Vector3 localPosition, Vector3 localSize) => objects.Add(new WallObject(TransformPoint(localPosition), TransformSize(localSize)));
    private void AddDetail(Vector3 localPosition, Vector3 localSize) => objects.Add(new ModuleDetailObject(TransformPoint(localPosition), TransformSize(localSize)));
}

