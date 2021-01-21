using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kronos.Repo;
using Kronos.Utilities;

namespace Kronos.Commands
{
    /// <summary>
    ///     Command to generate a report of likely military operations during the last (major or minor) update.
    ///     Use the "Run" method to execute.
    /// </summary>
    public class Ops : ICommand
    {
        private RepoApi api;

        /// <summary> Generate a report of likely military operations during the last (major or minor) update </summary>
        public async Task Run(string userAgent, bool interactiveLog = false)
        {
            await Run(userAgent, new Dictionary<string, string>(), interactiveLog);
        }

        /// <summary> Generate a report of likely military operations during the last (major or minor) update </summary>
        public async Task Run(string userAgent, Dictionary<string, string> userTags, bool interactiveLog = false)
        {
            Console.Write("Compiling operations report... \n");

            api = RepoApi.Api(userAgent);

            // Start and end of last (major or minor) update
            var updateStart = StartOfLastUpdate();
            var updateEnd = updateStart + 3 * 3600;

            // Get happenings
            var delegateChangeHappenings = await api.DelegateChangesFrom(updateStart);

            // Parse each happening to the DelegacyChange DTO
            var delegacyChanges = DelegacyChanges(delegateChangeHappenings);

            // Filter out the suspicious changes from among the (supposedly) legitimate changes
            var ops = await FilterOps(delegacyChanges, updateEnd - 43200, userTags, userAgent, interactiveLog);

            if (interactiveLog) Console.Write("Saving to report... ");

            // Generate report
            ops = ops.OrderBy(o => o.changeTimeStamp).ToList();
            var report = Report(ops);

            // Save
            var date = TimeUtil.DateForPath();
            Directory.CreateDirectory(date);
            await File.WriteAllTextAsync($"{date}/Kronos-Ops_{date}.md", report);

            if (interactiveLog) Console.Write("[done].\n");
        }

        /// <summary> Generate a report of likely military operations during the last (major or minor) update </summary>
        private double StartOfLastUpdate()
        {
            if (TimeUtil.Today().AddHours(3) < TimeUtil.Now() && TimeUtil.Now() < TimeUtil.Today().AddHours(14))
                return TimeUtil.UnixLastMajorStart();

            return TimeUtil.UnixLastMinorStart();
        }

        /// <summary> Get "move" happenings for a nation since a Unix timestamp </summary>
        private async Task<List<string>> LastMoves(string nationName, double since)
        {
            var url =
                $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;view=nation.{nationName};filter=move;sincetime={since}";
            var response = await api.Request(url);
            return response.FindAll("<EVENT(.*?)</EVENT>");
        }

        /// <summary> Get the "move" which happened right before the Unix timestamp at which the nation became WA Delegate </summary>
        private string MoveBeforeBecomingDelegate(List<string> delegateMoves, double becameDelegateTime)
        {
            // Due to the order of happenings received from NS always being last -> earliest, we can do a simple comparison.
            // The first move to have happened before becameDelegateTime is the one we need.
            foreach (var move in delegateMoves)
            {
                var timestamp = int.Parse(move.Find("<TIMESTAMP>(.*?)</TIMESTAMP>"));
                if (timestamp < becameDelegateTime) return move.Find("<TEXT>(.*?)</TEXT>");
            }

            return null;
        }

        /// <summary> Convert each delegacy change happening to the DelegacyChange DTO </summary>
        private static List<DelegacyChange> DelegacyChanges(List<string> changes)
        {
            var delegacyChanges = new List<DelegacyChange>();
            foreach (var change in changes)
                delegacyChanges.Add(new DelegacyChange
                {
                    region = change.Find("%%(.*?)%%"),
                    newDelegate = change.Find("@@(.*?)@@"),
                    changeTimeStamp = int.Parse(change.Find("<TIMESTAMP>(.*?)</TIMESTAMP>"))
                });

            return delegacyChanges;
        }

        /// <summary>
        ///     Go trough the delegacy changes to filter out the suspicious ones. A suspicious change is one where the
        ///     new delegate just moved into the region before becoming delegate, which commonly happens during tag raids
        ///     and detags, or during surprise invasions on small regions. This method cannot filter out big raids,
        ///     defences and sieges which happen over the course of multiple days.
        /// </summary>
        /// <returns> A dictionary with region-name, OpType pairs </returns>
        private async Task<List<DelegacyChange>> FilterOps(List<DelegacyChange> delegacyChanges, double since,
            Dictionary<string, string> userTags, string userAgent, bool interactiveLog = false)
        {
            // Get regions with tag
            var invaders = await api.TaggedInvader();
            var imperialists = await api.TaggedImperialist();
            var defenders = await api.TaggedDefender();
            var independents = await api.TaggedIndependent();

            var ops = new List<DelegacyChange>();

            // For each delegacy change
            foreach (var c in delegacyChanges)
            {
                var change = c;

                // Delegate nation name
                var delegateName = change.newDelegate;
                // Get the last moves made by that nation since ...
                var delMoves = await LastMoves(delegateName, since);

                // If the nation moved since ...
                if (delMoves.Count > 0)
                {
                    // Get the move right before becoming delegate
                    var moveBeforeBecomingWaD =
                        MoveBeforeBecomingDelegate(delMoves, change.changeTimeStamp);

                    // Skip change if nation didn't move before becoming delegate (it might have moved after)
                    if (moveBeforeBecomingWaD == null) continue;

                    change.region = TextUtil.ToTitleCase(change.region.Replace("_", " "));
                    var movedFrom = moveBeforeBecomingWaD.Find("%%(.*?)%%");

                    // Determine operation type
                    change.opType = OpTypeFromOrigin(movedFrom, invaders, imperialists, defenders, independents,
                        userTags);
                    if (change.opType == OpType.Suspicious)
                        change.opType = await OpTypeFromEmbassies(change.region, userTags, userAgent);

                    // Add change to operations
                    ops.Add(change);

                    if (interactiveLog)
                    {
                        var type = change.opType.ToString();
                        Console.Write($"* {type} activity in {change.region}\n");
                    }
                }
            }

            return ops;
        }

