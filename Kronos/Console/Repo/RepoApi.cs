using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Console.Utilities;

namespace Console.Repo
{
    /// <summary> Get data from the NS API </summary>
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

        /// <summary> This class is a singleton </summary>
        private RepoApi()
        {
        }

        /// <summary> This class is a singleton </summary>
        public static RepoApi Api => api ??= new RepoApi();

        /// <summary> Make an API request. Requests are queued and spaced apart at least 1 second. </summary>
        public async Task<string> Request(string url)
        {
            // Queue
            queue.Enqueue(url);

            // Space apart
            while (queue.Peek() != url || lastRequest > DateTime.Now.AddSeconds(-1))
                await Task.Delay(1000 - (int) (DateTime.Now - lastRequest).TotalMilliseconds);

            // Request
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.UserAgent = Shared.UserAgent;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using var response = (HttpWebResponse) await request.GetResponseAsync();

            // Dequeue
            queue.Dequeue();
            lastRequest = DateTime.Now;

            // Logging
            // System.Console.WriteLine($"Request @ {DateTime.Now}: {response.StatusCode}");

            // Return data
            await using var stream = response.GetResponseStream() ??
                                     throw new ProtocolViolationException("There is no response stream");
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary> Get nations becoming, usurping and being removed from the position of WA Delegate </summary>
        public async Task<List<string>> DelegateChangesFrom(double start)
        {
            var waHappenings = new List<string>();
            var more = true;
            var i = 1;

            // Run while more delegate changes are found
            while (more)
            {
                more = false;

                // Get (<= 200) happenings
                var url =
                    $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=member;sincetime={start};beforetime={start + i * 900};limit=200";
                var response = await Request(url);
                var found = response.FindAll("<EVENT id=\"[0-9]*\">(.*?)</EVENT>");

                // Add delegate changes
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

        /// <summary> Get the names of all regions with a given tag </summary>
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

        /// <summary> Get the names of all regions with the "invader" tag </summary>
        public async Task<List<string>> TaggedInvader()
        {
            if (taggedInvader != null) return taggedInvader;

            taggedInvader = await RegionsWithTag("invader");
            return taggedInvader;
        }

        /// <summary> Get the names of all regions with the "defender" tag </summary>
        public async Task<List<string>> TaggedDefender()
        {
            if (taggedDefender != null) return taggedDefender;

            taggedDefender = await RegionsWithTag("defender");
            return taggedDefender;
        }

        /// <summary> Get the names of all regions without founder </summary>
        public async Task<List<string>> TaggedFounderless()
        {
            if (taggedFounderless != null) return taggedFounderless;

            taggedFounderless = await RegionsWithTag("founderless");
            return taggedFounderless;
        }

        /// <summary> Get the names of all regions which are password-protected </summary>
        public async Task<List<string>> TaggedPassword()
        {
            if (taggedPassword != null) return taggedPassword;

            taggedPassword = await RegionsWithTag("password");
            return taggedPassword;
        }

        /// <summary>
        ///     Get the up-to-date nation count. Don't use this in combination with calculations using times calculated
        ///     from the region dump, instead use the function found in RepoRegionDump.
        /// </summary>
        public async Task<int> NumNations()
        {
            if (numNations != 0) return numNations;

            var url = "https://www.nationstates.net/cgi-bin/api.cgi?q=numnations";
            var response = await Request(url);
            numNations = int.Parse(response.Find("<NUMNATIONS>(.*?)</NUMNATIONS>"));
            return numNations;
        }

        /// <summary> Get the end of the last minor update from the world happenings. </summary>
        public async Task<double> EndOfMinor()
        {
            var decrementInterval = 900;
            var presumedEnd = TimeUtil.UnixLastMinorEnd();
            var lastInfluenceChange = 0;

            // While no end of update found in happenings
            while (lastInfluenceChange == 0)
            {
                // Get happenings
                var response =
                    await Request(
                        $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=change;beforetime={presumedEnd};limit=200");
                var influenceChanges = response.Split(".");

                // Check happenings for the last influence change
                foreach (var change in influenceChanges)
                    if (change.Contains("influence"))
                    {
                        lastInfluenceChange = int.Parse(change.Find("<TIMESTAMP>(.*)</TIMESTAMP>"));
                        break;
                    }

                // Next time check earlier
                presumedEnd -= decrementInterval;
            }

            return lastInfluenceChange;
        }

        /// <summary>
        ///     Get the last time the region updated according to the API. Contrary to the similarly named dump
        ///     tag, this may be major or minor. The dump tag only ever contains major.
        /// </summary>
        public async Task<int> LastUpdateFor(string region)
        {
            region = region.ToLower().Replace(" ", "_");
            var url = $"https://www.nationstates.net/cgi-bin/api.cgi?region={region}&q=lastupdate";
            var response = await Request(url);
            return int.Parse(response.Find("<LASTUPDATE>(.*)</LASTUPDATE>"));
        }
    }
}