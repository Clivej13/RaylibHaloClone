using System.Numerics;
using Raylib_cs;

namespace RaylibHaloClone;

public sealed partial class Level
{
    private void RenderSpawnPoint()
    {
        Raylib.DrawCylinder(PlayerSpawnPosition + new Vector3(0f, 0.025f, 0f), 0.65f, 0.65f, 0.05f, 24, SpawnMarkerColor);
        Raylib.DrawCylinderWires(PlayerSpawnPosition + new Vector3(0f, 0.035f, 0f), 0.65f, 0.65f, 0.07f, 24, Color.Green);
    }

    private void RenderExitZone(bool active)
    {
        Color fillColor = active ? ActiveExitFillColor : InactiveExitFillColor;
        Color wireColor = active ? ActiveExitWireColor : InactiveExitWireColor;

        Raylib.DrawCubeV(ExitPosition, ExitSize, fillColor);
        Raylib.DrawCubeWiresV(ExitPosition, ExitSize, wireColor);
        Raylib.DrawCubeWiresV(ExitPosition, ExitSize + new Vector3(0.08f), wireColor);
    }

    private void RenderDoors()
    {
        foreach (Door door in doors)
        {
            Vector3 position = door.IsOpen ? door.ClosedPosition + new Vector3(0f, door.Size.Y + 0.25f, 0f) : door.ClosedPosition;
            Raylib.DrawCubeV(position, door.Size, door.IsOpen ? door.OpenColor : door.ClosedColor);
            Raylib.DrawCubeWiresV(position, door.Size, door.IsOpen ? Color.SkyBlue : Color.Black);
        }
    }

    private void RenderSwitches()
    {
        foreach (InteractableSwitch interactableSwitch in switches)
        {
            interactableSwitch.Render();
        }
    }

    private void RenderWorldObjects()
    {
        foreach (WorldInteractable worldObject in worldObjects)
        {
            worldObject.Render();
        }
    }

    private void RenderLightFixtures()
    {
        foreach (Vector3 position in lightFixtures)
        {
            Raylib.DrawSphere(position, 0.22f, LightsOn ? Color.Gold : Color.DarkGray);
            Raylib.DrawSphereWires(position, 0.24f, 8, 8, LightsOn ? Color.Yellow : Color.Black);
        }
    }

}
