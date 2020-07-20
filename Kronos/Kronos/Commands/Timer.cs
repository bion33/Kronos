using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kronos.Domain;
using Kronos.Repo;
using Kronos.UI;
using Kronos.Utilities;

namespace Kronos.Commands
{
    /// <summary> Command to show an estimated countdown to a region's next update </summary>
    public class Timer : ICommand
    {
        private const int LastTriggerSecondsBeforeTarget = 3;
        private readonly string argument;
        private int currentTrigger = 1;
        private bool keepGoing = true;
        private bool nextUpdateIsMajor;
        private List<double> secondsPerNation;
        private Region target;
        private List<Region> triggers;

        public Timer()
        {
        }

        public Timer(List<string> arguments)
        {
            argument = string.Join(" ", arguments);
        }

        /// <summary> Show an estimated countdown to a region's next update </summary>
        public async Task Run()
        {
            var regions = await RepoRegionDump.Dump.Regions();
            var targetIndex = -1;

            // Get target region from argument if it was provided
            if (argument != null) targetIndex = regions.FindIndex(r => r.name.ToLower() == argument.ToLower());

            // Get target region from user (ask again if given target doesn't exist)
            while (targetIndex < 0)
            {
                UIConsole.Show("\nTarget Region: ");
                var t = UIConsole.GetInput();

                if (t == "") return;
                targetIndex = regions.FindIndex(r => r.name.ToLower() == t.ToLower());
                if (targetIndex == -1) UIConsole.Show("Region name not found.\n");
            }

            target = regions[targetIndex];

            // A boolean indicating if the next update is major (true) or minor (false)
            nextUpdateIsMajor = TimeUtil.UnixLastMajorStart() < TimeUtil.UnixLastMinorStart();

            // Length of the last update in seconds
            var lastUpdateTook = nextUpdateIsMajor
                ? await RepoRegionDump.Dump.MajorTook()
                : await RepoRegionDump.Dump.MinorTook();

            // Select the most appropriate triggers for the target, given the length of the last update
            triggers = await Triggers(targetIndex, regions, lastUpdateTook);

            // Show timer header
            UIConsole.Show("Press [Q] to quit Timer.\n\n");
            UIConsole.Show($"{" Time".PadRight(9, ' ')} | {"Trigger".PadRight(7, ' ')} | Variance \n");
            UIConsole.Show($"{"".PadRight(10, '-')} {"".PadRight(9, '-')} {"".PadRight(12, '-')}\n");

            // For each trigger, an average "seconds-per-nation" value is calculated and appended to this list. As the
            // update goes faster or slower, these averages will change in between triggers. This can then be used
            // to determine variance (unpredictability of the update), which the user can use to better determine how
            // much risk they can take.
            secondsPerNation = new List<double>
            {
                // The first seconds-per-nation value is taken from the previous update
                nextUpdateIsMajor ? await RepoRegionDump.Dump.MajorTick() : await RepoRegionDump.Dump.MinorTick()
            };

            // Display the timer asynchronously so that it counts down consistently. Do not await this.
            var unusedSuppressWarning = ShowUpdateTimer();

            // Watch the triggers and target to adjust seconds-per-nation and exit when target updates
            await WatchTriggers();

            UIConsole.Show("\n\n");
        }

        /// <summary> Get the best triggers for the target, given the time (in seconds) the last update took </summary>
        private static async Task<List<Region>> Triggers(int targetIndex, List<Region> regions, double lastUpdateTook)
        {
            // This "interval" represents an amount of nations. Given the length of an update, it can be used to
            // approximate the time between the updates of two regions. As the length of update is variable, this
            // interval should not be static.
            var firstInterval = (int) (LastTriggerSecondsBeforeTarget * await RepoRegionDump.Dump.NumNations() /
                                       (lastUpdateTook + 0.0));

            var triggers = new List<Region>();
            double multiplier = 1;

            // Start with the target, then iterate down to the first region in the update order.
            //
            // The interval (amount of nations between triggers) is increased with every trigger, so that we don't get
            // too many triggers (linearly you could get easily over a 100 triggers if you want any degree of accuracy).
            // Linear trigger progression is also inconvenient because we can only check 1 trigger per second.
            //
            // Once the difference in cumulative nations (nations are counted up from the first updating region in the
            // update order to the last, meaning the last region in the update order has a cumulative nation count
            // equalling the nations on NS in total) between the last added trigger and the current region is exceeded,
            // consider the current region as a new trigger. 
            for (var i = targetIndex; i >= 0; i--)
                if (i == targetIndex
                    || triggers.Last().nationCumulative - triggers.Last().nationCount -
                    (regions[i].nationCumulative - regions[i].nationCount) > firstInterval * multiplier)
                {
                    triggers.Add(regions[i]);
                    multiplier *= 1.5;
                }

            // Reverse the triggers, so that they go from earliest to latest
            triggers.Reverse();

            return triggers;
        }

        /// <summary> Check if a region has updated. Keep in mind that this is limited by RepoApi to 1 request per second! </summary>
        private async Task<bool> Updated(Region region)
        {
            var lastUpdate = await RepoApi.Api.LastUpdateFor(region.name);
            var startOfThisUpdate =
                nextUpdateIsMajor ? TimeUtil.UnixNextMajorStart() : TimeUtil.UnixNextMinorStart();

            return lastUpdate > startOfThisUpdate;
        }

