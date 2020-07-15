using System;
using System.Threading.Tasks;
using Console.UI;

namespace Console.Commands
{
    public class Quit : ICommand

    {
        public async Task Run()
        {
            UIConsole.Show("Goodbye!\n");
            Environment.Exit(0);
        }
    }
}