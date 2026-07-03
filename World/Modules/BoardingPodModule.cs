using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class BoardingPodModule
{
    private const float Width = 6f;
    private const float Depth = 5f;
    private const float Height = 3.2f;
    private const float WallThickness = 0.35f;
    private const float FloorThickness = 0.1f;
    private const float DoorWidth = 2.2f;
    private const float SwitchInset = 0.08f;

    private readonly List<ILevelObject> objects = new();

    public BoardingPodModule(Vector3 origin, ModuleFacing facing)
    {
        Origin = origin;
        Facing = facing;
        ExitDirection = TransformDirection(Vector3.UnitZ);
        SpawnPosition = TransformPoint(new Vector3(0f, 0f, -1.25f));
        SpawnLookDirection = ExitDirection;
        SwitchFaceDirection = TransformDirection(-Vector3.UnitX);
        SwitchPosition = TransformPoint(new Vector3(Width / 2f - WallThickness - SwitchInset, 1.15f, -0.85f));
        DoorPosition = TransformPoint(new Vector3(0f, 1.4f, Depth / 2f - WallThickness / 2f));
        DoorSize = TransformSize(new Vector3(DoorWidth, 2.8f, WallThickness));
        BuildGeometry();
    }

    public Vector3 Origin { get; }
    public ModuleFacing Facing { get; }
    public Vector3 SpawnPosition { get; }
    public Vector3 SpawnLookDirection { get; }
    public Vector3 ExitDirection { get; }
    public Vector3 SwitchPosition { get; }
    public Vector3 SwitchFaceDirection { get; }
    public Vector3 DoorPosition { get; }
    public Vector3 DoorSize { get; }
    public Vector3 SwitchSize => TransformSize(new Vector3(0.35f, 0.8f, 0.65f));
    public IEnumerable<BoundingBox> CollisionBoxes => objects.OfType<ISolidLevelObject>().Select(obj => obj.CollisionBox);
    public IReadOnlyList<ILevelObject> Objects => objects;

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
        AddDetail(new Vector3(0f, -FloorThickness / 2f, 0f), new Vector3(Width, FloorThickness, Depth));
        AddSolid(new Vector3(0f, Height + WallThickness / 2f, 0f), new Vector3(Width, WallThickness, Depth));
        AddSolid(new Vector3(0f, Height / 2f, -Depth / 2f + WallThickness / 2f), new Vector3(Width, Height, WallThickness));
        AddSolid(new Vector3(-Width / 2f + WallThickness / 2f, Height / 2f, 0f), new Vector3(WallThickness, Height, Depth));
        AddSolid(new Vector3(Width / 2f - WallThickness / 2f, Height / 2f, 0f), new Vector3(WallThickness, Height, Depth));

        float sideSegmentWidth = (Width - DoorWidth) / 2f;
        AddSolid(new Vector3(-Width / 2f + sideSegmentWidth / 2f, Height / 2f, Depth / 2f - WallThickness / 2f), new Vector3(sideSegmentWidth, Height, WallThickness));
        AddSolid(new Vector3(Width / 2f - sideSegmentWidth / 2f, Height / 2f, Depth / 2f - WallThickness / 2f), new Vector3(sideSegmentWidth, Height, WallThickness));
    }

    private void AddSolid(Vector3 localPosition, Vector3 localSize) => objects.Add(new WallObject(TransformPoint(localPosition), TransformSize(localSize)));

    private void AddDetail(Vector3 localPosition, Vector3 localSize) => objects.Add(new ModuleDetailObject(TransformPoint(localPosition), TransformSize(localSize)));
}


