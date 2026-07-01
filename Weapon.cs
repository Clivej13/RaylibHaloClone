using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Weapon
{
    private float cooldownRemaining;

    public Weapon(string name, float damage, float range, float fireRate)
    {
        Name = name;
        Damage = damage;
        Range = range;
        FireRate = fireRate;
    }

    public string Name { get; }
    public float Damage { get; }
    public float Range { get; }
    public float FireRate { get; }
    public bool CanFire => cooldownRemaining <= 0f;

    private float SecondsPerShot => 1f / FireRate;

    public static Weapon CreateRifle() => new("MA5B Rifle", 34f, 60f, 8f);

    public void Update(float deltaTime)
    {
        cooldownRemaining = MathF.Max(0f, cooldownRemaining - deltaTime);
    }

    public bool Fire(Vector3 origin, Vector3 direction, IReadOnlyList<Enemy> enemies)
    {
        if (!CanFire)
        {
            return false;
        }

        cooldownRemaining = SecondsPerShot;

        Enemy? closestEnemy = null;
        float closestDistance = Range;
        Ray ray = new(origin, Vector3.Normalize(direction));

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            RayCollision collision = Raylib.GetRayCollisionBox(ray, enemy.Hitbox);
            if (collision.Hit && collision.Distance <= closestDistance)
            {
                closestDistance = collision.Distance;
                closestEnemy = enemy;
            }
        }

        closestEnemy?.TakeDamage(Damage);
        return true;
    }
}
