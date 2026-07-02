using Raylib_cs;

namespace RaylibHaloClone;

public sealed class Hud
{
    private const int Padding = 16;
    private const float HitMarkerDuration = 0.18f;
    private const float ShieldHitFlashDuration = 0.22f;
    private const float HealthDamageFlashDuration = 0.28f;
    private const float ShieldBreakDuration = 0.65f;
    private const float ShieldRechargePulseDuration = 0.45f;

    private float hitMarkerRemaining;
    private float shieldHitFlashRemaining;
    private float healthDamageFlashRemaining;
    private float shieldBreakRemaining;
    private float shieldRechargePulseRemaining;
    private float previousShield = Player.MaxShield;
    private float previousHealth = Player.MaxHealth;

    public void Reset(Player player)
    {
        hitMarkerRemaining = 0f;
        shieldHitFlashRemaining = 0f;
        healthDamageFlashRemaining = 0f;
        shieldBreakRemaining = 0f;
        shieldRechargePulseRemaining = 0f;
        previousShield = player.Shield;
        previousHealth = player.Health;
    }

    public void Update(Player player, bool hit, float deltaTime)
    {
        hitMarkerRemaining = hit ? HitMarkerDuration : MathF.Max(0f, hitMarkerRemaining - deltaTime);
        shieldHitFlashRemaining = MathF.Max(0f, shieldHitFlashRemaining - deltaTime);
        healthDamageFlashRemaining = MathF.Max(0f, healthDamageFlashRemaining - deltaTime);
        shieldBreakRemaining = MathF.Max(0f, shieldBreakRemaining - deltaTime);
        shieldRechargePulseRemaining = MathF.Max(0f, shieldRechargePulseRemaining - deltaTime);

        if (player.Shield < previousShield)
        {
            shieldHitFlashRemaining = ShieldHitFlashDuration;
            if (previousShield > 0f && player.Shield <= 0f)
            {
                shieldBreakRemaining = ShieldBreakDuration;
            }
        }
        else if (player.Shield > previousShield && player.Shield < Player.MaxShield)
        {
            shieldRechargePulseRemaining = ShieldRechargePulseDuration;
        }

        if (player.Health < previousHealth)
        {
            healthDamageFlashRemaining = HealthDamageFlashDuration;
        }

        previousShield = player.Shield;
        previousHealth = player.Health;
    }

    public void Render(Player player, int livingEnemies, MatchState matchState)
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        int centerX = screenWidth / 2;
        int centerY = screenHeight / 2;

        RenderDamageOverlays(screenWidth, screenHeight);
        RenderCrosshair(centerX, centerY);

        if (hitMarkerRemaining > 0f)
        {
            RenderHitMarker(centerX, centerY);
        }

        Raylib.DrawFPS(Padding, Padding);
        Raylib.DrawText($"Position: {player.Position.X,6:0.00}, {player.Position.Y,5:0.00}, {player.Position.Z,6:0.00}", Padding, Padding + 32, 20, Color.RayWhite);
        Raylib.DrawText($"Speed: {player.CurrentHorizontalSpeed:0.00} m/s", Padding, Padding + 58, 20, Color.RayWhite);
        Raylib.DrawText($"Weapon: {player.CurrentWeapon.Name}", Padding, Padding + 84, 20, Color.RayWhite);
        Raylib.DrawText($"Ammo: {player.CurrentWeapon.MagazineAmmo}/{player.CurrentWeapon.MagazineSize} | Reserve: {player.CurrentWeapon.ReserveAmmo}", Padding, Padding + 110, 20, Color.RayWhite);
        RenderStatusBars(player, Padding, Padding + 140);
        Raylib.DrawText($"Targets: {livingEnemies}", Padding, Padding + 224, 20, Color.RayWhite);

        if (player.CurrentWeapon.IsReloading && matchState == MatchState.Playing)
        {
            Raylib.DrawText("RELOADING", centerX - 58, centerY + 34, 22, Color.Gold);
        }

        if (matchState != MatchState.Playing)
        {
            RenderMatchMessage(matchState, centerX, centerY, livingEnemies);
        }

