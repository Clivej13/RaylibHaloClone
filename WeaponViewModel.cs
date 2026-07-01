using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class WeaponViewModel
{
    private const float BobFrequency = 3f;
    private const float BobAmount = 0.035f;
    private const float RecoilDuration = 0.12f;
    private const float RecoilKick = 0.18f;

    private float bobPhase;
    private float recoilRemaining;

    public void Update(float movementSpeed, float deltaTime)
    {
        bobPhase += movementSpeed * BobFrequency * deltaTime;
        recoilRemaining = MathF.Max(0f, recoilRemaining - deltaTime);
    }

    public void AddRecoil()
    {
        recoilRemaining = RecoilDuration;
    }

    public void Render()
    {
        float bob = MathF.Sin(bobPhase) * BobAmount;
        float recoil = recoilRemaining / RecoilDuration;

        Vector3 basePosition = new(-0.55f, -0.45f + bob, 1.15f - recoil * RecoilKick);

        Camera3D viewCamera = new(
            Vector3.Zero,
            Vector3.UnitZ,
            Vector3.UnitY,
            60f,
            CameraProjection.Perspective);

        Raylib.BeginMode3D(viewCamera);

        DrawPart(
            basePosition,
            new Vector3(0.22f, 0.18f, 0.75f),
            new Color(48, 55, 62, 255));

        DrawPart(
            basePosition + new Vector3(0f, 0.09f, -0.22f),
            new Vector3(0.18f, 0.12f, 0.26f),
            new Color(75, 84, 95, 255));

        DrawPart(
            basePosition + new Vector3(0f, -0.16f, -0.08f),
            new Vector3(0.14f, 0.3f, 0.16f),
            new Color(34, 38, 44, 255));

        DrawPart(
            basePosition + new Vector3(0f, 0.01f, 0.48f),
            new Vector3(0.12f, 0.1f, 0.45f),
            new Color(40, 45, 52, 255));

        Raylib.EndMode3D();
    }

    private static void DrawPart(Vector3 position, Vector3 size, Color color)
    {
        Raylib.DrawCubeV(position, size, color);
        Raylib.DrawCubeWiresV(position, size, Color.Black);
    }
}