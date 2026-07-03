using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class BoardingPodCrashSiteModule
{
    private readonly List<ILevelObject> objects = new();

    public BoardingPodCrashSiteModule(Vector3 origin, ModuleFacing facing)
    {
        Origin = origin;
        Facing = facing;
        BuildGeometry();
    }

    public Vector3 Origin { get; }
    public ModuleFacing Facing { get; }
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
        AddBrokenBreachFrame();
        AddDebrisField();
        AddImpactFloorDetails();
    }

    private void AddBrokenBreachFrame()
    {
        AddWallFragment(new Vector3(-1.35f, 1.55f, -2.44f), new Vector3(0.35f, 1.75f, 0.28f));
        AddWallFragment(new Vector3(1.32f, 1.35f, -2.48f), new Vector3(0.42f, 1.45f, 0.3f));
        AddWallFragment(new Vector3(-0.65f, 2.88f, -2.5f), new Vector3(0.9f, 0.24f, 0.34f));
        AddWallFragment(new Vector3(0.72f, 2.74f, -2.47f), new Vector3(0.7f, 0.32f, 0.28f));
        AddWallFragment(new Vector3(-1.05f, 0.28f, -2.75f), new Vector3(0.75f, 0.42f, 0.5f));
        AddWallFragment(new Vector3(1.08f, 0.22f, -2.7f), new Vector3(0.62f, 0.32f, 0.42f));
        AddWallFragment(new Vector3(-0.12f, 2.35f, -2.9f), new Vector3(0.32f, 0.72f, 0.22f));
    }

    private void AddDebrisField()
    {
        AddDetail(new Vector3(-0.8f, 0.08f, -3.18f), new Vector3(0.55f, 0.16f, 0.35f));
        AddDetail(new Vector3(0.55f, 0.06f, -3.02f), new Vector3(0.38f, 0.12f, 0.42f));
        AddDetail(new Vector3(-1.55f, 0.12f, -2.86f), new Vector3(0.42f, 0.24f, 0.3f));
        AddDetail(new Vector3(1.62f, 0.1f, -2.62f), new Vector3(0.46f, 0.2f, 0.32f));
        AddDetail(new Vector3(-0.42f, 0.07f, -2.18f), new Vector3(0.32f, 0.14f, 0.28f));
        AddDetail(new Vector3(0.36f, 0.09f, -1.62f), new Vector3(0.48f, 0.18f, 0.26f));
        AddDetail(new Vector3(-1.85f, 0.08f, -1.55f), new Vector3(0.5f, 0.16f, 0.24f));
        AddDetail(new Vector3(1.95f, 0.09f, -1.18f), new Vector3(0.34f, 0.18f, 0.34f));
        AddDetail(new Vector3(-0.15f, 0.05f, -0.72f), new Vector3(0.3f, 0.1f, 0.22f));
        AddDetail(new Vector3(0.98f, 0.07f, -0.38f), new Vector3(0.42f, 0.14f, 0.24f));
        AddDetail(new Vector3(-2.45f, 0.1f, -3.35f), new Vector3(0.48f, 0.2f, 0.46f));
        AddDetail(new Vector3(2.42f, 0.11f, -2.95f), new Vector3(0.52f, 0.22f, 0.34f));

        AddSolid(new Vector3(-2.55f, 0.38f, -2.58f), new Vector3(0.9f, 0.76f, 0.72f));
        AddSolid(new Vector3(2.52f, 0.32f, -2.18f), new Vector3(0.82f, 0.64f, 0.68f));
        AddSolid(new Vector3(-2.35f, 0.28f, -0.92f), new Vector3(0.78f, 0.56f, 0.52f));
    }

    private void AddImpactFloorDetails()
    {
        AddScorchMark(new Vector3(0f, 0.012f, -2.65f), new Vector3(2.15f, 0.024f, 1.25f));
        AddScorchMark(new Vector3(-0.35f, 0.014f, -1.5f), new Vector3(1.45f, 0.028f, 0.72f));
        AddDetail(new Vector3(-1.15f, 0.035f, -1.92f), new Vector3(0.9f, 0.07f, 0.08f));
        AddDetail(new Vector3(1.12f, 0.035f, -1.74f), new Vector3(0.75f, 0.07f, 0.08f));
        AddDetail(new Vector3(0.48f, 0.04f, -2.22f), new Vector3(0.08f, 0.08f, 0.7f));
    }

    private void AddSolid(Vector3 localPosition, Vector3 localSize) => objects.Add(new WallObject(TransformPoint(localPosition), TransformSize(localSize)));

    private void AddDetail(Vector3 localPosition, Vector3 localSize) => objects.Add(new ModuleDetailObject(TransformPoint(localPosition), TransformSize(localSize)));

    private void AddWallFragment(Vector3 localPosition, Vector3 localSize)
    {
        objects.Add(new BoxLevelObject(
            "Broken Wall Fragment",
            TransformPoint(localPosition),
            TransformSize(localSize),
            lightsOn => lightsOn ? Level.LitWallColor : Level.DarkWallColor,
            _ => Color.DarkGray));
    }

    private void AddScorchMark(Vector3 localPosition, Vector3 localSize)
    {
        objects.Add(new BoxLevelObject(
            "Impact Scorch Mark",
            TransformPoint(localPosition),
            TransformSize(localSize),
            _ => new Color(18, 16, 14, 210),
            _ => new Color(70, 52, 34, 180)));
    }
}