        Raylib.DrawText("WASD Move | Shift Sprint | Space Jump | Mouse Look | LMB Fire | R Reload", Padding, screenHeight - 34, 20, Color.LightGray);
    }

    private void RenderStatusBars(Player player, int x, int y)
    {
        RenderBar("SHIELD", player.Shield / Player.MaxShield, x, y, 260, 22, new Color(72, 194, 255, 255), shieldHitFlashRemaining, shieldRechargePulseRemaining);
        RenderBar("HEALTH", player.Health / Player.MaxHealth, x, y + 42, 260, 22, new Color(96, 236, 126, 255), healthDamageFlashRemaining, 0f);
    }

    private static void RenderBar(string label, float percent, int x, int y, int width, int height, Color fillColor, float damageFlash, float rechargePulse)
    {
        percent = MathUtils.Clamp(percent, 0f, 1f);
        Color frameColor = damageFlash > 0f ? Color.White : new Color(120, 132, 150, 255);
        Color glowColor = rechargePulse > 0f ? new Color(126, 235, 255, 85) : new Color(0, 0, 0, 120);

        Raylib.DrawText(label, x, y - 20, 16, Color.LightGray);
        Raylib.DrawRectangle(x - 4, y - 4, width + 8, height + 8, glowColor);
        Raylib.DrawRectangle(x, y, width, height, new Color(12, 18, 28, 220));
        Raylib.DrawRectangle(x, y, (int)(width * percent), height, fillColor);
        Raylib.DrawRectangleLines(x, y, width, height, frameColor);

        int segments = 10;
        for (int i = 1; i < segments; i++)
        {
            int tickX = x + (width * i / segments);
            Raylib.DrawLine(tickX, y + 3, tickX, y + height - 3, new Color(255, 255, 255, 55));
        }
    }

    private void RenderDamageOverlays(int screenWidth, int screenHeight)
    {
        if (shieldHitFlashRemaining > 0f)
        {
            byte alpha = (byte)(85f * (shieldHitFlashRemaining / ShieldHitFlashDuration));
            Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(70, 185, 255, alpha));
        }

        if (healthDamageFlashRemaining > 0f)
        {
            byte alpha = (byte)(105f * (healthDamageFlashRemaining / HealthDamageFlashDuration));
            Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(210, 35, 35, alpha));
        }

        if (shieldBreakRemaining > 0f)
        {
            float t = shieldBreakRemaining / ShieldBreakDuration;
            byte alpha = (byte)(155f * t);
            Raylib.DrawCircleLines(screenWidth / 2, screenHeight / 2, 250f + (1f - t) * 180f, new Color(122, 220, 255, alpha));
            Raylib.DrawText("SHIELD DOWN", (screenWidth / 2) - 80, (screenHeight / 2) + 86, 22, new Color(122, 220, 255, alpha));
        }
    }

    private static void RenderCrosshair(int centerX, int centerY)
    {
        Raylib.DrawLine(centerX - 12, centerY, centerX - 4, centerY, Color.RayWhite);
        Raylib.DrawLine(centerX + 4, centerY, centerX + 12, centerY, Color.RayWhite);
        Raylib.DrawLine(centerX, centerY - 12, centerX, centerY - 4, Color.RayWhite);
        Raylib.DrawLine(centerX, centerY + 4, centerX, centerY + 12, Color.RayWhite);
        Raylib.DrawCircle(centerX, centerY, 1.5f, Color.RayWhite);
    }

    private static void RenderMatchMessage(MatchState matchState, int centerX, int centerY, int livingEnemies)
    {
        bool victory = matchState == MatchState.Victory;
        string title = victory ? "VICTORY" : "DEFEATED";
        string subtitle = victory ? "All targets eliminated" : "Armor systems offline";
        string detail = victory ? "Arena secure" : $"Targets remaining: {livingEnemies}";
        Color accent = victory ? Color.Gold : new Color(255, 80, 80, 255);

        const int panelWidth = 520;
        const int panelHeight = 190;
        int panelX = centerX - panelWidth / 2;
        int panelY = centerY - 130;
        Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, new Color(4, 8, 14, 225));
        Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, accent);
        Raylib.DrawRectangle(panelX, panelY, panelWidth, 5, accent);

        int titleWidth = Raylib.MeasureText(title, 48);
        int subtitleWidth = Raylib.MeasureText(subtitle, 22);
        int detailWidth = Raylib.MeasureText(detail, 20);
        const string restart = "Press Enter to Restart";
        int restartWidth = Raylib.MeasureText(restart, 20);

        Raylib.DrawText(title, centerX - titleWidth / 2, panelY + 26, 48, accent);
        Raylib.DrawText(subtitle, centerX - subtitleWidth / 2, panelY + 86, 22, Color.RayWhite);
        Raylib.DrawText(detail, centerX - detailWidth / 2, panelY + 116, 20, Color.LightGray);
        Raylib.DrawText(restart, centerX - restartWidth / 2, panelY + 150, 20, Color.SkyBlue);
    }

    private void RenderHitMarker(int centerX, int centerY)
    {
        float t = hitMarkerRemaining / HitMarkerDuration;
        int inner = 9 + (int)(5f * (1f - t));
        int outer = 24 + (int)(7f * (1f - t));
        byte alpha = (byte)(255f * t);
        Color markerColor = new(255, 230, 118, alpha);
        Raylib.DrawLine(centerX - outer, centerY - outer, centerX - inner, centerY - inner, markerColor);
        Raylib.DrawLine(centerX + inner, centerY - inner, centerX + outer, centerY - outer, markerColor);
        Raylib.DrawLine(centerX - outer, centerY + outer, centerX - inner, centerY + inner, markerColor);
        Raylib.DrawLine(centerX + inner, centerY + inner, centerX + outer, centerY + outer, markerColor);
        Raylib.DrawCircleLines(centerX, centerY, 18f, new Color(255, 255, 255, (byte)(110f * t)));
    }
}
