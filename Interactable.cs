using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public interface IInteractable
{
    Vector3 Position { get; }
    float Radius { get; }
    string DisplayName { get; }
    bool IsActive { get; }
    BoundingBox Bounds { get; }
    string GetPrompt(Player player);
    bool CanInteract(Player player);
    void Interact(Player player, Level level);
    void Render();
}

public abstract class WorldInteractable : IInteractable
{
    protected WorldInteractable(string displayName, Vector3 position, Vector3 size)
    {
        DisplayName = displayName;
        Position = position;
        Size = size;
        Radius = MathF.Max(size.X, MathF.Max(size.Y, size.Z)) * 0.5f;
    }

    public Vector3 Position { get; protected set; }
    public Vector3 Size { get; }
    public float Radius { get; }
    public string DisplayName { get; }
    public bool IsActive { get; protected set; } = true;
    public BoundingBox Bounds => Level.ToBoundingBox(Position, Size);

    public abstract string GetPrompt(Player player);
    public abstract bool CanInteract(Player player);
    public abstract void Interact(Player player, Level level);
    public abstract void Render();
}

public sealed class HealthPackPickup : WorldInteractable
{
    private readonly float healAmount;

    public HealthPackPickup(Vector3 position, float healAmount = 45f)
        : base("Health Pack", position, new Vector3(0.75f, 0.35f, 0.75f))
    {
        this.healAmount = healAmount;
    }

    public override string GetPrompt(Player player) => player.Health < Player.MaxHealth ? $"Press E to pick up {DisplayName}" : "Health full";
    public override bool CanInteract(Player player) => IsActive && player.Health < Player.MaxHealth;

    public override void Interact(Player player, Level level)
    {
        if (!CanInteract(player)) return;
        player.Heal(healAmount);
        IsActive = false;
    }

    public override void Render()
    {
        if (!IsActive) return;
        Raylib.DrawCubeV(Position, Size, Color.Green);
        Raylib.DrawCubeWiresV(Position, Size, Color.White);
        Raylib.DrawCubeV(Position + new Vector3(0f, Size.Y * 0.55f, 0f), new Vector3(0.5f, 0.06f, 0.16f), Color.RayWhite);
        Raylib.DrawCubeV(Position + new Vector3(0f, Size.Y * 0.55f, 0f), new Vector3(0.16f, 0.06f, 0.5f), Color.RayWhite);
    }
}

public sealed class WeaponPickup : WorldInteractable
{
    public WeaponPickup(Vector3 position, Weapon weapon)
        : base(weapon.Name, position, new Vector3(1.05f, 0.25f, 0.35f))
    {
        Weapon = weapon;
    }

    public Weapon Weapon { get; }
    public override string GetPrompt(Player player) => $"Press E to pick up {DisplayName}";
    public override bool CanInteract(Player player) => IsActive;

    public override void Interact(Player player, Level level)
    {
        if (!CanInteract(player)) return;
        Weapon? dropped = player.PickUpWeapon(Weapon);
        IsActive = false;
        level.DropWeapon(player, dropped);
    }

    public override void Render()
    {
        if (!IsActive) return;
        Raylib.DrawCubeV(Position, Size, new Color(58, 92, 126, 255));
        Raylib.DrawCubeWiresV(Position, Size, Color.SkyBlue);
        Raylib.DrawCylinder(Position + new Vector3(0.58f, 0f, 0f), 0.06f, 0.06f, 0.45f, 10, Color.DarkGray);
    }
}
