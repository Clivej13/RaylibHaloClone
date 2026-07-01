using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Hud
{
    private const int Padding = 16;

    public void Render(Player player)
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        int centerX = screenWidth / 2;
        int centerY = screenHeight / 2;

        Raylib.DrawLine(centerX - 10, centerY, centerX - 3, centerY, Color.White);
        Raylib.DrawLine(centerX + 3, centerY, centerX + 10, centerY, Color.White);
        Raylib.DrawLine(centerX, centerY - 10, centerX, centerY - 3, Color.White);
        Raylib.DrawLine(centerX, centerY + 3, centerX, centerY + 10, Color.White);

        Raylib.DrawFPS(Padding, Padding);
        Raylib.DrawText($"Position: {player.Position.X,6:0.00}, {player.Position.Y,5:0.00}, {player.Position.Z,6:0.00}", Padding, Padding + 32, 20, Color.RayWhite);
        Raylib.DrawText($"Speed: {player.CurrentHorizontalSpeed:0.00} m/s", Padding, Padding + 58, 20, Color.RayWhite);
        Raylib.DrawText("WASD Move | Shift Sprint | Space Jump | Mouse Look", Padding, screenHeight - 34, 20, Color.LightGray);
    }
}
