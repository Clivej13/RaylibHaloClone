namespace RaylibHaloClone;

public enum EquippedSlot
{
    Primary,
    Secondary,
    Sidearm
}

public enum LethalType
{
    None,
    FragGrenade
}

public enum SpecialType
{
    None,
    EquipmentModule
}

public sealed class Equipment
{
    public const int MaxMedkits = 3;
    public const int MaxLethals = 2;
    public const int MaxSpecials = 1;
    public Weapon? PrimaryWeapon { get; set; }
    public Weapon? SecondaryWeapon { get; set; }
    public Weapon? Sidearm { get; set; }
    public EquippedSlot EquippedSlot { get; private set; } = EquippedSlot.Primary;
    public int MedkitCount { get; private set; }
    public int LethalCount { get; private set; }
    public LethalType LethalType { get; private set; } = LethalType.None;
    public int SpecialCount { get; private set; }
    public SpecialType SpecialType { get; private set; } = SpecialType.None;

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

    public bool AddMedkit()
    {
        if (MedkitCount >= MaxMedkits)
        {
            return false;
        }

        MedkitCount++;
        return true;
    }

    public bool UseMedkit()
    {
        if (MedkitCount <= 0)
        {
            return false;
        }

        MedkitCount--;
        return true;
    }

    public bool AddLethal(LethalType lethalType, int amount = 1)
    {
        if (lethalType == LethalType.None || amount <= 0 || LethalCount >= MaxLethals)
        {
            return false;
        }

        LethalType = lethalType;
        LethalCount = Math.Min(MaxLethals, LethalCount + amount);
        return true;
    }

    public bool UseLethal()
    {
        if (LethalCount <= 0 || LethalType == LethalType.None)
        {
            return false;
        }

        LethalCount--;
        if (LethalCount == 0)
        {
            LethalType = LethalType.None;
        }

        return true;
    }

    public bool AddSpecial(SpecialType specialType, int amount = 1)
    {
        if (specialType == SpecialType.None || amount <= 0 || SpecialCount >= MaxSpecials)
        {
            return false;
        }

        SpecialType = specialType;
        SpecialCount = Math.Min(MaxSpecials, SpecialCount + amount);
        return true;
    }

    public bool UseSpecial()
    {
        if (SpecialCount <= 0 || SpecialType == SpecialType.None)
        {
            return false;
        }

        SpecialCount--;
        if (SpecialCount == 0)
        {
            SpecialType = SpecialType.None;
        }

        return true;
    }

    public void ResetToDefaultMissionLoadout()
    {
        PrimaryWeapon = Weapon.CreateRifle();
        SecondaryWeapon = null;
        Sidearm = Weapon.CreatePistol();
        EquippedSlot = EquippedSlot.Primary;
        MedkitCount = 1;
        LethalCount = 0;
        LethalType = LethalType.None;
        SpecialCount = 0;
        SpecialType = SpecialType.None;
    }
}
