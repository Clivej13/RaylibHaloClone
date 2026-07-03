using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class ModuleDetailObject : BoxLevelObject
{
    public ModuleDetailObject(Vector3 position, Vector3 size)
        : base("Module Detail", position, size, lightsOn => lightsOn ? Level.LitFloorColor : Level.DarkFloorColor, _ => Color.DarkGray)
    {
    }
}
