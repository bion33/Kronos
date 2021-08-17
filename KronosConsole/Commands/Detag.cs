using System.Threading.Tasks;
using KronosConsole.Repo;

namespace KronosConsole.Commands
{
    /// <summary> Command to generate a sheet with update times and information for detag-able regions </summary>
    public class Detag : ICommand
    {
        /// <summary> Generate a sheet with update times and information for detag-able regions </summary>
        public async Task Run()
        {
            await new Kronos.Commands.Detag().Run(Shared.UserAgent, Shared.UserTags, true);
        }
    }
}