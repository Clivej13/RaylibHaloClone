using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class ExitZoneObject : ILevelObject
{
    public ExitZoneObject(Vector3 position, Vector3 size)
    {
        Position = position;
        Size = size;
    }

    public Vector3 Position { get; }
    public Vector3 Size { get; }
    public BoundingBox ExitBox => Level.ToBoundingBox(Position, Size);

    public void Render(bool active)
    {
        Color fillColor = active ? Level.ActiveExitFillColor : Level.InactiveExitFillColor;
        Color wireColor = active ? Level.ActiveExitWireColor : Level.InactiveExitWireColor;

        Raylib.DrawCubeV(Position, Size, fillColor);
        Raylib.DrawCubeWiresV(Position, Size, wireColor);
        Raylib.DrawCubeWiresV(Position, Size + new Vector3(0.08f), wireColor);
    }
}
