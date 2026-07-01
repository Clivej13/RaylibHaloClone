using System.Numerics;

namespace RaylibHaloClone;

public static class MathUtils
{
    public const float Deg2Rad = MathF.PI / 180f;

    public static float Clamp(float value, float min, float max) => MathF.Min(MathF.Max(value, min), max);

    public static Vector3 Flatten(Vector3 value) => new(value.X, 0f, value.Z);

    public static Vector3 ClampHorizontal(Vector3 position, float minX, float maxX, float minZ, float maxZ)
    {
        position.X = Clamp(position.X, minX, maxX);
        position.Z = Clamp(position.Z, minZ, maxZ);
        return position;
    }
}
