using System.Threading.Tasks;
using KronosConsole.Repo;
using KronosConsole.UI;

namespace KronosConsole.Commands
{
    /// <summary> Command to generate a sheet with update times and information for all regions </summary>
    public class Sheet : ICommand
    {
        /// <summary> Generate a sheet with update times and information for all regions </summary>
        public async Task Run()
        {
            await new Kronos.Commands.Sheet().Run(Shared.UserAgent, true);
        }
    }
}