using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class CoverObject : SolidBoxLevelObject
{
    public CoverObject(Vector3 position, Vector3 size)
        : base("Cover", position, size, lightsOn => lightsOn ? Level.LitCoverColor : Level.DarkCoverColor, _ => Color.Black)
    {
    }
}
