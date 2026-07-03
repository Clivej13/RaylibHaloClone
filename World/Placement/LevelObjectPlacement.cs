using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed partial class Level
{
    public void DropWeapon(Player player, Weapon? weapon)
    {
        if (weapon is null)
        {
            return;
        }

        Vector3 flatLook = MathUtils.Flatten(player.LookDirection);
        if (flatLook.LengthSquared() < 0.001f) flatLook = Vector3.UnitZ;
        Vector3 preferred = player.Position + Vector3.Normalize(flatLook) * 1.35f + new Vector3(0f, 0.35f, 0f);
        Vector3 clearPosition = FindClearObjectPosition(preferred, new Vector3(1.05f, 0.25f, 0.35f));
        AddWorldObject(new WeaponPickup(clearPosition, weapon));
    }

    private void AddInitialPickups()
    {
        AddWorldObject(new HealthPackPickup(new Vector3(-5f, 0.25f, 6f)));
        AddWorldObject(new WeaponPickup(new Vector3(5f, 0.35f, 6f), Weapon.CreateRifle()));
        AddWorldObject(new WeaponPickup(new Vector3(7f, 0.35f, 8f), Weapon.CreateShotgun()));
        AddWorldObject(new WeaponPickup(new Vector3(3f, 0.35f, 8f), Weapon.CreatePistol()));
    }

    private void AddWorldObject(WorldInteractable worldObject)
    {
        Vector3 clearPosition = FindClearObjectPosition(worldObject.Position, worldObject.Size);
        if (clearPosition != worldObject.Position)
        {
            worldObject = worldObject switch
            {
                HealthPackPickup => new HealthPackPickup(clearPosition),
                WeaponPickup weaponPickup => new WeaponPickup(clearPosition, weaponPickup.Weapon),
                _ => worldObject
            };
        }

        worldObjects.Add(worldObject);
        AddLevelObject(worldObject);
    }

    private Vector3 FindClearObjectPosition(Vector3 preferredPosition, Vector3 size)
    {
        Vector3[] offsets =
        [
            Vector3.Zero, new Vector3(0.8f, 0f, 0f), new Vector3(-0.8f, 0f, 0f),
            new Vector3(0f, 0f, 0.8f), new Vector3(0f, 0f, -0.8f),
            new Vector3(1.1f, 0f, 1.1f), new Vector3(-1.1f, 0f, 1.1f),
            new Vector3(1.1f, 0f, -1.1f), new Vector3(-1.1f, 0f, -1.1f)
        ];

        foreach (Vector3 offset in offsets)
        {
            Vector3 candidate = ClampToArena(preferredPosition + offset, MathF.Max(size.X, size.Z) * 0.5f);
            BoundingBox candidateBox = ToBoundingBox(candidate, size);
            if (CollisionBoxes.Any(box => Raylib.CheckCollisionBoxes(candidateBox, box))) continue;
            if (Interactables.Any(obj => Raylib.CheckCollisionBoxes(candidateBox, obj.Bounds))) continue;
            return candidate;
        }

        return ClampToArena(preferredPosition, MathF.Max(size.X, size.Z) * 0.5f);
    }
}
