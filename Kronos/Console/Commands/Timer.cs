using System.Threading.Tasks;

namespace Console.Commands
{
    public class Timer : ICommand
    {
        public async Task Run()
        {
            await Task.Delay(1);
        }
    }
}