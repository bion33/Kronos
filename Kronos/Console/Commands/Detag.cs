using System.Threading.Tasks;
using Console.Repo;
using Console.Utilities;

namespace Console.Commands
{
    public class Detag : ICommand
    {
        public async Task Run()
        {
            var api = Shared.Api;
            var dump = new RepoDump();

            var startOfLastMajor = TimeUtil.PosixLastMajorStart();
            var endOfLastMajor = await dump.EndOfMajor();
            var majorDuration = endOfLastMajor - startOfLastMajor;
            var majorTick = majorDuration / await api.NumNations();
            
            var startOfLastMinor = TimeUtil.PosixLastMinorStart();
            var endOfLastMinor = await api.EndOfMinor();
            var minorDuration = endOfLastMinor - startOfLastMinor;
            var minorTick = minorDuration / await api.NumNations();

            var regions = await dump.Regions();
            
            
            for (int i = 0; i < regions.Count; i++)
            {
                if (i == 0)
                {
                    regions[i].majorUpdateTime = regions[i].nationCount * majorTick;
                    regions[i].minorUpdateTime = regions[i].nationCount * minorTick;
                }
                else
                {
                    regions[i].majorUpdateTime = regions[i - 1].majorUpdateTime + regions[i].nationCount * majorTick;
                    regions[i].minorUpdateTime = regions[i - 1].minorUpdateTime + regions[i].nationCount * minorTick;
                }
            }
        }
    }
}