using System.Threading.Tasks;
using Console.UI;

namespace Console
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            UIConsole.Show("Kronos, at your service.\n");
            UIConsole.Show("Starting...\n");

            while (true)
            {
                var commands = UIConsole.GetCommands(args);
                foreach (var command in commands) await command.Run();   
            }
        }
    }
}