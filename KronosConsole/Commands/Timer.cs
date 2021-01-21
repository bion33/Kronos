using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Kronos.Repo;
using Kronos.Utilities;
using KronosConsole.UI;
using Shared = KronosConsole.Repo.Shared;

namespace KronosConsole.Commands
{
    /// <summary> Command to show an estimated countdown to a region's next update </summary>
    public class Timer : ICommand
    {
        private readonly string argument;
        private Kronos.Commands.Timer timer;

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
            var regions = await RepoRegionDump.Dump(Shared.UserAgent).Regions(interactiveLog: true);
            var targetIndex = -1;

            // Get target region from argument if it was provided
            if (argument != null) targetIndex = regions.FindIndex(r => r.name.ToLower() == argument.ToLower());

            // Get target region from user (ask again if given target doesn't exist)
            while (targetIndex < 0)
            {
                UIConsole.Show("\nTarget Region: ");
                var t = UIConsole.GetInput();

                if(t.StartsWith("https://www.nationstates.net/region="))
                {
                    t = t["https://www.nationstates.net/region=".Length..];
                }
                if (t.Contains("_"))
                {
                    t = FromID(t);
                }

                if (t == "") return;
                targetIndex = regions.FindIndex(r => r.name.ToLower() == t.ToLower());
                if (targetIndex == -1) UIConsole.Show("Region name not found.\n");
            }

            timer = new Kronos.Commands.Timer(regions[targetIndex].name);
#pragma warning disable CS4014 
            timer.Run(Shared.UserAgent, true);
#pragma warning restore CS4014

            // Show timer header
            UIConsole.Show("Press [Q] to quit Timer.\n\n");
            UIConsole.Show($"{" Time".PadRight(9, ' ')} | {"Trigger".PadRight(7, ' ')} | Variance \n");
            UIConsole.Show($"{"".PadRight(10, '-')} {"".PadRight(9, '-')} {"".PadRight(12, '-')}\n");

            await ShowUpdateTimer();

            // Tell timer to stop, if still running (in case user interrupted)
            timer.Stop();

            UIConsole.Show("\n\n");
        }

        /// <summary>
        ///     Show the user the time until the current target region updates, the trigger being used at the
        ///     moment, and the current variance compared to the previous trigger.
        /// </summary>
        private async Task ShowUpdateTimer()
        {
            // While the user has not interrupted the timer, and the target has not yet updated.
            while (true)
            {
                // The trigger we're using right now
                var trigger = timer.CurrentTrigger().ToString().PadLeft(3, ' ');
                // The total amount of triggers - 1, to remove the target region itself
                // Might be confusing to the user otherwise why the trigger counter never gets to the last trigger
                var relevantTriggers = timer.TotalTriggers().ToString().PadRight(3, ' ');

                // Output line string
                var str =
                    $"{TimeUtil.ToHms(timer.CurrentTimeToUpdate())} | {trigger}/{relevantTriggers} | {timer.CurrentVariance():0.} s";

                // Clear previous output line
                UIConsole.Show("\r".PadRight(str.Length * 2, ' '));
                // Show current output line
                UIConsole.Show($"\r{str}");

                // Check if user wants to interrupt
                if (UIConsole.Interrupted()) break;

                // Chill, no need to get a hot CPU
                await Task.Delay(250);
            }
        }

        public static string FromID(string text)
        {
            return text?.Trim().ToLower(CultureInfo.InvariantCulture).Replace('_', ' ');
        }
    }
}