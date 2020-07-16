using System;
using System.Threading.Tasks;
using Console.UI;

namespace Console.Commands
{
    /// <summary> Command to quit Kronos </summary>
    public class Quit : ICommand
    {
        /// <summary> Quit Kronos </summary>
        public async Task Run()
        {
            UIConsole.Show("Goodbye!\n");
            Environment.Exit(0);
        }
    }
}