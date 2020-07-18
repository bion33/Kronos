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
    public class Ops : ICommand
    {
        public async Task Run()
        {
            UIConsole.Show("Compiling operations report... \n");

            var api = RepoApi.Api;

            var start = GetStart();
            var end = start + 3 * 3600;

            var changes = await api.DelegateChangesFrom(start);
            var invaders = await api.TaggedInvader();
            var defenders = await api.TaggedDefender();

            var delegacyChanges = DelegacyChanges(changes);

            var ops = await FilterOps(delegacyChanges, end - 86400, invaders, defenders);

            var report = Report(ops);

            File.WriteAllText($"Kronos-Ops_{TimeUtil.DateForPath()}.md", report);
            UIConsole.Show("Done. \n");
        }

        private double GetStart()
        {
            if (TimeUtil.Today().AddHours(3) < TimeUtil.Now() && TimeUtil.Now() < TimeUtil.Today().AddHours(14))
                return TimeUtil.UnixLastMajorStart();

            return TimeUtil.UnixLastMinorStart();
        }

        private async Task<List<string>> GetDelegateMoves(string del, double since)
        {
            try
            {
                var api = RepoApi.Api;
                var url =
                    $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;view=nation.{del};filter=move;sincetime={since}";
                var response = await api.Request(url);
                return response.FindAll("<EVENT(.*?)</EVENT>");
            }
            catch (HttpRequestException)
            {
                return new List<string>();
            }
        }

        private string GetMoveBeforeBecomingDelegate(List<string> delegateMoves, double becameDelegateTime)
        {
            foreach (var move in delegateMoves)
            {
                var timestamp = int.Parse(move.Find("<TIMESTAMP>(.*?)</TIMESTAMP>"));
                if (timestamp < becameDelegateTime) return move.Find("<TEXT>(.*?)</TEXT>");
            }

            return null;
        }

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
                report += "# === No suspicious activity found ===\n";
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
            }
            else
            {
                report += "# === No suspicious activity found ===\n";
            }

            return report;
        }

        private struct DelegacyChange
        {
            public string region;
            public string newDelegate;
            public int changeTimeStamp;
        }

        private enum OpType
        {
            Defence,
            Suspicious,
            Invasion
        }
    }
}