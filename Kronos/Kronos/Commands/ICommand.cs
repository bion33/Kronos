using System.Threading.Tasks;

namespace Kronos.Commands
{
    /// <summary> Interface for application commands </summary>
    public interface ICommand
    {
        /// <summary> Execute the command </summary>
        public Task Run();
    }
}