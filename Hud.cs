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

    public void Render(Player player, int livingEnemies, MatchState matchState, string objectiveText, string? interactionPrompt)
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

        RenderDebugInfo(player, livingEnemies, objectiveText);
        RenderPlayerStatus(player, screenWidth);
        RenderWeaponInfo(player, screenWidth, screenHeight);

        if (player.CurrentWeapon?.IsReloading == true && matchState == MatchState.Playing)
        {
            Raylib.DrawText("RELOADING", centerX - 58, centerY + 34, 22, Color.Gold);
        }

        if (!string.IsNullOrWhiteSpace(interactionPrompt) && matchState == MatchState.Playing)
        {
            int promptWidth = Raylib.MeasureText(interactionPrompt, 22);
            Raylib.DrawText(interactionPrompt, centerX - promptWidth / 2, centerY + 62, 22, Color.Gold);
        }

        if (matchState != MatchState.Playing)
        {
            RenderMatchMessage(matchState, centerX, centerY, livingEnemies);
        }

        Raylib.DrawText("WASD Move | Shift Sprint | Space Jump | Mouse Look | LMB Fire | R Reload | 1/2/3 Switch | E Interact | G Drop", Padding, screenHeight - 34, 20, Color.LightGray);
    }

    private static void RenderDebugInfo(Player player, int livingEnemies, string objectiveText)
    {
        Color objectiveColor = objectiveText.StartsWith("Objective complete", StringComparison.Ordinal) ? Color.SkyBlue : Color.RayWhite;

        Raylib.DrawFPS(Padding, Padding);
        Raylib.DrawText($"Position: {player.Position.X,6:0.00}, {player.Position.Y,5:0.00}, {player.Position.Z,6:0.00}", Padding, Padding + 32, 20, Color.RayWhite);
        Raylib.DrawText($"Speed: {player.CurrentHorizontalSpeed:0.00} m/s", Padding, Padding + 58, 20, Color.RayWhite);
        Raylib.DrawText($"Targets: {livingEnemies}", Padding, Padding + 84, 20, Color.RayWhite);
        Raylib.DrawText(objectiveText, Padding, Padding + 110, 20, objectiveColor);
    }

    private void RenderPlayerStatus(Player player, int screenWidth)
    {
        const int barWidth = 260;
        int x = (screenWidth - barWidth) / 2;
        int y = Padding + 24;

        RenderBar("SHIELD", player.Shield / Player.MaxShield, x, y, barWidth, 22, Rgba(72, 194, 255, 255), shieldHitFlashRemaining, shieldRechargePulseRemaining);
        RenderBar("HEALTH", player.Health / Player.MaxHealth, x, y + 42, barWidth, 22, Rgba(96, 236, 126, 255), healthDamageFlashRemaining, 0f);
    }

    private static void RenderWeaponInfo(Player player, int screenWidth, int screenHeight)
    {
        const int lineHeight = 26;
        const int fontSize = 20;
        int panelWidth = 300;
        int panelHeight = lineHeight * 5;
        int x = screenWidth - panelWidth - Padding;
        int y = screenHeight - panelHeight - Padding - 26;
        Weapon? weapon = player.CurrentWeapon;

        Raylib.DrawText($"Slot: {player.EquippedSlot}", x, y, fontSize, Color.LightGray);
        Raylib.DrawText($"Weapon: {weapon?.Name ?? "Unarmed"}", x, y + lineHeight, fontSize, weapon is null ? Color.Gold : Color.RayWhite);

        if (weapon is null)
        {
            Raylib.DrawText("Slot Status: Empty", x, y + lineHeight * 2, fontSize, Color.LightGray);
            return;
        }

        string reloadState = weapon.IsReloading ? "Reload: RELOADING" : "Reload: READY";
        Raylib.DrawText($"Category: {weapon.Category} | {(weapon.IsAutomatic ? "Auto" : "Semi")}", x, y + lineHeight * 2, fontSize, Color.LightGray);
        Raylib.DrawText($"Magazine: {weapon.MagazineAmmo}/{weapon.MagazineSize}", x, y + lineHeight * 3, fontSize, Color.RayWhite);
        Raylib.DrawText($"Reserve: {weapon.ReserveAmmo} | {reloadState}", x, y + lineHeight * 4, fontSize, weapon.IsReloading ? Color.Gold : Color.LightGray);
    }

    private static void RenderBar(string label, float percent, int x, int y, int width, int height, Color fillColor, float damageFlash, float rechargePulse)
    {
        percent = MathUtils.Clamp(percent, 0f, 1f);
        Color frameColor = damageFlash > 0f ? Color.White : Rgba(120, 132, 150, 255);
        Color glowColor = rechargePulse > 0f ? Rgba(126, 235, 255, 85) : Rgba(0, 0, 0, 120);

        Raylib.DrawText(label, x, y - 20, 16, Color.LightGray);
        Raylib.DrawRectangle(x - 4, y - 4, width + 8, height + 8, glowColor);
        Raylib.DrawRectangle(x, y, width, height, Rgba(12, 18, 28, 220));
        Raylib.DrawRectangle(x, y, (int)(width * percent), height, fillColor);
        Raylib.DrawRectangleLines(x, y, width, height, frameColor);

        const int segments = 10;
        for (int i = 1; i < segments; i++)
        {
            int tickX = x + (width * i / segments);
            Raylib.DrawLine(tickX, y + 3, tickX, y + height - 3, Rgba(255, 255, 255, 55));
        }
    }

    private void RenderDamageOverlays(int screenWidth, int screenHeight)
    {
        if (shieldHitFlashRemaining > 0f)
        {
            int alpha = (int)(85f * (shieldHitFlashRemaining / ShieldHitFlashDuration));
            Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, Rgba(70, 185, 255, alpha));
        }

        if (healthDamageFlashRemaining > 0f)
        {
            int alpha = (int)(105f * (healthDamageFlashRemaining / HealthDamageFlashDuration));
            Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, Rgba(210, 35, 35, alpha));
        }

        if (shieldBreakRemaining > 0f)
        {
            float t = shieldBreakRemaining / ShieldBreakDuration;
            int alpha = (int)(155f * t);
            Raylib.DrawCircleLines(screenWidth / 2, screenHeight / 2, 250f + (1f - t) * 180f, Rgba(122, 220, 255, alpha));
            Raylib.DrawText("SHIELD DOWN", (screenWidth / 2) - 80, (screenHeight / 2) + 86, 22, Rgba(122, 220, 255, alpha));
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
        string subtitle = victory ? "Extraction complete" : "Armor systems offline";
        string detail = victory ? "Arena secure" : $"Targets remaining: {livingEnemies}";
        Color accent = victory ? Color.Gold : Rgba(255, 80, 80, 255);

        const int panelWidth = 520;
        const int panelHeight = 190;
        int panelX = centerX - panelWidth / 2;
        int panelY = centerY - 130;

        Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, Rgba(4, 8, 14, 225));
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
        int alpha = (int)(255f * t);

        Color markerColor = Rgba(255, 230, 118, alpha);
        Raylib.DrawLine(centerX - outer, centerY - outer, centerX - inner, centerY - inner, markerColor);
        Raylib.DrawLine(centerX + inner, centerY - inner, centerX + outer, centerY - outer, markerColor);
        Raylib.DrawLine(centerX - outer, centerY + outer, centerX - inner, centerY + inner, markerColor);
        Raylib.DrawLine(centerX + inner, centerY + inner, centerX + outer, centerY + outer, markerColor);
        Raylib.DrawCircleLines(centerX, centerY, 18f, Rgba(255, 255, 255, (int)(110f * t)));
    }

    private static Color Rgba(int r, int g, int b, int a)
    {
        return new Color(
            (byte)Math.Clamp(r, 0, 255),
            (byte)Math.Clamp(g, 0, 255),
            (byte)Math.Clamp(b, 0, 255),
            (byte)Math.Clamp(a, 0, 255));
    }
}