using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Game : IDisposable
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string WindowTitle = "Halo-Inspired FPS Prototype - Phase 1";
    private const float FixedTimeStep = 1f / 60f;
    private const float MaxFrameTime = 0.25f;

    private readonly Level level;
    private readonly Player player;
    private readonly Hud hud;
    private float accumulator;
    private bool disposed;

    public Game()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, WindowTitle);
        Raylib.SetTargetFPS(144);
        Raylib.DisableCursor();

        level = new Level();
        player = new Player(level.PlayerSpawnPosition);
        hud = new Hud();
    }

    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            float frameTime = MathF.Min(Raylib.GetFrameTime(), MaxFrameTime);
            accumulator += frameTime;

            player.UpdateLook(Raylib.GetMouseDelta());

            while (accumulator >= FixedTimeStep)
            {
                player.FixedUpdate(level, FixedTimeStep);
                accumulator -= FixedTimeStep;
            }

            Render();
        }
    }

    private void Render()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(16, 18, 24, 255));

        Raylib.BeginMode3D(player.Camera);
        level.Render();
        Raylib.EndMode3D();

        hud.Render(player);
        Raylib.EndDrawing();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Raylib.EnableCursor();
        Raylib.CloseWindow();
        disposed = true;
    }
}
