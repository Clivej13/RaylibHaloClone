using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

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
