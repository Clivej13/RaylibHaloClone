using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Hud
{
    private const int Padding = 16;
    private const float HitMarkerDuration = 0.12f;

    private float hitMarkerRemaining;

    public void Reset()
    {
        hitMarkerRemaining = 0f;
    }

    public void UpdateHitMarker(bool hit, float deltaTime)
    {
        hitMarkerRemaining = hit ? HitMarkerDuration : MathF.Max(0f, hitMarkerRemaining - deltaTime);
    }

    public void Render(Player player, int livingEnemies, MatchState matchState)
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        int centerX = screenWidth / 2;
        int centerY = screenHeight / 2;

        Raylib.DrawLine(centerX - 10, centerY, centerX - 3, centerY, Color.White);
        Raylib.DrawLine(centerX + 3, centerY, centerX + 10, centerY, Color.White);
        Raylib.DrawLine(centerX, centerY - 10, centerX, centerY - 3, Color.White);
        Raylib.DrawLine(centerX, centerY + 3, centerX, centerY + 10, Color.White);

        if (hitMarkerRemaining > 0f)
        {
            RenderHitMarker(centerX, centerY);
        }

        Raylib.DrawFPS(Padding, Padding);
        Raylib.DrawText($"Position: {player.Position.X,6:0.00}, {player.Position.Y,5:0.00}, {player.Position.Z,6:0.00}", Padding, Padding + 32, 20, Color.RayWhite);
        Raylib.DrawText($"Speed: {player.CurrentHorizontalSpeed:0.00} m/s", Padding, Padding + 58, 20, Color.RayWhite);
        Raylib.DrawText($"Weapon: {player.CurrentWeapon.Name}", Padding, Padding + 84, 20, Color.RayWhite);
        Raylib.DrawText($"Ammo: {player.CurrentWeapon.MagazineAmmo}/{player.CurrentWeapon.MagazineSize} | Reserve: {player.CurrentWeapon.ReserveAmmo}", Padding, Padding + 110, 20, Color.RayWhite);
        Raylib.DrawText($"Shield: {player.Shield:0} | Health: {player.Health:0}", Padding, Padding + 136, 20, player.IsAlive ? Color.SkyBlue : Color.Red);
        Raylib.DrawText($"Targets: {livingEnemies}", Padding, Padding + 162, 20, Color.RayWhite);

        if (player.CurrentWeapon.IsReloading && matchState == MatchState.Playing)
        {
            Raylib.DrawText("RELOADING", centerX - 58, centerY + 34, 22, Color.Gold);
        }

        if (matchState != MatchState.Playing)
        {
            RenderMatchMessage(matchState, centerX, centerY);
        }

        Raylib.DrawText("WASD Move | Shift Sprint | Space Jump | Mouse Look | LMB Fire | R Reload", Padding, screenHeight - 34, 20, Color.LightGray);
    }

    private static void RenderMatchMessage(MatchState matchState, int centerX, int centerY)
    {
        string message = matchState == MatchState.Victory
            ? "VICTORY - Press Enter to Restart"
            : "DEFEATED - Press Enter to Restart";
        Color color = matchState == MatchState.Victory ? Color.Gold : Color.Red;
        int fontSize = 34;
        int textWidth = Raylib.MeasureText(message, fontSize);
        Raylib.DrawRectangle(centerX - (textWidth / 2) - 18, centerY - 90, textWidth + 36, 58, new Color(0, 0, 0, 180));
        Raylib.DrawText(message, centerX - textWidth / 2, centerY - 78, fontSize, color);
    }

    private static void RenderHitMarker(int centerX, int centerY)
    {
        const int inner = 14;
        const int outer = 24;
        Raylib.DrawLine(centerX - outer, centerY - outer, centerX - inner, centerY - inner, Color.Gold);
        Raylib.DrawLine(centerX + inner, centerY - inner, centerX + outer, centerY - outer, Color.Gold);
        Raylib.DrawLine(centerX - outer, centerY + outer, centerX - inner, centerY + inner, Color.Gold);
        Raylib.DrawLine(centerX + inner, centerY + inner, centerX + outer, centerY + outer, Color.Gold);
    }
}
