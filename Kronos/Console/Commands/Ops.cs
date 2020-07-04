using System.Collections.Generic;
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
            UIConsole.Show("Compiling operations report...");
            
            var api = Shared.Api;
            
            var start = GetStart();
            var end = start + 7200;

            var changes = await api.GetDelegateChangesBetween(start, end);
            var invaders = await api.GetInvaders();
            var defenders = await api.GetDefenders();

            List<string> newDelegates = new List<string>();
            List<int> timestamps = new List<int>();
            List<string> newRegions = new List<string>();
            foreach (var change in changes)
            {
                newDelegates.Add(RegexUtil.Find(change, "@@(.*?)@@"));
                timestamps.Add(int.Parse(RegexUtil.Find(change, "<TIMESTAMP>(.*?)</TIMESTAMP>")));
                newRegions.Add(RegexUtil.Find(change, "%%(.*?)%%"));
            }
            
            List<string> invaded = new List<string>();
            List<string> defended = new List<string>();
            List<string> suspicious = new List<string>();
            for (int i = 0; i < newDelegates.Count; i++)
            {
                var del = newDelegates[i];
                var delMoves = await GetDelegateMoves(del, end - 86400);
                
                if (delMoves.Count > 0)
                {
                    var moveBeforeBecomingWaD = GetMoveBeforeBecomingDelegate(delMoves, timestamps[i]);
                    if (moveBeforeBecomingWaD == null) continue;
                    
                    var region = TextUtil.ToTitleCase(newRegions[i].Replace("_", " "));
                    var link = $"https://www.nationstates.net/region={newRegions[i]}";
                    if (invaders.Any(x => x.ToLower().Replace(" ", "_") == moveBeforeBecomingWaD))
                    {
                        invaded.Add($"Possible raider activity in {region}. \n{link}");
                    }
                    else
                    {
                        var movedFrom = RegexUtil.Find(moveBeforeBecomingWaD, "%%(.*?)%%");
                        if (defenders.Any(x => x.ToLower().Replace(" ", "_") == movedFrom))
                        {
                            defended.Add($"Likely defence operation in {region}. \n{link}");
                        }
                        else
                        {
                            suspicious.Add($"Suspicious delegacy change in {region}. \n{link}");
                        }
                    }
                }
            }

            UIConsole.Show(Report(invaded, suspicious, defended));
        }

        private double GetStart()
        {
            if (0 <= TimeUtil.HourNow() && TimeUtil.HourNow() <= 2) return TimeUtil.PosixToday() + 18000 - 86400;
            else if (2 < TimeUtil.HourNow() && TimeUtil.HourNow() <= 14) return TimeUtil.PosixToday() + 18000;
            return TimeUtil.PosixToday() + 61200;
        }

        private async Task<List<string>> GetDelegateMoves(string del, double since)
        {
            try
            {
                var api = Shared.Api;
                var url = $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;view=nation.{del};filter=move;sincetime={since}";
                var response = await api.GetAsync(url);
                return RegexUtil.FindAll(response, "<EVENT(.*?)</EVENT>");
            }
            catch (HttpRequestException e)
            {
                return new List<string>();
            }
        }

        private string GetMoveBeforeBecomingDelegate(List<string> delegateMoves, double becameDelegateTime)
        {
            foreach (var move in delegateMoves)
            {
                var timestamp = int.Parse(RegexUtil.Find(move, "<TIMESTAMP>(.*?)</TIMESTAMP>"));
                if (timestamp < becameDelegateTime)
                {
                    return RegexUtil.Find(move, "<TEXT>(.*?)</TEXT>");
                }
            }

            return null;
        }

        private string Report(List<string> invaded, List<string> suspicious, List<string> defended)
        {
            string report = $"Report: {TimeUtil.Now()}\n";
            if (invaded.Count > 0 || defended.Count > 0 || suspicious.Count > 0)
            {
                if (invaded.Count > 0)
                {
                    report += "# === Possible Raider Activity === \n";
                    invaded.ForEach(i => report += $"* {i}\n");
                }

                if (suspicious.Count > 0)
                {
                    report += "# === Suspicious Delegacy Changes === \n";
                    suspicious.ForEach(i => report += $"* {i}\n");
                }

                if (defended.Count > 0)
                {
                    report += "# === Likely Defence Operations === \n";
                    defended.ForEach(i => report += $"* {i}\n");   
                }
            }
            else
            {
                report += $"# === No suspicious activity found ===\n";
            }
            
            return report;
        } 
    }
}