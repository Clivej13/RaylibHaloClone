using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Hud
{
    private const int Padding = 16;
    private const float HitMarkerDuration = 0.12f;

    private float hitMarkerRemaining;

    public void UpdateHitMarker(bool hit, float deltaTime)
    {
        hitMarkerRemaining = hit ? HitMarkerDuration : MathF.Max(0f, hitMarkerRemaining - deltaTime);
    }

    public void Render(Player player, int livingEnemies)
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
        Raylib.DrawText($"Targets: {livingEnemies}", Padding, Padding + 136, 20, Color.RayWhite);

        if (player.CurrentWeapon.IsReloading)
        {
            Raylib.DrawText("RELOADING", centerX - 58, centerY + 34, 22, Color.Gold);
        }

        Raylib.DrawText("WASD Move | Shift Sprint | Space Jump | Mouse Look | LMB Fire | R Reload", Padding, screenHeight - 34, 20, Color.LightGray);
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
