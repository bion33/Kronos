using System.Collections.Generic;
using System.Threading.Tasks;
using Console.Domain;
using Console.Repo;
using Console.UI;
using Console.Utilities;

namespace Console.Commands
{
    public class Kronos : ICommand
    {
        private static List<Region> regionsParsed;

        public async Task Run()
        {
            UIConsole.Show("Creating Kronos sheet... \n");
            var regions = await Regions();
        }

        public static async Task<List<Region>> Regions()
        {
            if (regionsParsed != null) return regionsParsed;

            var api = RepoApi.Api;
            var dump = RepoDump.Dump;

            var startOfLastMajor = TimeUtil.PosixLastMajorStart();
            var endOfLastMajor = await dump.EndOfMajor();
            var majorDuration = endOfLastMajor - startOfLastMajor;
            var majorTick = majorDuration / await api.NumNations();

            var startOfLastMinor = TimeUtil.PosixLastMinorStart();
            var endOfLastMinor = await api.EndOfMinor();
            var minorDuration = endOfLastMinor - startOfLastMinor;
            var minorTick = minorDuration / await api.NumNations();

            var regions = await dump.Regions();
            regions = AddMinorUpdateTimes(regions, minorTick);
            regions = AddReadableUpdateTimes(regions);
            regionsParsed = regions;

            return regions;
        }

        public static List<Region> AddMinorUpdateTimes(List<Region> regions, double minorTick)
        {
            for (var i = 0; i < regions.Count; i++)
                regions[i].minorUpdateTime = i == 0
                    ? regions[i].nationCount * minorTick + TimeUtil.PosixLastMinorStart()
                    : regions[i - 1].minorUpdateTime + regions[i].nationCount * minorTick;

            return regions;
        }

        public static List<Region> AddReadableUpdateTimes(List<Region> regions)
        {
            for (var i = 0; i < regions.Count; i++)
            {
                regions[i].readableMajorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].majorUpdateTime);
                regions[i].readableMinorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].minorUpdateTime);
            }

            return regions;
        }
    }
}