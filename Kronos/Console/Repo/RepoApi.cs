using System;
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
        private static RepoApi api;
        private readonly Queue<string> queue = new Queue<string>();
        private DateTime lastRequest = DateTime.Now;
        private int numNations;
        private List<string> taggedDefender;
        private List<string> taggedFounderless;
        private List<string> taggedInvader;
        private List<string> taggedPassword;

        private RepoApi()
        {
        }

        public static RepoApi Api => api ??= new RepoApi();

        public async Task<string> Request(string url)
        {
            queue.Enqueue(url);
            while (queue.Peek() != url || lastRequest > DateTime.Now.AddSeconds(-1))
                await Task.Delay(1000 - (int) (DateTime.Now - lastRequest).TotalMilliseconds);

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.UserAgent = Shared.UserAgent;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using var response = (HttpWebResponse) await request.GetResponseAsync();

            queue.Dequeue();
            lastRequest = DateTime.Now;
            // System.Console.WriteLine($"Request @ {DateTime.Now}: {response.StatusCode}");

            await using var stream = response.GetResponseStream() ??
                                     throw new ProtocolViolationException("There is no response stream");
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        public async Task<List<string>> DelegateChangesFrom(double start)
        {
            var waHappenings = new List<string>();
            var more = true;
            var i = 1;

            while (more)
            {
                more = false;
                var url =
                    $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=member;sincetime={start};beforetime={start + i * 900};limit=200";
                var response = await Request(url);
                var found = response.FindAll("<EVENT id=\"[0-9]*\">(.*?)</EVENT>");
                foreach (var happening in found)
                    if (!waHappenings.Contains(happening) && happening.Contains("WA Delegate"))
                    {
                        waHappenings.Add(happening);
                        more = true;
                    }

                i += 1;
            }

            return waHappenings;
        }

        public async Task<List<string>> RegionsWithTag(string tag)
        {
            var url = $"https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags={tag}";
            var response = await Request(url);
            response = response.Replace("\n", "");
            var found = response.Find("<REGIONS>(.*?)</REGIONS>");
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
            numNations = int.Parse(response.Find("<NUMNATIONS>(.*?)</NUMNATIONS>"));
            return numNations;
        }

        public async Task<double> EndOfMinor()
        {
            var decrementInterval = 900;
            var presumedEnd = TimeUtil.UnixLastMinorEnd();
            var lastInfluenceChange = 0;

            while (lastInfluenceChange == 0)
            {
                var response =
                    await Request(
                        $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=change;beforetime={presumedEnd};limit=200");
                var influenceChanges = response.Split(".");
                presumedEnd -= decrementInterval;

                foreach (var change in influenceChanges)
                    if (change.Contains("influence"))
                    {
                        lastInfluenceChange = int.Parse(change.Find("<TIMESTAMP>(.*)</TIMESTAMP>"));
                        break;
                    }
            }

            return lastInfluenceChange;
        }

        public async Task<int> LastUpdateFor(string region)
        {
            region = region.ToLower().Replace(" ", "_");
            var url = $"https://www.nationstates.net/cgi-bin/api.cgi?region={region}&q=lastupdate";
            var response = await Request(url);
            return int.Parse(response.Find("<LASTUPDATE>(.*)</LASTUPDATE>"));
        }
    }
}