        /// <summary> Check if an update is currently running </summary>
        private bool UpdateStarted()
        {
            var startOfThisUpdate =
                nextUpdateIsMajor ? TimeUtil.UnixNextMajorStart() : TimeUtil.UnixNextMinorStart();
            return TimeUtil.UnixNow() > startOfThisUpdate;
        }

        /// <summary> Calculate the estimated time a given region will update next, given a known seconds-per-nation </summary>
        private double NextUpdateFor(Region region, double averageSecondsPerNation)
        {
            var nextUpdate = nextUpdateIsMajor ? TimeUtil.UnixNextMajorStart() : TimeUtil.UnixNextMinorStart();
            return nextUpdate + averageSecondsPerNation * (region.nationCumulative - region.nationCount);
        }

        /// <summary>
        ///     Show the user the time until the current target region updates, the trigger being used at the
        ///     moment, and the current variance compared to the previous trigger.
        /// </summary>
        private async Task ShowUpdateTimer()
        {
            // While the user has not interrupted the timer, and the target has not yet updated.
            while (keepGoing)
            {
                // The trigger we're using right now
                var trigger = CurrentTrigger.ToString().PadLeft(3, ' ');
                // The total amount of triggers - 1, to remove the target region itself
                // Might be confusing to the user otherwise why the trigger counter never gets to the last trigger
                var relevantTriggers = TotalTriggers.ToString().PadRight(3, ' ');

                // Output line string
                var str = $"{TimeUtil.ToHms(CurrentTimeToUpdate())} | {trigger}/{relevantTriggers} | {CurrentVariance():0.} s";

                // Clear previous output line
                UIConsole.Show("\r".PadRight(str.Length * 2, ' '));
                // Show current output line
                UIConsole.Show($"\r{str}");

                // Check if user wants to interrupt
                keepGoing = !UIConsole.Interrupted();

                // Chill, no need to get a hot CPU
                await Task.Delay(250);
            }
        }

        /// <summary>
        ///     Check each trigger to see if it updated yet, and if it did, save new seconds-per-nation and move
        ///     to the next trigger until the target has updated or the user has interrupted.
        /// </summary>
        private async Task WatchTriggers()
        {
            // While the user does not interrupt and the target has not updated
            while (keepGoing)
            {
                // Update has not yet started, skip
                if (!UpdateStarted())
                {
                    await Task.Delay(1000);
                    continue;
                }

                // If target already updated, break
                if (await Updated(target))
                {
                    keepGoing = false;
                    break;
                }

                // Go backwards trough the list of triggers until we encounter the first one which is yet to update to
                // save requests (and thus time).
                var startTrigger = 0;
                for (var i = triggers.Count - 2; i >= 0; i--)
                {
                    if (!await Updated(triggers[i])) continue;

                    startTrigger = i;
                    await SecsPerNation(triggers[i]);
                    break;
                }

                // Update has started
                // For each trigger while user has not interrupted and target has not yet updated
                for (var i = startTrigger; i < triggers.Count && keepGoing; i++)
                {
                    var trigger = triggers[i];
                    currentTrigger = i;

                    // Wait until trigger updates or user interrupts
                    while (!await Updated(trigger) && keepGoing)
                    {
                    }

                    await SecsPerNation(trigger);
                }

                // If the for loop exited because the target updated, keepGoing must be set to false
                keepGoing = false;
            }

            // Show target updated message if this is the case
            if (await Updated(target))
            {
                UIConsole.Show("\nTarget updated.");
                await Task.Delay(500);
            }
        }

        /// <summary>
        ///     Recalculate seconds per nation based upon the last time the given trigger updated. This method makes an
        ///     API request to get said last time, so use this method sparsely.
        /// </summary>
        private async Task SecsPerNation(Region trigger)
        {
            var newUpdate = await RepoApi.Api.LastUpdateFor(trigger.name);
            var updateStart = nextUpdateIsMajor ? TimeUtil.UnixNextMajorStart() : TimeUtil.UnixNextMinorStart();
            var newSecondsPerNation = (newUpdate - updateStart) / (trigger.nationCumulative - trigger.nationCount);
            secondsPerNation.Add(newSecondsPerNation);
        }

        /// <summary> The current estimated time in seconds until the target region updates </summary>
        public double CurrentTimeToUpdate()
        {
            var currentSecondsPerNation = secondsPerNation.Last();
            return NextUpdateFor(target, currentSecondsPerNation) - TimeUtil.UnixNow();
        }

        /// <summary> The difference between the current estimated update time and the previous, in seconds </summary>
        public double CurrentVariance()
        {
            var currentSecondsPerNation = secondsPerNation.Last();
            var lastSecondsPerNation = secondsPerNation.Count > 1
                ? secondsPerNation[^2]
                : currentSecondsPerNation;
            return currentSecondsPerNation * (target.nationCumulative - target.nationCount) - lastSecondsPerNation * 
                (target.nationCumulative - target.nationCount);
        }

        /// <summary> The trigger which is currently being watched until it updates </summary>
        public int CurrentTrigger => currentTrigger;

        /// <summary> The total amount of triggers before the target region updates </summary>
        public int TotalTriggers => triggers.Count - 1;    // The target region is the last "trigger"
    }
}