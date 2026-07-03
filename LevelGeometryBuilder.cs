using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed partial class Level
{
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

}
