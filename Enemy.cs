using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Enemy
{
    private static readonly Color BodyColor = new(170, 72, 72, 255);
    private static readonly Color HeadColor = new(210, 130, 96, 255);
    private static readonly Color WireColor = new(70, 24, 24, 255);

    private const float Width = 0.9f;
    private const float Height = 2.1f;
    private const float Depth = 0.9f;
    private const float DamageFlashTime = 0.12f;

    private float damageFlashRemaining;

    public Enemy(Vector3 position, float health = 100f)
    {
        Position = position;
        Health = health;
    }

    public Vector3 Position { get; }
    public float Health { get; private set; }
    public bool IsAlive => Health > 0f;
    private bool IsDamageFlashing => damageFlashRemaining > 0f;

    public BoundingBox Hitbox
    {
        get
        {
            Vector3 halfExtents = new(Width / 2f, Height / 2f, Depth / 2f);
            Vector3 center = Position + new Vector3(0f, Height / 2f, 0f);
            return new BoundingBox(center - halfExtents, center + halfExtents);
        }
    }

    public void Update(float deltaTime)
    {
        damageFlashRemaining = MathF.Max(0f, damageFlashRemaining - deltaTime);
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
    }
}
