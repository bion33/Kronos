using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Console.Repo;
using Console.UI;
using Console.Utilities;

namespace Console.Commands
{
    /// <summary> Command to generate a report of likely military operations during the last (major or minor) update </summary>
    public class Ops : ICommand
    {
        /// <summary> Generate a report of likely military operations during the last (major or minor) update </summary>
        public async Task Run()
        {
            UIConsole.Show("Compiling operations report... \n");

            var api = RepoApi.Api;

            // Start and end of last (major or minor) update
            var updateStart = StartOfLastUpdate();
            var updateEnd = updateStart + 3 * 3600;

            // Get happenings
            var delegateChangeHappenings = await api.DelegateChangesFrom(updateStart);

            // Parse each happening to the DelegacyChange DTO
            var delegacyChanges = DelegacyChanges(delegateChangeHappenings);

            // Filter out the suspicious changes from among the (supposedly) legitimate changes
            var ops = await FilterOps(delegacyChanges, updateEnd - 86400);
            
            // Generate report
            var report = Report(ops);

            // Save
            File.WriteAllText($"Kronos-Ops_{TimeUtil.DateForPath()}.md", report);
            UIConsole.Show("Done. \n");
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
            var api = RepoApi.Api;
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
        private async Task<Dictionary<string, OpType>> FilterOps(List<DelegacyChange> delegacyChanges, double since)
        {
            // Get regions with tag
            var invaders = await RepoApi.Api.TaggedInvader();
            var imperialists = await RepoApi.Api.TaggedImperialist();
            var defenders = await RepoApi.Api.TaggedDefender();

            var ops = new Dictionary<string, OpType>();
            
            // For each delegacy change
            for (var i = 0; i < delegacyChanges.Count; i++)
            {
                // Delegate nation name
                var delegateName = delegacyChanges[i].newDelegate;
                // Get the last moves made by that nation since ...
                var delMoves = await LastMoves(delegateName, since);
                
                // If the nation moved since ...
                if (delMoves.Count > 0)
                {
                    // Get the move right before becoming delegate
                    var moveBeforeBecomingWaD =
                        MoveBeforeBecomingDelegate(delMoves, delegacyChanges[i].changeTimeStamp);

                    // Skip change if nation didn't move before becoming delegate (it might have moved after)
                    if (moveBeforeBecomingWaD == null) continue;

                    var region = TextUtil.ToTitleCase(delegacyChanges[i].region.Replace("_", " "));
                    var movedFrom = moveBeforeBecomingWaD.Find("%%(.*?)%%");

                    // Determine operation type
                    if (invaders.Any(x => x.ToLower().Replace(" ", "_") == movedFrom)
                        || imperialists.Any(x => x.ToLower().Replace(" ", "_") == movedFrom))
                    {
                        // If the incoming delegate came from a region tagged "invader" or "imperialist",
                        // it's probably a tag raid or surprise invasion
                        ops[region] = OpType.Invasion;
                    }
                    else if (defenders.Any(x => x.ToLower().Replace(" ", "_") == movedFrom)) 
                    {
                        // If the incoming delegate came from a region tagged "defender", it's probably a defence or detag
                        ops[region] = OpType.Defence;
                    }
                    else
                    {
                        // Sometimes defenders, raiders and imperialists use non-tagged regions as jump points
                        // So any change right after moving must at least be considered suspicious
                        ops[region] = OpType.Suspicious;
                    }
                }
            }

            return ops;
        }

        /// <summary> Make a readable report out of a dictionary with region-name, OpType pairs </summary>
        private string Report(Dictionary<string, OpType> ops)
        {
            // Add date and time at the top
            var report = $"Report: {TimeUtil.Now()}\n";
            
            // If no operations
            if (ops.Count == 0)
            {
                report += "\n# === No suspicious activity found ===\n";
            }
            // If any operations
            else
            {
                // Invasions
                var invaded = ops.Where(p => p.Value == OpType.Invasion).ToList();
                if (invaded.Count > 0)
                {
                    report += $"\n# === {invaded.Count} Possible Raider Activity === \n";
                    invaded.ForEach(p =>
                    {
                        var link = $"https://www.nationstates.net/region={p.Key.ToLower().Replace(" ", "_")}";
                        report += $"* Possible raider activity in {p.Key} \n{link}\n";
                    });
                }
                
                // Defences
                var defended = ops.Where(p => p.Value == OpType.Defence).ToList();
                if (defended.Count > 0)
                {
                    report += $"\n# === {defended.Count} Likely Defence Operations === \n";
                    defended.ForEach(p =>
                    {
                        var link = $"https://www.nationstates.net/region={p.Key.ToLower().Replace(" ", "_")}";
                        report += $"* Likely defence operation in {p.Key} \n{link}\n";
                    });
                }
                
                // Other
                var suspicious = ops.Where(p => p.Value == OpType.Suspicious).ToList();
                if (suspicious.Count > 0)
                {
                    report += $"\n# === {suspicious.Count} Suspicious Delegacy Changes === \n";
                    suspicious.ForEach(p =>
                    {
                        var link = $"https://www.nationstates.net/region={p.Key.ToLower().Replace(" ", "_")}";
                        report += $"* Suspicious delegacy change in {p.Key} \n{link}\n";
                    });
                }
            }

            return report;
        }

        /// <summary> DTO for delegacy change happenings </summary>
        private struct DelegacyChange
        {
            public string region;
            public string newDelegate;
            public int changeTimeStamp;
        }

        /// <summary> Enum for categorising operations </summary>
        private enum OpType
        {
            Defence,
            Suspicious,
            Invasion
        }
    }
}