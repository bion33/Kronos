using System;
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
        private static double secondsPerNation;
        private static double variance;
        private static bool usingSheet;
        private static bool largeRegionAhead;

        public async Task Run()
        {
            UIConsole.Show("Preparing region data... \n");
            var regions = await RepoDump.Dump.Regions();
            var index = -1;

            while (index < 0)
            {
                UIConsole.Show("Provide no region below to exit. \n\nTarget Region: ");
                var t = UIConsole.GetInput();
            
                if (t == "") return;
                index = regions.FindIndex(r => r.name.ToLower() == t.ToLower());
                if (index == -1) UIConsole.Show("Region name not found.\n");   
            }

            var target = regions[index];
            var major = TimeUtil.PosixLastMajorStart() < TimeUtil.PosixLastMinorStart();
            var lastUpdateTook = (major) ? await RepoDump.Dump.MajorTook() : await RepoDump.Dump.MinorTook();
            var interval = (int) (MaxSecondsDeviation * await RepoDump.Dump.NumNations() / (lastUpdateTook + 0.0));

            var triggers = Triggers(index, regions, interval);

            var thisUpdateStart = (major) ? TimeUtil.PosixThisMajorStart() : TimeUtil.PosixThisMinorStart();
            UpdateSecondsPerNation(major, triggers, interval, thisUpdateStart);

            UIConsole.Show($"{"Time".PadRight(9, ' ')}|{" Variance".PadRight(12, ' ')}|{" Status".PadRight(25, ' ')}\n{"".PadRight(8, '-')} {"".PadRight(12, '-')} {"".PadRight(25, '-')}\n");

            while (true)
            {
                await Task.Delay(1000);
                
                var timeToUpdate = (target.nationCumulative * secondsPerNation) + thisUpdateStart - TimeUtil.PosixNow();    // There's an issue here, it's 12 minutes off when on sheet time for The West Pacific.
                var strTimeToUpdate = TimeUtil.ToHms(timeToUpdate).PadRight(9, ' ');
                
                var varies = variance.ToString().PadRight(11, ' ');
                
                var statusMessage = (usingSheet) ? "Using sheet time." : (largeRegionAhead) ? "<!> Large region ahead." : "";
                statusMessage = statusMessage.PadRight(24, ' ');
                
                
                UIConsole.Show($"\r{strTimeToUpdate}| {varies}| {statusMessage}");
            }
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

        private async Task UpdateSecondsPerNation(bool major, List<Region> triggers, int interval, double thisUpdateStart)
        {
            var keepCheckingTriggers = true;

            while (keepCheckingTriggers)
            {
                if (thisUpdateStart > TimeUtil.PosixNow())
                {
                    usingSheet = true;
                    secondsPerNation = (major) ? await RepoDump.Dump.MajorTick() : await RepoDump.Dump.MinorTick();
                    await Task.Delay(1000);
                }
                else
                {
                    usingSheet = false;
                    for (int i = 0; i < triggers.Count; i++)
                    {
                        var trigger = triggers[i];
                        largeRegionAhead = (trigger.nationCumulative - triggers[i - 1].nationCumulative) > interval * 2;
                        var lastTriggerUpdate = (major) ? trigger.majorUpdateTime : trigger.minorUpdateTime;
                        var waitForTriggerUpdate = true;
                        var thisTriggerUpdate = 0.0;
                        while (waitForTriggerUpdate)
                        {
                            thisTriggerUpdate = await RepoApi.Api.LastUpdateFor(trigger.name) - TimeUtil.PosixToday();
                            waitForTriggerUpdate = (int) thisTriggerUpdate == (int) lastTriggerUpdate;
                        }
                        secondsPerNation = (thisTriggerUpdate - thisUpdateStart) / trigger.nationCumulative;
                        variance = lastTriggerUpdate - thisTriggerUpdate;
                    }

                    keepCheckingTriggers = false;
                }
            }
        }
    }
}