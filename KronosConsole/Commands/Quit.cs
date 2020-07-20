using System;
using System.Threading.Tasks;
using Kronos.Repo;
using KronosConsole.UI;

namespace KronosConsole.Commands
{
    /// <summary> Command to quit Kronos </summary>
    public class Quit : ICommand
    {
        /// <summary> Quit Kronos </summary>
        public async Task Run()
        {
            UIConsole.Show(
                $"Kronos downloaded {Math.Ceiling(Shared.BytesDownloaded / 1000.0):.} KiB of data in total.\n");
            UIConsole.Show("Goodbye!\n");
            await Task.Delay(100);
            Environment.Exit(0);
        }
    }
}