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
        private const int LastTriggerSecondsBeforeTarget = 3;
        private int currentTrigger;
        private bool keepGoing = true;
        private bool nextUpdateIsMajor;
        private List<double> secondsPerNation;
        private Region target;
        private List<Region> triggers;

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

            target = regions[index];
            nextUpdateIsMajor = TimeUtil.UnixLastMajorStart() < TimeUtil.UnixLastMinorStart();
            var lastUpdateTook = nextUpdateIsMajor ? await RepoDump.Dump.MajorTook() : await RepoDump.Dump.MinorTook();
            var interval = (int) (LastTriggerSecondsBeforeTarget * await RepoDump.Dump.NumNations() /
                                  (lastUpdateTook + 0.0));

            triggers = Triggers(index, regions, interval);

            UIConsole.Show("Press [Q] to quit Timer.\n");
            UIConsole.Show($"{" Time".PadRight(9, ' ')} | {"Trigger".PadRight(7, ' ')} | Variance \n");
            UIConsole.Show($"{"".PadRight(10, '-')} {"".PadRight(9, '-')} {"".PadRight(12, '-')}\n");

            secondsPerNation = new List<double>
            {
                nextUpdateIsMajor ? await RepoDump.Dump.MajorTick() : await RepoDump.Dump.MinorTick()
            };

            currentTrigger = 1;
            ShowUpdateTimer();

            while (keepGoing)
                if (!await Updated(target))
                    await WatchTriggers();

            // else average seconds/nation from sheet times is used

            UIConsole.Show("\nDone.\n");
        }

        private static List<Region> Triggers(int index, List<Region> regions, int interval)
        {
            var triggers = new List<Region>();
            double multiplier = 1;
            for (var i = index; i >= 0; i--)
                if (i == index || triggers.Last().nationCumulative - regions[i].nationCumulative >
                    interval * multiplier)
                {
                    triggers.Add(regions[i]);
                    multiplier *= 1.5;
                }

            triggers.Reverse();
            return triggers;
        }

        private async Task<bool> Updated(Region region)
        {
            var lastUpdate = await RepoApi.Api.LastUpdateFor(region.name);
            var startOfThisUpdate =
                nextUpdateIsMajor ? TimeUtil.UnixNextMajorStart() : TimeUtil.UnixNextMinorStart();

            return lastUpdate > startOfThisUpdate;
        }

        private double NextUpdateFor(Region region, double averageSecondsPerNation)
        {
            var nextUpdate = nextUpdateIsMajor ? TimeUtil.UnixNextMajorStart() : TimeUtil.UnixNextMinorStart();
            return nextUpdate + averageSecondsPerNation * region.nationCumulative;
        }

        private async Task ShowUpdateTimer()
        {
            while (keepGoing)
            {
                var average = secondsPerNation.Sum() / secondsPerNation.Count;
                var current = secondsPerNation.Last();
                var variance = current - average;
                var timeToUpdate = NextUpdateFor(target, current) - TimeUtil.UnixNow();

                var str =
                    $"{TimeUtil.ToHms(timeToUpdate)} | {currentTrigger.ToString().PadLeft(3, ' ')}/{triggers.Count.ToString().PadRight(3, ' ')} | {variance:0.00} s";

                UIConsole.Show("\r".PadRight(str.Length * 2, ' '));
                UIConsole.Show($"\r{str}");

                keepGoing = !UIConsole.Interrupted();
                await Task.Delay(500);
            }
        }

        private async Task WatchTriggers()
        {
            if (!await Updated(target))
                for (var i = 0; i < triggers.Count && keepGoing; i++)
                {
                    var trigger = triggers[i];
                    while (!await Updated(trigger))
                    {
                    }

                    var newUpdate = await RepoApi.Api.LastUpdateFor(trigger.name);
                    var updateStart = nextUpdateIsMajor ? TimeUtil.UnixNextMajorStart() : TimeUtil.UnixNextMinorStart();
                    var newSecondsPerNation = (newUpdate - updateStart) / trigger.nationCumulative;
                    secondsPerNation.Add(newSecondsPerNation);
                    currentTrigger = i + 1;
                }

            keepGoing = false;
            UIConsole.Show("\nTarget updated.");
        }
    }
}