using System;
using System.Threading.Tasks;

namespace Console.Commands
{
    public interface ICommand
    {
        public Task Run();
    }
}