using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class LightFixtureObject : ILevelObject
{
    public LightFixtureObject(Vector3 position)
    {
        Position = position;
    }

    public Vector3 Position { get; }
    public Vector3 Size { get; } = new(0.48f, 0.48f, 0.48f);

    public void Render(bool lightsOn)
    {
        Raylib.DrawSphere(Position, 0.22f, lightsOn ? Color.Gold : Color.DarkGray);
        Raylib.DrawSphereWires(Position, 0.24f, 8, 8, lightsOn ? Color.Yellow : Color.Black);
    }
}
