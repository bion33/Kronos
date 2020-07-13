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
                return TimeUtil.PosixLastMajorStart();

            return TimeUtil.PosixLastMinorStart();
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
                var timestamp = int.Parse(move.Find( "<TIMESTAMP>(.*?)</TIMESTAMP>"));
                if (timestamp < becameDelegateTime) return move.Find( "<TEXT>(.*?)</TEXT>");
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

        private async Task<Dictionary<string, OpType>> FilterOps(List<DelegacyChange> delegacyChanges, double since,
            List<string> invaders, List<string> defenders)
        {
            var ops = new Dictionary<string, OpType>();
            for (var i = 0; i < delegacyChanges.Count; i++)
            {
                var delegateName = delegacyChanges[i].newDelegate;
                var delMoves = await GetDelegateMoves(delegateName, since);

                if (delMoves.Count > 0)
                {
                    var moveBeforeBecomingWaD =
                        GetMoveBeforeBecomingDelegate(delMoves, delegacyChanges[i].changeTimeStamp);
                    if (moveBeforeBecomingWaD == null) continue;

                    var region = TextUtil.ToTitleCase(delegacyChanges[i].region.Replace("_", " "));
                    var link = $"https://www.nationstates.net/region={delegacyChanges[i].region}";
                    if (invaders.Any(x => x.ToLower().Replace(" ", "_") == moveBeforeBecomingWaD))
                    {
                        ops[region] = OpType.Invasion;
                    }
                    else
                    {
                        var movedFrom = moveBeforeBecomingWaD.Find("%%(.*?)%%");
                        if (defenders.Any(x => x.ToLower().Replace(" ", "_") == movedFrom))
                            ops[region] = OpType.Defence;
                        else
                            ops[region] = OpType.Suspicious;
                    }
                }
            }

            return ops;
        }

        private string Report(Dictionary<string, OpType> ops)
        {
            var report = $"Report: {TimeUtil.Now()}\n";
            if (ops.Count > 0)
            {
                var invaded = ops.Where(p => p.Value == OpType.Invasion).ToList();
                if (invaded.Count > 0)
                {
                    report += $"# === {invaded.Count} Possible Raider Activity === \n";
                    invaded.ForEach(p =>
                    {
                        var link = $"https://www.nationstates.net/region={p.Key.ToLower().Replace(" ", "_")}";
                        report += $"* Possible raider activity in {p.Key} \n{link}\n";
                    });
                }

                var suspicious = ops.Where(p => p.Value == OpType.Suspicious).ToList();
                if (suspicious.Count > 0)
                {
                    report += $"# === {suspicious.Count} Suspicious Delegacy Changes === \n";
                    suspicious.ForEach(p =>
                    {
                        var link = $"https://www.nationstates.net/region={p.Key.ToLower().Replace(" ", "_")}";
                        report += $"* Suspicious delegacy change in {p.Key} \n{link}\n";
                    });
                }

                var defended = ops.Where(p => p.Value == OpType.Defence).ToList();
                if (defended.Count > 0)
                {
                    report += $"# === {defended.Count} Likely Defence Operations === \n";
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