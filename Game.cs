using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public enum MatchState
{
    Playing,
    Victory,
    Defeated
}

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
    private readonly List<Enemy> enemies;
    private readonly WeaponViewModel weaponViewModel;
    private float accumulator;
    private bool disposed;
    private MatchState matchState = MatchState.Playing;

    public Game()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, WindowTitle);
        Raylib.SetTargetFPS(144);
        Raylib.DisableCursor();

        level = new Level();
        player = new Player(level.PlayerSpawnPosition);
        hud = new Hud();
        enemies = CreateTestEnemies();
        weaponViewModel = new WeaponViewModel();
    }

    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            float frameTime = MathF.Min(Raylib.GetFrameTime(), MaxFrameTime);

            if (matchState == MatchState.Playing)
            {
                UpdatePlaying(frameTime);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                ResetLevel();
            }

            Render();
        }
    }

    private void UpdatePlaying(float frameTime)
    {
        accumulator += frameTime;

        player.UpdateLook(Raylib.GetMouseDelta());
        player.UpdateShieldRecharge(frameTime);
        CombatUpdateResult combatResult = player.UpdateCombat(enemies, frameTime);
        hud.Update(player, combatResult.Hit, frameTime);
        weaponViewModel.Update(player.MovementBobSpeed, frameTime);

        if (combatResult.Fired)
        {
            weaponViewModel.AddRecoil();
        }

        foreach (Enemy enemy in enemies)
        {
            enemy.Update(player, level, frameTime);
        }

        while (accumulator >= FixedTimeStep)
        {
            player.FixedUpdate(level, FixedTimeStep);
            accumulator -= FixedTimeStep;
        }

        UpdateMatchState();
    }

    private void UpdateMatchState()
    {
        if (!player.IsAlive)
        {
            matchState = MatchState.Defeated;
            return;
        }

        if (enemies.All(enemy => !enemy.IsAlive))
        {
            matchState = MatchState.Victory;
        }
    }

    private void ResetLevel()
    {
        player.Reset(level.PlayerSpawnPosition);
        enemies.Clear();
        enemies.AddRange(CreateTestEnemies());
        hud.Reset(player);
        accumulator = 0f;
        matchState = MatchState.Playing;
    }

    private void Render()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(16, 18, 24, 255));

        Raylib.BeginMode3D(player.Camera);
        level.Render();
        foreach (Enemy enemy in enemies)
        {
            enemy.Render();
        }

        if (player.HasWeaponTracer)
        {
            Raylib.DrawLine3D(player.WeaponTraceStart, player.WeaponTraceEnd, new Color(120, 210, 255, 220));
        }

        Raylib.EndMode3D();

        weaponViewModel.Render(player.HasMuzzleFlash, player.MuzzleFlashIntensity);
        hud.Render(player, enemies.Count(enemy => enemy.IsAlive), matchState);
        Raylib.EndDrawing();
    }

    private static List<Enemy> CreateTestEnemies() =>
    [
        new Enemy(new Vector3(-6f, 0f, -8f)),
        new Enemy(new Vector3(0f, 0f, -12f)),
        new Enemy(new Vector3(6f, 0f, -8f)),
        new Enemy(new Vector3(-10f, 0f, 2f)),
        new Enemy(new Vector3(10f, 0f, 2f))
    ];

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
