using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public readonly record struct CombatUpdateResult(bool Fired, bool Hit);

public sealed class Player
{
    public const float EyeHeight = 1.75f;

    private const float Radius = 0.35f;
    private const float Height = 1.9f;
    private const float GroundSnapTolerance = 0.05f;
    private const float CollisionSkin = 0.001f;
    private const float WalkSpeed = 6f;
    private const float SprintSpeed = 10f;
    private const float Acceleration = 14f;
    private const float AirAcceleration = 5f;
    private const float JumpVelocity = 7.5f;
    private const float Gravity = 22f;
    private const float MaxLookPitch = 88f;
    private const float MouseSensitivity = 0.10f;
    public const float MaxHealth = 100f;
    public const float MaxShield = 100f;
    private const float ShieldRechargeDelay = 5f;
    private const float ShieldRechargeRate = 28f;
    private const float WeaponTracerTime = 0.06f;
    private const float MuzzleFlashTime = 0.05f;
    private const float ScreenKickDegrees = 0.45f;


    private Vector3 position;
    private Vector3 velocity;
    private float yaw;
    private float pitch;
    private bool grounded;
    private float timeSinceDamage = ShieldRechargeDelay;
    private float weaponTracerRemaining;
    private float muzzleFlashRemaining;
    private Vector3 lastWeaponTraceStart;
    private Vector3 lastWeaponTraceEnd;


    public Player(Vector3 spawnPosition)
    {
        position = spawnPosition;
        Health = MaxHealth;
        Shield = MaxShield;
        Vector3 cameraPosition = GetCameraPosition();
        Camera = new Camera3D(cameraPosition, cameraPosition + Forward, Vector3.UnitY, 75f, CameraProjection.Perspective);
        Equipment.ResetToDefaultMissionLoadout();
    }

    public Camera3D Camera { get; private set; }
    public Vector3 Position => position;
    public BoundingBox CollisionBox => CreatePlayerBox(position);
    public Vector3 LookDirection => Forward;
    public Vector3 CameraPosition => GetCameraPosition();
    public Equipment Equipment { get; } = new();
    public Weapon? CurrentWeapon => Equipment.CurrentWeapon;
    public EquippedSlot EquippedSlot => Equipment.EquippedSlot;
    public float Health { get; private set; }
    public float Shield { get; private set; }
    public bool IsAlive => Health > 0f;
    public float CurrentHorizontalSpeed => MathUtils.Flatten(velocity).Length();
    public float MovementBobSpeed => CurrentHorizontalSpeed;
    public bool HasWeaponTracer => weaponTracerRemaining > 0f;
    public bool HasMuzzleFlash => muzzleFlashRemaining > 0f;
    public float MuzzleFlashIntensity => MuzzleFlashTime <= 0f ? 0f : muzzleFlashRemaining / MuzzleFlashTime;
    public Vector3 WeaponTraceStart => lastWeaponTraceStart;
    public Vector3 WeaponTraceEnd => lastWeaponTraceEnd;

    private Vector3 Forward
    {
        get
        {
            float yawRadians = yaw * MathUtils.Deg2Rad;
            float pitchRadians = pitch * MathUtils.Deg2Rad;
            return Vector3.Normalize(new Vector3(
                MathF.Sin(yawRadians) * MathF.Cos(pitchRadians),
                MathF.Sin(pitchRadians),
                MathF.Cos(yawRadians) * MathF.Cos(pitchRadians)));
        }
    }

    private Vector3 FlatForward
    {
        get
        {
            float yawRadians = yaw * MathUtils.Deg2Rad;
            return Vector3.Normalize(new Vector3(MathF.Sin(yawRadians), 0f, MathF.Cos(yawRadians)));
        }
    }

    private Vector3 Right => Vector3.Normalize(Vector3.Cross(FlatForward, Vector3.UnitY));

    public void ApplyDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
        {
            return;
        }

