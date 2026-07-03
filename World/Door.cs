using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Door : ISolidLevelObject, IResettableLevelObject
{
    public Door(string name, Vector3 closedPosition, Vector3 size, Color closedColor, Color openColor)
    {
        Name = name;
        ClosedPosition = closedPosition;
        Size = size;
        ClosedColor = closedColor;
        OpenColor = openColor;
    }

    public string Name { get; }
    public Vector3 ClosedPosition { get; }
    public Vector3 Position => IsOpen ? ClosedPosition + new Vector3(0f, Size.Y + 0.25f, 0f) : ClosedPosition;
    public Vector3 Size { get; }
    public Color ClosedColor { get; }
    public Color OpenColor { get; }
    public bool IsOpen { get; private set; }
    public BoundingBox CollisionBox => Level.ToBoundingBox(ClosedPosition, Size);

    public void Render(bool lightsOn)
    {
        Raylib.DrawCubeV(Position, Size, IsOpen ? OpenColor : ClosedColor);
        Raylib.DrawCubeWiresV(Position, Size, IsOpen ? Color.SkyBlue : Color.Black);
    }

    public void Open() => IsOpen = true;
    public void Reset() => IsOpen = false;
}
