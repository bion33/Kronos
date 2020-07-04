using System.Threading.Tasks;
using Console.Commands;
using Console.Repo;
using Console.UI;

namespace Console
{
    static class Program
    {
        private static async Task Main(string[] args)
        {
            UIConsole.Show("Kronos, at your service.\n");
            UIConsole.Show("Starting...\n");
            
            var commands = UIConsole.GetCommands(args);
            foreach (var command in commands) await command.Run();
        }
    }
}