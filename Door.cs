using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Door
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
    public Vector3 Size { get; }
    public Color ClosedColor { get; }
    public Color OpenColor { get; }
    public bool IsOpen { get; private set; }
    public BoundingBox CollisionBox => Level.ToBoundingBox(ClosedPosition, Size);

    public void Open() => IsOpen = true;
    public void Reset() => IsOpen = false;
}

