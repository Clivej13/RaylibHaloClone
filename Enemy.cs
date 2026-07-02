using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Enemy
{
    private static readonly Color BodyColor = new(170, 72, 72, 255);
    private static readonly Color HeadColor = new(210, 130, 96, 255);
    private static readonly Color WireColor = new(70, 24, 24, 255);
    private static readonly Color GunColor = new(28, 31, 36, 255);
    private static readonly Color AimLineColor = new(255, 90, 24, 120);
    private static readonly Color ShotColor = new(255, 48, 48, 220);
    private static readonly Color MuzzleFlashColor = new(255, 210, 80, 235);

    private const float Width = 0.9f;
    private const float Height = 2.1f;
    private const float Depth = 0.9f;
    private const float DamageFlashTime = 0.12f;
    private const float DetectionRange = 18f;
    private const float AttackRange = 11f;
    private const float StopDistance = 2.2f;
    private const float MoveSpeed = 2.4f;
    private const float StrafeSpeed = 1.25f;
    private const float FireCooldown = 1.25f;
    private const float AimTime = 0.42f;
    private const float Damage = 12f;
    private const float ShotCueTime = 0.1f;
    private const float MuzzleFlashTime = 0.12f;

    private float damageFlashRemaining;
    private float fireTimer;
    private float aimTimer;
    private float recentShotTimer;
    private float muzzleFlashTimer;
    private float strafeTimer;
    private int strafeDirection = 1;
    private Vector3 facingDirection = Vector3.UnitZ;
    private Vector3 lastShotStart;
    private Vector3 lastShotEnd;

    public Enemy(Vector3 position, float health = 100f)
    {
        Position = position;
        Health = health;
    }

    public Vector3 Position { get; private set; }
    public float Health { get; private set; }
    public bool IsAlive => Health > 0f;
    private bool IsDamageFlashing => damageFlashRemaining > 0f;
    private bool IsAiming => aimTimer > 0f;

    public BoundingBox Hitbox
    {
        get
        {
            Vector3 halfExtents = new(Width / 2f, Height / 2f, Depth / 2f);
            Vector3 center = Position + new Vector3(0f, Height / 2f, 0f);
            return new BoundingBox(center - halfExtents, center + halfExtents);
        }
    }

    public void Update(Player player, Level level, float deltaTime)
    {
        damageFlashRemaining = MathF.Max(0f, damageFlashRemaining - deltaTime);
        recentShotTimer = MathF.Max(0f, recentShotTimer - deltaTime);
        muzzleFlashTimer = MathF.Max(0f, muzzleFlashTimer - deltaTime);
        fireTimer = MathF.Max(0f, fireTimer - deltaTime);

        if (!IsAlive || !player.IsAlive)
        {
            aimTimer = 0f;
            return;
        }

        Vector3 toPlayer = MathUtils.Flatten(player.Position - Position);
        float distanceToPlayer = toPlayer.Length();
        if (distanceToPlayer > DetectionRange || distanceToPlayer <= 0.001f)
        {
            aimTimer = 0f;
            return;
        }

        Vector3 directionToPlayer = toPlayer / distanceToPlayer;
        facingDirection = directionToPlayer;

        if (distanceToPlayer > StopDistance && !IsAiming)
        {
            Vector3 moveDirection = distanceToPlayer > AttackRange ? directionToPlayer : GetStrafeDirection(directionToPlayer, deltaTime);
            TryMove(level, moveDirection * MoveSpeed * deltaTime);
        }

        bool canSeePlayer = distanceToPlayer <= AttackRange && HasLineOfSight(player, level);
        if (!canSeePlayer)
        {
            aimTimer = 0f;
            return;
        }

        if (fireTimer > 0f)
        {
            return;
        }

        aimTimer += deltaTime;
        if (aimTimer >= AimTime)
        {
            FireAt(player);
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive)
        {
            return;
        }

        Health = MathF.Max(0f, Health - damage);
        damageFlashRemaining = DamageFlashTime;
    }

    public void Render()
    {
        if (!IsAlive)
        {
            return;
        }

        Vector3 bodyCenter = Position + new Vector3(0f, 0.9f, 0f);
        Vector3 bodySize = new(Width, 1.6f, Depth);
        Vector3 headCenter = Position + new Vector3(0f, 1.85f, 0f);

        Color bodyColor = IsDamageFlashing ? Color.White : BodyColor;
        Color headColor = IsDamageFlashing ? Color.White : HeadColor;

        Raylib.DrawCubeV(bodyCenter, bodySize, bodyColor);
        Raylib.DrawCubeWiresV(bodyCenter, bodySize, WireColor);
        Raylib.DrawSphere(headCenter, 0.32f, headColor);
        Raylib.DrawSphereWires(headCenter, 0.32f, 8, 8, WireColor);
        DrawGun();

        if (IsAiming)
        {
            DrawAimTelegraph();
        }

        if (recentShotTimer > 0f)
        {
            Raylib.DrawLine3D(lastShotStart, lastShotEnd, ShotColor);
        }

        if (muzzleFlashTimer > 0f)
        {
            DrawMuzzleGlow(muzzleFlashTimer / MuzzleFlashTime, MuzzleFlashColor);
        }
    }

    private void DrawGun()
    {
        Vector3 muzzle = GetShotOrigin();
        Vector3 stock = muzzle - facingDirection * 0.75f;
        Raylib.DrawCylinderEx(stock, muzzle, 0.08f, 0.11f, 8, GunColor);
        Raylib.DrawCylinderWiresEx(stock, muzzle, 0.08f, 0.11f, 8, Color.Black);
    }

    private void DrawAimTelegraph()
    {
        float charge = MathUtils.Clamp(aimTimer / AimTime, 0f, 1f);
        DrawMuzzleGlow(charge, new Color(255, 96, 24, 210));
        Raylib.DrawLine3D(GetShotOrigin(), lastShotEnd, AimLineColor);
    }

    private void DrawMuzzleGlow(float intensity, Color color)
    {
        float radius = 0.08f + 0.18f * MathUtils.Clamp(intensity, 0f, 1f);
        Raylib.DrawSphere(GetShotOrigin(), radius, color);
    }

    private Vector3 GetStrafeDirection(Vector3 directionToPlayer, float deltaTime)
    {
        strafeTimer -= deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection *= -1;
            strafeTimer = 1.4f;
        }

        Vector3 strafe = Vector3.Normalize(Vector3.Cross(directionToPlayer, Vector3.UnitY)) * strafeDirection;
        return Vector3.Normalize(strafe * StrafeSpeed + directionToPlayer * 0.35f);
    }

    private void TryMove(Level level, Vector3 movement)
    {
        if (movement.LengthSquared() <= 0f)
        {
            return;
        }

        Vector3 nextPosition = level.ClampToArena(Position + movement, Width / 2f);
        BoundingBox nextHitbox = CreateHitbox(nextPosition);
        foreach (BoundingBox box in level.CollisionBoxes)
        {
            if (Raylib.CheckCollisionBoxes(nextHitbox, box))
            {
                return;
            }
        }

        Position = nextPosition;
    }

    private bool HasLineOfSight(Player player, Level level)
    {
        Vector3 shotStart = GetShotOrigin();
        lastShotEnd = player.CameraPosition;
        Vector3 toPlayer = player.CameraPosition - shotStart;
        float distance = toPlayer.Length();
        Ray ray = new(shotStart, Vector3.Normalize(toPlayer));

        foreach (BoundingBox box in level.CollisionBoxes)
        {
            RayCollision collision = Raylib.GetRayCollisionBox(ray, box);
            if (collision.Hit && collision.Distance < distance)
            {
                return false;
            }
        }

        return true;
    }

    private void FireAt(Player player)
    {
        fireTimer = FireCooldown;
        aimTimer = 0f;
        recentShotTimer = ShotCueTime;
        muzzleFlashTimer = MuzzleFlashTime;
        lastShotStart = GetShotOrigin();
        lastShotEnd = player.CameraPosition;
        player.ApplyDamage(Damage);
    }

    private Vector3 GetShotOrigin() => Position + new Vector3(0f, 1.45f, 0f) + facingDirection * 0.7f;

    private static BoundingBox CreateHitbox(Vector3 position)
    {
        Vector3 halfExtents = new(Width / 2f, Height / 2f, Depth / 2f);
        Vector3 center = position + new Vector3(0f, Height / 2f, 0f);
        return new BoundingBox(center - halfExtents, center + halfExtents);
    }
}
