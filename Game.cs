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
    private const string WindowTitle = "Halo-Inspired FPS Prototype";
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
    private bool eliminateTargetsObjectiveComplete;
    private IInteractable? selectedInteractable;

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
        UpdateInteraction();

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

        eliminateTargetsObjectiveComplete = enemies.All(enemy => !enemy.IsAlive);
        if (eliminateTargetsObjectiveComplete && level.ExitSwitchActivated && IsPlayerFullyInsideExit(player))
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
        eliminateTargetsObjectiveComplete = false;
        selectedInteractable = null;
        level.ResetInteractiveState();
    }

    private void Render()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(level.BackgroundColor);

        Raylib.BeginMode3D(player.Camera);
        level.Render(eliminateTargetsObjectiveComplete && level.ExitSwitchActivated);
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
        hud.Render(player, enemies.Count(enemy => enemy.IsAlive), matchState, GetObjectiveText(), selectedInteractable?.GetPrompt(player));
        Raylib.EndDrawing();
    }

    private void UpdateInteraction()
    {
        selectedInteractable = GetCrosshairInteractable();
        if (selectedInteractable is not null && Raylib.IsKeyPressed(KeyboardKey.E))
        {
            selectedInteractable.Interact(player, level);
            selectedInteractable = GetCrosshairInteractable();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.G))
        {
            level.DropWeapon(player, player.DropCurrentWeapon());
            selectedInteractable = GetCrosshairInteractable();
        }
    }

    private IInteractable? GetCrosshairInteractable()
    {
        const float maxActivationDistance = 2.85f;
        Ray ray = new(player.CameraPosition, Vector3.Normalize(player.LookDirection));
        IInteractable? closest = null;
        float closestDistance = maxActivationDistance;

        foreach (IInteractable interactable in level.Interactables)
        {
            if (!interactable.IsActive || !interactable.CanInteract(player))
            {
                continue;
            }

            RayCollision collision = Raylib.GetRayCollisionBox(ray, interactable.Bounds);
            if (collision.Hit && collision.Distance <= closestDistance)
            {
                closest = interactable;
                closestDistance = collision.Distance;
            }
        }

        return closest;
    }

    private string GetObjectiveText()
    {
        if (!eliminateTargetsObjectiveComplete)
        {
            return "Objective: Eliminate all targets";
        }

        return level.ExitSwitchActivated
            ? "Objective complete: Proceed to extraction"
            : "Objective: Activate extraction switch";
    }

    private bool IsPlayerFullyInsideExit(Player player) => ContainsPlayerBox(level.ExitBox, player.CollisionBox);

    private static bool ContainsPlayerBox(BoundingBox exitBox, BoundingBox playerBox) =>
        playerBox.Min.X > exitBox.Min.X && playerBox.Max.X < exitBox.Max.X &&
        playerBox.Min.Y > exitBox.Min.Y && playerBox.Max.Y < exitBox.Max.Y &&
        playerBox.Min.Z > exitBox.Min.Z && playerBox.Max.Z < exitBox.Max.Z;

    private static List<Enemy> CreateTestEnemies() =>
    [
        new Enemy(new Vector3(-4.8f, 0f, -3.3f)),
        new Enemy(new Vector3(0.8f, 0f, -5.8f)),
        new Enemy(new Vector3(4.6f, 0f, -4.2f)),
        new Enemy(new Vector3(-3.2f, 0f, -8.1f)),
        new Enemy(new Vector3(2.8f, 0f, -8.4f))
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
