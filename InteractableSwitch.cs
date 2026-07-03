using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public enum SwitchType
{
    ExitActivation,
    DoorOpen,
    PoweredDoor,
    Lights,
    BoardingPodDoor,
    LinkedDoor
}

public enum SwitchState
{
    Off,
    On,
    Used
}

public sealed class InteractableSwitch : IInteractable
{
    public InteractableSwitch(SwitchType type, Vector3 position, Vector3 size, Vector3? faceDirection = null, string? targetDoorName = null)
    {
        Type = type;
        Position = position;
        Size = size;
        FaceDirection = MathUtils.SafeNormalize(faceDirection ?? -Vector3.UnitZ, -Vector3.UnitZ);
        TargetDoorName = targetDoorName;
    }

    public SwitchType Type { get; }
    public Vector3 Position { get; }
    public Vector3 Size { get; }
    public Vector3 FaceDirection { get; }
    public string? TargetDoorName { get; }
    public SwitchState State { get; private set; } = SwitchState.Off;
    public bool CanUse => State == SwitchState.Off;
    public float Radius => MathF.Max(Size.X, MathF.Max(Size.Y, Size.Z)) * 0.5f;
    public string DisplayName => Type switch
    {
        SwitchType.ExitActivation => "extraction switch",
        SwitchType.DoorOpen => "security door switch",
        SwitchType.PoweredDoor => "powered door switch",
        SwitchType.Lights => "light switch",
        SwitchType.BoardingPodDoor => "boarding pod door switch",
        SwitchType.LinkedDoor => TargetDoorName is null ? "door switch" : $"{TargetDoorName} switch",
        _ => "switch"
    };
    public bool IsActive => CanUse;
    public BoundingBox Bounds => Level.ToBoundingBox(Position, Size);

    public string GetPrompt(Player player) => $"Press E to activate {DisplayName}";
    public bool CanInteract(Player player) => CanUse;
    public void Interact(Player player, Level level) => level.ActivateSwitch(this);

    public void Render()
    {
        Color bodyColor = State == SwitchState.Off ? new Color(76, 82, 92, 255) : new Color(48, 120, 74, 255);
        Color buttonColor = State == SwitchState.Off ? Color.Red : Color.Green;
        Raylib.DrawCubeV(Position, Size, bodyColor);
        Raylib.DrawCubeWiresV(Position, Size, Color.Black);

        Vector3 buttonSize = MathF.Abs(FaceDirection.X) > MathF.Abs(FaceDirection.Z)
            ? new Vector3(0.12f, 0.22f, 0.32f)
            : new Vector3(0.32f, 0.22f, 0.12f);
        Raylib.DrawCubeV(Position + FaceDirection * 0.27f + new Vector3(0f, 0.15f, 0f), buttonSize, buttonColor);
    }

    public void Activate() => State = SwitchState.Used;
    public void Reset() => State = SwitchState.Off;
}


