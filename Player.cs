using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Player
{
    public const float EyeHeight = 1.75f;

    private const float Radius = 0.35f;
    private const float Height = 1.9f;
    private const float GroundSnapTolerance = 0.05f;
    private const float WalkSpeed = 6f;
    private const float SprintSpeed = 10f;
    private const float Acceleration = 14f;
    private const float AirAcceleration = 5f;
    private const float JumpVelocity = 7.5f;
    private const float Gravity = 22f;
    private const float MaxLookPitch = 88f;
    private const float MouseSensitivity = 0.10f;

    private Vector3 position;
    private Vector3 velocity;
    private float yaw;
    private float pitch;
    private bool grounded;

    public Player(Vector3 spawnPosition)
    {
        position = spawnPosition;
        Vector3 cameraPosition = GetCameraPosition();
        Camera = new Camera3D(cameraPosition, cameraPosition + Forward, Vector3.UnitY, 75f, CameraProjection.Perspective);
    }

    public Camera3D Camera { get; private set; }
    public Vector3 Position => position;
    public float CurrentHorizontalSpeed => MathUtils.Flatten(velocity).Length();

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

    public void UpdateLook(Vector2 mouseDelta)
    {
        yaw -= mouseDelta.X * MouseSensitivity;
        pitch = MathUtils.Clamp(pitch - mouseDelta.Y * MouseSensitivity, -MaxLookPitch, MaxLookPitch);
        UpdateCamera();
    }

    public void FixedUpdate(Level level, float deltaTime)
    {
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
            if (!Raylib.CheckCollisionBoxes(CreatePlayerBox(position), box))
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

    private BoundingBox CreatePlayerBox(Vector3 feetPosition)
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
