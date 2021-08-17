using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kronos.Commands
{
    /// <summary> Interface for commands. Every command uses the Run method to execute. </summary>
    public interface ICommand
    {
        /// <summary> Execute the command </summary>
        public Task Run(string userAgent, Dictionary<string, string> userTags, bool interactiveLog = false);
    }
}