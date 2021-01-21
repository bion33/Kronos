using System.Threading.Tasks;
using KronosConsole.Repo;

namespace KronosConsole.Commands
{
    /// <summary> Command to generate a report of likely military operations during the last (major or minor) update </summary>
    public class Ops : ICommand
    {
        /// <summary> Generate a report of likely military operations during the last (major or minor) update </summary>
        public async Task Run()
        {
            await new Kronos.Commands.Ops().Run(Shared.UserAgent, Shared.UserTags, true);
        }
    }
}