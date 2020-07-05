using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Console.Utilities;

namespace Console.Repo
{
    public class RepoApi
    {
        private readonly Queue<string> queue = new Queue<string>();
        private List<string> taggedInvader;
        private List<string> taggedPassword;
        private List<string> taggedDefender;
        private List<string> taggedFounderless;
        private int numNations;
        
        public async Task<string> Request(string url)
        {
            queue.Enqueue(url);
            await Task.Delay(1000);
            while (queue.Peek() != url)
            {
                await Task.Delay(1000);
            }
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = Shared.UserAgent;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
            await using Stream stream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(stream);
            
            queue.Dequeue();
            return await reader.ReadToEndAsync();
        }

        public async Task<List<string>> DelegateChangesBetween(double start, double end)
        {
            var api = Shared.Api;

            List<string> waHappenings = new List<string>();
            var more = true;
            var i = 0;

            while (more)
            {
                more = false;
                var url = $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=member;sincetime={start};beforetime={end - (i * 2400)};limit=200";
                var response = await Request(url);
                var found = RegexUtil.FindAll(response, "<EVENT id=\"[0-9]*\">(.*?)</EVENT>");
                foreach (var happening in found)
                {
                    if (!waHappenings.Contains(happening) && happening.Contains("WA Delegate"))
                    {
                        waHappenings.Add(happening);
                        more = true;
                    }
                }
            }

            return waHappenings;
        }

        public async Task<List<string>> RegionsWithTag(string tag)
        {
            var url = $"https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags={tag}";
            var response = await Request(url);
            response = response.Replace("\n", "");
            var found = RegexUtil.Find(response, "<REGIONS>(.*?)</REGIONS>");
            found = found.Replace("['", "").Replace("']", "");
            var tagged = found.Split(",").ToList();
            return tagged;
        }
        
        public async Task<List<string>> TaggedInvader()
        {
            if (taggedInvader != null) return taggedInvader;

            taggedInvader = await RegionsWithTag("invader");
            return taggedInvader;
        }
        
        public async Task<List<string>> TaggedDefender()
        {
            if (taggedDefender != null) return taggedDefender;
            
            taggedDefender = await RegionsWithTag("defender");
            return taggedDefender;
        }

        public async Task<List<string>> TaggedFounderless()
        {
            if (taggedFounderless != null) return taggedFounderless;
            
            taggedFounderless = await RegionsWithTag("founderless");
            return taggedFounderless;
        }
        
        
        public async Task<List<string>> TaggedPassword()
        {
            if (taggedPassword != null) return taggedPassword;
            
            taggedPassword = await RegionsWithTag("password");
            return taggedPassword;
        }
        
        public async Task<int> NumNations()
        {
            if (numNations != 0) return numNations;

            var url = "https://www.nationstates.net/cgi-bin/api.cgi?q=numnations";
            var response = await Request(url);
            numNations = int.Parse(RegexUtil.Find(response, "<NUMNATIONS>(.*?)</NUMNATIONS>"));
            return numNations;
        }
        
        public async Task<double> EndOfMinor()
        {
            var decrementInterval = 900;
            var presumedEnd = TimeUtil.PosixLastMinorEnd();
            var lastInfluenceChange = 0;

            while (lastInfluenceChange == 0)
            {
                var response = await Request($"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=change;beforetime={presumedEnd};limit=200");
                var influenceChanges = response.Split(".");
                presumedEnd -= decrementInterval;

                foreach (var change in influenceChanges)
                {
                    if (change.Contains("influence"))
                    {
                        lastInfluenceChange = int.Parse(RegexUtil.Find(change, "<TIMESTAMP>(.*)</TIMESTAMP>"));
                        break;   
                    }
                }
            }

            return lastInfluenceChange;
        }
    }
}