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
