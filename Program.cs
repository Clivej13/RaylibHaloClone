namespace RaylibHaloClone;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var game = new Game();
        game.Run();
    }
}
