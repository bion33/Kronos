using System.Collections.Generic;
using System.Threading.Tasks;
using Console.Domain;
using Console.Repo;
using Console.Utilities;

namespace Console.Commands
{
    public class Detag : ICommand
    {
        public async Task Run()
        {
            var regions = await Kronos.Regions();
        }
    }
}