        /// <summary>
        ///     Based on the given origin region, determine what kind of operation it was
        /// </summary>
        private OpType OpTypeFromOrigin(string region, List<string> invaders, List<string> imperialists,
            List<string> defenders, List<string> independents, Dictionary<string, string> userTags)
        {
            // If the incoming delegate came from a region the user gave priority, set it aside 
            if (userTags.Any(kv => kv.Key == region && kv.Value == "PriorityRegions")) return OpType.Priority;

            // If the incoming delegate came from a region tagged "invader" or "imperialist", it's probably a
            // tag raid or surprise invasion
            if (invaders.Any(x => x.ToLower().Replace(" ", "_") == region)
                || imperialists.Any(x => x.ToLower().Replace(" ", "_") == region)
                || userTags.Any(kv => kv.Key == region && kv.Value == "RaiderRegions"))
                return OpType.Raider;

            // If the incoming delegate came from a region tagged "defender", it's probably a defence or detag
            if (defenders.Any(x => x.ToLower().Replace(" ", "_") == region)
                || userTags.Any(kv => kv.Key == region && kv.Value == "DefenderRegions"))
                return OpType.Defender;

            // If the incoming delegate came from a region tagged "independent", it could be anything
            if (independents.Any(x => x.ToLower().Replace(" ", "_") == region)
                || userTags.Any(kv => kv.Key == region && kv.Value == "IndependentRegions"))
                return OpType.Independent;

            // Sometimes GP-ers use non-tagged regions as jump points, so any change right after moving must at
            // least be considered suspicious
            return OpType.Suspicious;
        }

        /// <summary>
        ///     Based on the region's embassies, determine what kind of operation it was
        /// </summary>
        private async Task<OpType> OpTypeFromEmbassies(string region, Dictionary<string, string> userTags,
            string userAgent)
        {
            // Get region's embassies
            var embassies = await RepoApi.Api(userAgent).EmbassiesOf(region);

            // If any pending embassy is in userTags, use it's corresponding tag to define the opType
            foreach (var pair in userTags)
                if (embassies.Any(e => e.Value == "pending" && e.Key.ToLower().Replace(" ", "_") == pair.Key))
                    switch (pair.Value)
                    {
                        case "PriorityRegions": return OpType.Priority;
                        case "RaiderRegions": return OpType.Raider;
                        case "IndependentRegions": return OpType.Independent;
                        case "DefenderRegions": return OpType.Defender;
                    }

            // Default
            return OpType.Suspicious;
        }

        /// <summary> Make a readable report out of a dictionary with region-name, OpType pairs </summary>
        private string Report(List<DelegacyChange> ops)
        {
            // Add date and time at the top
            var report = $"Report: {TimeUtil.Now()}\n";

            // If no operations
            if (ops.Count == 0)
            {
                report += "\n# === No military activity found ===\n";
            }
            // If any operations
            else
            {
                report += ReportSection(ops, OpType.Priority);
                report += ReportSection(ops, OpType.Raider);
                report += ReportSection(ops, OpType.Independent);
                report += ReportSection(ops, OpType.Defender);
                report += ReportSection(ops, OpType.Suspicious, "Suspicious", "Change");
            }

            return report;
        }

        /// <summary>
        ///     Create a section of the report from the given operations, for the specified OpType, where opTypeName
        ///     is used in the section's title and opSynonym can be used to use different terminology from the default
        ///     "Operation".
        /// </summary>
        private string ReportSection(List<DelegacyChange> ops, OpType opType, string opTypeName = null,
            string opSynonym = "Operation")
        {
            opTypeName ??= opType.ToString();
            var section = "";
            var group = ops.Where(p => p.opType == opType).ToList();
            if (group.Count > 0)
            {
                section += $"\n# === {group.Count} {opTypeName} {opSynonym}s === \n";
                group.ForEach(p =>
                {
                    var link = $"https://www.nationstates.net/region={p.region.ToLower().Replace(" ", "_")}";
                    section += $"* {opSynonym} in {p.region} @ {TimeUtil.ToUpdateOffset(p.changeTimeStamp)} \n{link}\n";
                });
            }

            return section;
        }

        /// <summary> DTO for delegacy change happenings </summary>
        private struct DelegacyChange
        {
            public string region;
            public string newDelegate;
            public int changeTimeStamp;
            public OpType opType;
        }

        /// <summary> Enum for categorising operations </summary>
        private enum OpType
        {
            Raider,
            Independent,
            Defender,
            Priority,
            Suspicious
        }
    }
}