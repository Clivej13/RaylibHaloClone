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
    private static readonly Color RoutePlatformColor = new(88, 132, 190, 255);
    private static readonly Color FinalPlatformColor = new(190, 142, 64, 255);

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
    public IReadOnlyList<BoundingBox> CollisionBoxes => collisionBoxes;

    private readonly List<BoundingBox> collisionBoxes = new();
    private readonly List<(Vector3 Position, Vector3 Size)> coverObjects = new();
    private readonly List<(Vector3 Position, Vector3 Size, bool IsFinal)> routePlatforms = new();
    private readonly List<(Vector3 Position, Vector3 Size)> walls = new();

    public Level()
    {
        AddWall(new Vector3(0f, WallHeight / 2f, -ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(0f, WallHeight / 2f, ArenaHalfSize), new Vector3(ArenaHalfSize * 2f, WallHeight, WallThickness));
        AddWall(new Vector3(-ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));
        AddWall(new Vector3(ArenaHalfSize, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f));

        BuildPlatformingRoute();
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

        foreach (var platform in routePlatforms)
        {
            Color color = platform.IsFinal ? FinalPlatformColor : RoutePlatformColor;
            Raylib.DrawCubeV(platform.Position, platform.Size, color);
            Raylib.DrawCubeWiresV(platform.Position, platform.Size, Color.Black);
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

    private static BoundingBox ToBoundingBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size / 2f;
        return new BoundingBox(center - halfSize, center + halfSize);
    }
}