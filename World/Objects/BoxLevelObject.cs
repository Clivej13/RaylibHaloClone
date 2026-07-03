using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public class BoxLevelObject : ILevelObject
{
    private readonly Func<bool, Color> fillColorProvider;
    private readonly Func<bool, Color> wireColorProvider;

    public BoxLevelObject(string name, Vector3 position, Vector3 size, Func<bool, Color> fillColorProvider, Func<bool, Color> wireColorProvider)
    {
        Name = name;
        Position = position;
        Size = size;
        this.fillColorProvider = fillColorProvider;
        this.wireColorProvider = wireColorProvider;
    }

    public string Name { get; }
    public Vector3 Position { get; }
    public Vector3 Size { get; }
    public virtual void Render(bool lightsOn)
    {
        Raylib.DrawCubeV(Position, Size, fillColorProvider(lightsOn));
        Raylib.DrawCubeWiresV(Position, Size, wireColorProvider(lightsOn));
    }
}

public class SolidBoxLevelObject : BoxLevelObject, ISolidLevelObject
{
    public SolidBoxLevelObject(string name, Vector3 position, Vector3 size, Func<bool, Color> fillColorProvider, Func<bool, Color> wireColorProvider)
        : base(name, position, size, fillColorProvider, wireColorProvider)
    {
    }

    public BoundingBox CollisionBox => Level.ToBoundingBox(Position, Size);
}
