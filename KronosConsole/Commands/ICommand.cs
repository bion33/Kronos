using System.Threading.Tasks;

namespace KronosConsole.Commands
{
    /// <summary> Interface for commands. Every command uses the Run method to execute. </summary>
    public interface ICommand
    {
        /// <summary> Execute the command </summary>
        public Task Run();
    }
}