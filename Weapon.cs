using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public readonly record struct WeaponFireResult(bool Fired, bool Hit);

public sealed class Weapon
{
    private float cooldownRemaining;
    private float reloadRemaining;

    public Weapon(string name, float damage, float range, float fireCooldown, int magazineSize, int reserveAmmo, float reloadTime)
    {
        Name = name;
        Damage = damage;
        Range = range;
        FireCooldown = fireCooldown;
        MagazineSize = magazineSize;
        MagazineAmmo = magazineSize;
        ReserveAmmo = reserveAmmo;
        ReloadTime = reloadTime;
    }

    public string Name { get; }
    public float Damage { get; }
    public float Range { get; }
    public float FireCooldown { get; }
    public int MagazineSize { get; }
    public int MagazineAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    public float ReloadTime { get; }
    public bool IsReloading => reloadRemaining > 0f;
    public bool CanFire => cooldownRemaining <= 0f && !IsReloading && MagazineAmmo > 0;

    public static Weapon CreateRifle() => new("MA5B Rifle", 34f, 60f, 0.1f, 32, 160, 1.4f);

    public void Update(float deltaTime)
    {
        cooldownRemaining = MathF.Max(0f, cooldownRemaining - deltaTime);

        if (!IsReloading)
        {
            return;
        }

        reloadRemaining = MathF.Max(0f, reloadRemaining - deltaTime);
        if (reloadRemaining <= 0f)
        {
            FinishReload();
        }
    }

    public void Reload()
    {
        if (IsReloading || MagazineAmmo >= MagazineSize || ReserveAmmo <= 0)
        {
            return;
        }

        reloadRemaining = ReloadTime;
    }

    public WeaponFireResult Fire(Vector3 origin, Vector3 direction, IReadOnlyList<Enemy> enemies)
    {
        if (!CanFire)
        {
            return new WeaponFireResult(false, false);
        }

        MagazineAmmo--;
        cooldownRemaining = FireCooldown;

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

        if (closestEnemy is null)
        {
            return new WeaponFireResult(true, false);
        }

        closestEnemy.TakeDamage(Damage);
        return new WeaponFireResult(true, true);
    }

    private void FinishReload()
    {
        int neededAmmo = MagazineSize - MagazineAmmo;
        int ammoToLoad = Math.Min(neededAmmo, ReserveAmmo);
        MagazineAmmo += ammoToLoad;
        ReserveAmmo -= ammoToLoad;
    }
}
