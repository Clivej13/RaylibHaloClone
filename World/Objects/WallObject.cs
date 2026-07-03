using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class WallObject : SolidBoxLevelObject
{
    public WallObject(Vector3 position, Vector3 size)
        : base("Wall", position, size, lightsOn => lightsOn ? Level.LitWallColor : Level.DarkWallColor, _ => Color.DarkGray)
    {
    }
}
