using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class SpawnPointObject : ILevelObject
{
    public SpawnPointObject(Vector3 position, Vector3 lookDirection)
    {
        Position = position;
        LookDirection = lookDirection;
    }

    public Vector3 Position { get; }
    public Vector3 LookDirection { get; }
    public Vector3 Size { get; } = new(1.3f, 0.07f, 1.3f);

    public void Render(bool lightsOn)
    {
        Raylib.DrawCylinder(Position + new Vector3(0f, 0.025f, 0f), 0.65f, 0.65f, 0.05f, 24, Level.SpawnMarkerColor);
        Raylib.DrawCylinderWires(Position + new Vector3(0f, 0.035f, 0f), 0.65f, 0.65f, 0.07f, 24, Color.Green);
    }
}
