using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Console.Domain;
using Console.Repo;
using Console.UI;
using Console.Utilities;

namespace Console.Commands
{
    public class Timer : ICommand
    {
        private const int MaxSecondsDeviation = 10;
        private bool nextUpdateIsMajor;
        private Region currentTrigger;
        private List<double> secondsPerNation;
        private bool keepGoing = true;

        public async Task Run()
        {
            UIConsole.Show("Preparing region data... \n");
            var regions = await RepoDump.Dump.Regions();
            var index = -1;

            while (index < 0)
            {
                UIConsole.Show("Provide no region below to quit Timer. \n\nTarget Region: ");
                var t = UIConsole.GetInput();
            
                if (t == "") return;
                index = regions.FindIndex(r => r.name.ToLower() == t.ToLower());
                if (index == -1) UIConsole.Show("Region name not found.\n");   
            }

            var target = regions[index];
            nextUpdateIsMajor = TimeUtil.PosixLastMajorStart() < TimeUtil.PosixLastMinorStart();
            var lastUpdateTook = (nextUpdateIsMajor) ? await RepoDump.Dump.MajorTook() : await RepoDump.Dump.MinorTook();
            var interval = (int) (MaxSecondsDeviation * await RepoDump.Dump.NumNations() / (lastUpdateTook + 0.0));

            var triggers = Triggers(index, regions, interval);

            UIConsole.Show("Press [Q] to quit Timer.\nTime\n----\n");

            secondsPerNation = new List<double>
            {
                (nextUpdateIsMajor) ? await RepoDump.Dump.MajorTick() : await RepoDump.Dump.MinorTick()
            };

            currentTrigger = target;
            ShowUpdateTimer();
            
            while (keepGoing)
            {
                if (! await Updated(target)) await Watch(triggers);
                
                // else average seconds/nation from sheet times is used
            }
            
            UIConsole.Show("\nDone.\n");
        }

        private static List<Region> Triggers(int index, List<Region> regions, int interval)
        {
            var triggers = new List<Region>();
            for (int i = index; i >= 0; i--)
            {
                if (i == index || triggers.Last().nationCumulative - regions[i].nationCumulative > interval)
                    triggers.Add(regions[i]);
            }

            triggers.Reverse();
            return triggers;
        }

        private async Task<bool> Updated(Region region)
        {
            var lastUpdate = await RepoApi.Api.LastUpdateFor(region.name);
            
            if (nextUpdateIsMajor) return lastUpdate > region.majorUpdateTime;
            return lastUpdate > region.minorUpdateTime;
        }

        private double NextUpdateFor(Region region, double averageSecondsPerNation)
        {
            var nextUpdate = (nextUpdateIsMajor) ? TimeUtil.PosixNextMajorStart() : TimeUtil.PosixNextMinorStart();
            return nextUpdate + (averageSecondsPerNation * region.nationCumulative);
        }

        private async Task ShowUpdateTimer()
        {
            while (keepGoing)
            {
                var average = secondsPerNation.Sum() / (secondsPerNation.Count + 0.0);
                var timeToUpdate = NextUpdateFor(currentTrigger, average) - TimeUtil.PosixNow();
                UIConsole.Show($"\r{TimeUtil.ToHms(timeToUpdate)}");
                keepGoing = ! UIConsole.Interrupted();
                await Task.Delay(500);
            }
        }

        private async Task Watch(List<Region> triggers)
        {
            for (int i = 0; i < triggers.Count && keepGoing; i++)
            {
                currentTrigger = triggers[i];
                while (! await Updated(currentTrigger)) await Task.Delay(500);
                var newUpdate = await RepoApi.Api.LastUpdateFor(currentTrigger.name);
                var updateStart = (nextUpdateIsMajor) ? TimeUtil.PosixNextMajorStart() : TimeUtil.PosixLastMinorStart();
                var newSecondsPerNation = (newUpdate - updateStart) / currentTrigger.nationCumulative;
                secondsPerNation.Add(newSecondsPerNation);
            }
        }
    }
}