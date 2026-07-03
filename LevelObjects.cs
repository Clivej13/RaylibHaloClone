using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public interface ILevelObject
{
    Vector3 Position { get; }
    Vector3 Size { get; }
    void Render(bool lightsOn);
}

public interface ISolidLevelObject : ILevelObject
{
    BoundingBox CollisionBox { get; }
}

public interface IResettableLevelObject : ILevelObject
{
    void Reset();
}

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

public sealed class WallObject : SolidBoxLevelObject
{
    public WallObject(Vector3 position, Vector3 size)
        : base("Wall", position, size, lightsOn => lightsOn ? Level.LitWallColor : Level.DarkWallColor, _ => Color.DarkGray)
    {
    }
}

public sealed class CoverObject : SolidBoxLevelObject
{
    public CoverObject(Vector3 position, Vector3 size)
        : base("Cover", position, size, lightsOn => lightsOn ? Level.LitCoverColor : Level.DarkCoverColor, _ => Color.Black)
    {
    }
}

public sealed class PlatformObject : SolidBoxLevelObject
{
    public PlatformObject(Vector3 position, Vector3 size, bool isFinal = false)
        : base("Route Platform", position, size, lightsOn => GetPlatformColor(lightsOn, isFinal), _ => Color.Black)
    {
        IsFinal = isFinal;
    }

    public bool IsFinal { get; }

    private static Color GetPlatformColor(bool lightsOn, bool isFinal)
    {
        Color color = isFinal ? Level.FinalPlatformColor : Level.RoutePlatformColor;
        return lightsOn ? color : new Color((byte)(color.R / 2), (byte)(color.G / 2), (byte)(color.B / 2), color.A);
    }
}

public sealed class ModuleDetailObject : BoxLevelObject
{
    public ModuleDetailObject(Vector3 position, Vector3 size)
        : base("Module Detail", position, size, lightsOn => lightsOn ? Level.LitFloorColor : Level.DarkFloorColor, _ => Color.DarkGray)
    {
    }
}

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
