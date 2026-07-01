using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Level
{
    public const float ArenaHalfSize = 24f;
    public const float WallHeight = 5f;
    public const float WallThickness = 1f;

    private static readonly Color FloorColor = new(64, 72, 82, 255);
    private static readonly Color WallColor = new(82, 92, 108, 255);
    private static readonly Color CoverColor = new(100, 111, 128, 255);

    public Vector3 PlayerSpawnPosition { get; } = new(0f, 0f, 8f);
    public IReadOnlyList<BoundingBox> CollisionBoxes => collisionBoxes;

    private readonly List<BoundingBox> collisionBoxes = new();
    private readonly List<(Vector3 Position, Vector3 Size)> coverObjects = new();
    private readonly List<(Vector3 Position, Vector3 Size)> walls = new();

    public Level()
    {
        AddWall(new Vector3(0f, WallHeight / 2f, -ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(0f, WallHeight / 2f, ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(-ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));
        AddWall(new Vector3(ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));

        AddCover(new Vector3(-8f, 1f, -6f), new Vector3(3f, 2f, 5f));
        AddCover(new Vector3(7f, 1.25f, -8f), new Vector3(5f, 2.5f, 3f));
        AddCover(new Vector3(-5f, 0.75f, 6f), new Vector3(4f, 1.5f, 3f));
        AddCover(new Vector3(9f, 1f, 7f), new Vector3(3f, 2f, 4f));
        AddCover(new Vector3(0f, 0.6f, 0f), new Vector3(3f, 1.2f, 3f));
    }

    public void Render()
    {
        Raylib.DrawPlane(Vector3.Zero, new Vector2(ArenaHalfSize * 2f, ArenaHalfSize * 2f), FloorColor);

        foreach (var wall in walls)
        {
            Raylib.DrawCubeV(wall.Position, wall.Size, WallColor);
            Raylib.DrawCubeWiresV(wall.Position, wall.Size, Color.DarkGray);
        }

        foreach (var cover in coverObjects)
        {
            Raylib.DrawCubeV(cover.Position, cover.Size, CoverColor);
            Raylib.DrawCubeWiresV(cover.Position, cover.Size, Color.Black);
        }

        Raylib.DrawGrid((int)ArenaHalfSize * 2, 1f);
    }

    public Vector3 ClampToArena(Vector3 position, float radius)
    {
        return MathUtils.ClampHorizontal(position, -ArenaHalfSize + radius, ArenaHalfSize - radius, -ArenaHalfSize + radius, ArenaHalfSize - radius);
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

    private static BoundingBox ToBoundingBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size / 2f;
        return new BoundingBox(center - halfSize, center + halfSize);
    }
}