        timeSinceDamage = 0f;
        PlayShieldHitSound();
        float shieldDamage = MathF.Min(Shield, damage);
        Shield -= shieldDamage;
        Health = MathF.Max(0f, Health - (damage - shieldDamage));
    }

    public bool Heal(float amount)
    {
        if (!IsAlive || amount <= 0f || Health >= MaxHealth)
        {
            return false;
        }

        Health = MathF.Min(MaxHealth, Health + amount);
        return true;
    }

    public Weapon? PickUpWeapon(Weapon newWeapon)
    {
        EquippedSlot matchingSlot = Equipment.GetSlotForCategory(newWeapon.Category);
        Weapon? previousWeapon = Equipment.GetWeapon(matchingSlot);

        if (previousWeapon is null)
        {
            Equipment.SetWeapon(matchingSlot, newWeapon);
            Equipment.EquipSlot(matchingSlot);
            return null;
        }

        // If the relevant slot is full, keep the rule deterministic: replace the
        // category-matched slot and return its previous weapon so the pickup system
        // can drop it nearby. When that slot is currently equipped, this is a direct
        // swap with the visible weapon; otherwise the equipped slot is left unchanged.
        return Equipment.SetWeapon(matchingSlot, newWeapon);
    }

    public Weapon? DropCurrentWeapon()
    {
        return Equipment.RemoveCurrentWeapon();
    }

    public void EquipSlot(EquippedSlot slot)
    {
        Equipment.EquipSlot(slot);
    }

    public void UpdateShieldRecharge(float deltaTime)
    {
        if (!IsAlive)
        {
            return;
        }

        timeSinceDamage += deltaTime;
        if (timeSinceDamage >= ShieldRechargeDelay && Shield < MaxShield)
        {
            Shield = MathF.Min(MaxShield, Shield + ShieldRechargeRate * deltaTime);
        }
    }

    public void Reset(Vector3 spawnPosition)
    {
        position = spawnPosition;
        velocity = Vector3.Zero;
        grounded = false;
        Health = MaxHealth;
        Shield = MaxShield;
        timeSinceDamage = ShieldRechargeDelay;
        // Reset/restarts restore the default mission loadout. This intentionally recreates
        // the starting MA5B Rifle and M6D Pistol only at level reset, not every frame/update,
        // so dropped weapons stay removed from their slots until another weapon is picked up.
        Equipment.ResetToDefaultMissionLoadout();
        weaponTracerRemaining = 0f;
        muzzleFlashRemaining = 0f;
        UpdateCamera();
    }

    public void UpdateLook(Vector2 mouseDelta)
    {
        yaw -= mouseDelta.X * MouseSensitivity;
        pitch = MathUtils.Clamp(pitch - mouseDelta.Y * MouseSensitivity, -MaxLookPitch, MaxLookPitch);
        UpdateCamera();
    }

    public CombatUpdateResult UpdateCombat(IReadOnlyList<Enemy> enemies, float deltaTime)
    {
        if (!IsAlive)
        {
            return new CombatUpdateResult(false, false);
        }

        CurrentWeapon?.Update(deltaTime);
        weaponTracerRemaining = MathF.Max(0f, weaponTracerRemaining - deltaTime);
        muzzleFlashRemaining = MathF.Max(0f, muzzleFlashRemaining - deltaTime);

        if (Raylib.IsKeyPressed(KeyboardKey.One)) Equipment.EquipSlot(EquippedSlot.Primary);
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) Equipment.EquipSlot(EquippedSlot.Secondary);
        if (Raylib.IsKeyPressed(KeyboardKey.Three)) Equipment.EquipSlot(EquippedSlot.Sidearm);

        Weapon? currentWeapon = CurrentWeapon;
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            currentWeapon?.Reload();
        }

        if (currentWeapon is null)
        {
            return new CombatUpdateResult(false, false);
        }

        bool wantsToFire = currentWeapon.IsAutomatic
            ? Raylib.IsMouseButtonDown(MouseButton.Left)
            : Raylib.IsMouseButtonPressed(MouseButton.Left);
        if (!wantsToFire)
        {
            return new CombatUpdateResult(false, false);
        }

        Vector3 traceStart = CameraPosition + LookDirection * 0.45f - Vector3.UnitY * 0.12f;
        WeaponFireResult fireResult = currentWeapon.Fire(CameraPosition, LookDirection, enemies);
        if (fireResult.Fired)
        {
            lastWeaponTraceStart = traceStart;
            lastWeaponTraceEnd = fireResult.TraceEnd;
            weaponTracerRemaining = WeaponTracerTime;
            muzzleFlashRemaining = MuzzleFlashTime;
            ApplyWeaponKick();
            PlayWeaponFireSound();
        }

        return new CombatUpdateResult(fireResult.Fired, fireResult.Hit);
    }

    public void FixedUpdate(Level level, float deltaTime)
    {
        if (!IsAlive)
        {
            return;
        }

        Vector3 input = GetMovementInput();
        float targetSpeed = Raylib.IsKeyDown(KeyboardKey.LeftShift) ? SprintSpeed : WalkSpeed;
        Vector3 targetVelocity = input * targetSpeed;
        float accel = grounded ? Acceleration : AirAcceleration;

        velocity.X = MoveTowards(velocity.X, targetVelocity.X, accel * deltaTime);
        velocity.Z = MoveTowards(velocity.Z, targetVelocity.Z, accel * deltaTime);

        if (grounded && Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            velocity.Y = JumpVelocity;
            grounded = false;
        }

        velocity.Y -= Gravity * deltaTime;
        MoveAndCollide(level, deltaTime);
        UpdateCamera();
    }

    private void ApplyWeaponKick()
    {
        pitch = MathUtils.Clamp(pitch + ScreenKickDegrees, -MaxLookPitch, MaxLookPitch);
        UpdateCamera();
    }

    private static void PlayWeaponFireSound()
    {
        // Placeholder hook for future rifle audio.
    }

    private static void PlayShieldHitSound()
    {
        // Placeholder hook for future shield impact audio.
    }

    private Vector3 GetMovementInput()
    {
        Vector3 input = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) input += FlatForward;
        if (Raylib.IsKeyDown(KeyboardKey.S)) input -= FlatForward;
        if (Raylib.IsKeyDown(KeyboardKey.D)) input += Right;
        if (Raylib.IsKeyDown(KeyboardKey.A)) input -= Right;
        return input.LengthSquared() > 1f ? Vector3.Normalize(input) : input;
    }

    private void MoveAndCollide(Level level, float deltaTime)
    {
        grounded = false;

        MoveHorizontalAxis(level, velocity.X * deltaTime, Axis.X);
        MoveHorizontalAxis(level, velocity.Z * deltaTime, Axis.Z);
        MoveVertical(level, velocity.Y * deltaTime);

        position = level.ClampToArena(position, Radius);
    }

    private void MoveHorizontalAxis(Level level, float delta, Axis axis)
    {
        if (delta == 0f)
        {
            return;
        }

        Vector3 previousPosition = position;
        if (axis == Axis.X)
        {
            position.X += delta;
        }
        else
        {
            position.Z += delta;
        }

        position = level.ClampToArena(position, Radius);

        foreach (BoundingBox box in level.CollisionBoxes)
        {
            if (!ShouldResolveHorizontalCollision(previousPosition, position, box, axis, delta))
            {
                continue;
            }

            if (axis == Axis.X)
            {
                position.X = delta > 0f ? box.Min.X - Radius : box.Max.X + Radius;
                velocity.X = 0f;
            }
            else
            {
                position.Z = delta > 0f ? box.Min.Z - Radius : box.Max.Z + Radius;
                velocity.Z = 0f;
            }

            position = level.ClampToArena(position, Radius);
        }
    }

    private void MoveVertical(Level level, float delta)
    {
        float previousY = position.Y;
        position.Y += delta;

        foreach (BoundingBox box in level.CollisionBoxes)
        {
            if (!Raylib.CheckCollisionBoxes(CreatePlayerBox(position), box))
            {
                continue;
            }

            if (delta <= 0f && previousY >= box.Max.Y - GroundSnapTolerance)
            {
                position.Y = box.Max.Y;
                velocity.Y = 0f;
                grounded = true;
            }
            else if (delta > 0f && previousY + Height <= box.Min.Y + GroundSnapTolerance)
            {
                position.Y = box.Min.Y - Height;
                velocity.Y = 0f;
            }
        }

        if (position.Y <= 0f)
        {
            position.Y = 0f;
            velocity.Y = 0f;
            grounded = true;
        }
    }

    private bool ShouldResolveHorizontalCollision(Vector3 previousPosition, Vector3 currentPosition, BoundingBox box, Axis axis, float delta)
    {
        if (!HasHorizontalOverlapOnOtherAxis(currentPosition, box, axis) || !HasSideVerticalOverlap(currentPosition, box))
        {
            return false;
        }

        return axis == Axis.X
            ? CrossedXFace(previousPosition, currentPosition, box, delta)
            : CrossedZFace(previousPosition, currentPosition, box, delta);
    }

    private static bool HasSideVerticalOverlap(Vector3 feetPosition, BoundingBox box)
    {
        float playerBottom = feetPosition.Y;
        float playerTop = feetPosition.Y + Height;

        return playerBottom < box.Max.Y - CollisionSkin && playerTop > box.Min.Y + CollisionSkin;
    }

    private static bool HasHorizontalOverlapOnOtherAxis(Vector3 feetPosition, BoundingBox box, Axis axis)
    {
        if (axis == Axis.X)
        {
            return feetPosition.Z + Radius > box.Min.Z + CollisionSkin && feetPosition.Z - Radius < box.Max.Z - CollisionSkin;
        }

        return feetPosition.X + Radius > box.Min.X + CollisionSkin && feetPosition.X - Radius < box.Max.X - CollisionSkin;
    }

    private static bool CrossedXFace(Vector3 previousPosition, Vector3 currentPosition, BoundingBox box, float delta)
    {
        return delta > 0f
            ? previousPosition.X + Radius <= box.Min.X + CollisionSkin && currentPosition.X + Radius > box.Min.X
            : previousPosition.X - Radius >= box.Max.X - CollisionSkin && currentPosition.X - Radius < box.Max.X;
    }

    private static bool CrossedZFace(Vector3 previousPosition, Vector3 currentPosition, BoundingBox box, float delta)
    {
        return delta > 0f
            ? previousPosition.Z + Radius <= box.Min.Z + CollisionSkin && currentPosition.Z + Radius > box.Min.Z
            : previousPosition.Z - Radius >= box.Max.Z - CollisionSkin && currentPosition.Z - Radius < box.Max.Z;
    }

    private static BoundingBox CreatePlayerBox(Vector3 feetPosition)
    {
        Vector3 min = new(feetPosition.X - Radius, feetPosition.Y, feetPosition.Z - Radius);
        Vector3 max = new(feetPosition.X + Radius, feetPosition.Y + Height, feetPosition.Z + Radius);
        return new BoundingBox(min, max);
    }

    private Vector3 GetCameraPosition() => position + new Vector3(0f, EyeHeight, 0f);

    private void UpdateCamera()
    {
        Vector3 cameraPosition = GetCameraPosition();
        Camera = new Camera3D(cameraPosition, cameraPosition + Forward, Vector3.UnitY, Camera.FovY, Camera.Projection);
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + MathF.Sign(target - current) * maxDelta;
    }

    private enum Axis
    {
        X,
        Z
    }
}
