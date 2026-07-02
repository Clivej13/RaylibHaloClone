namespace RaylibHaloClone;

public enum EquippedSlot
{
    Primary,
    Secondary,
    Sidearm
}

public sealed class Equipment
{
    public Weapon? PrimaryWeapon { get; set; }
    public Weapon? SecondaryWeapon { get; set; }
    public Weapon? Sidearm { get; set; }
    public EquippedSlot EquippedSlot { get; private set; } = EquippedSlot.Primary;

    public Weapon? CurrentWeapon => GetWeapon(EquippedSlot);

    public void EquipSlot(EquippedSlot slot)
    {
        EquippedSlot = slot;
    }

    public static EquippedSlot GetSlotForCategory(WeaponCategory category) => category switch
    {
        WeaponCategory.Primary => EquippedSlot.Primary,
        WeaponCategory.Secondary => EquippedSlot.Secondary,
        WeaponCategory.Sidearm => EquippedSlot.Sidearm,
        _ => EquippedSlot.Primary
    };

    public Weapon? GetWeapon(EquippedSlot slot) => slot switch
    {
        EquippedSlot.Primary => PrimaryWeapon,
        EquippedSlot.Secondary => SecondaryWeapon,
        EquippedSlot.Sidearm => Sidearm,
        _ => null
    };

    public Weapon? SetWeapon(EquippedSlot slot, Weapon? weapon)
    {
        Weapon? previous = GetWeapon(slot);
        switch (slot)
        {
            case EquippedSlot.Primary:
                PrimaryWeapon = weapon;
                break;
            case EquippedSlot.Secondary:
                SecondaryWeapon = weapon;
                break;
            case EquippedSlot.Sidearm:
                Sidearm = weapon;
                break;
        }

        return previous;
    }

    public Weapon? RemoveCurrentWeapon() => SetWeapon(EquippedSlot, null);

    public void ResetToDefaultMissionLoadout()
    {
        PrimaryWeapon = Weapon.CreateRifle();
        SecondaryWeapon = null;
        Sidearm = Weapon.CreatePistol();
        EquippedSlot = EquippedSlot.Primary;
    }
}
