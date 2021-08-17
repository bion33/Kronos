using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Kronos.Utilities;

namespace Kronos.Repo
{
    /// <summary> Get data from the NS API </summary>
    public class RepoApi
    {
        private static RepoApi api;
        private readonly Queue<string> queue = new Queue<string>();
        private readonly string userAgent;
        private DateTime lastRequest = DateTime.Now;
        private int numNations;
        private List<string> taggedDefender;
        private List<string> taggedFounderless;
        private List<string> taggedImperialist;
        private List<string> taggedIndependent;
        private List<string> taggedInvader;
        private List<string> taggedPassword;

        /// <summary> This class is a singleton </summary>
        private RepoApi(string userAgent)
        {
            this.userAgent = userAgent;
        }

        /// <summary> This class is a singleton </summary>
        public static RepoApi Api(string userAgent)
        {
            return api ??= new RepoApi(userAgent);
        }

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
            request.UserAgent = userAgent;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using var response = (HttpWebResponse) await request.GetResponseAsync();

            // Add download size
            Shared.BytesDownloaded += response.Headers.ToByteArray().Length;
            if (response.ContentLength > 0) Shared.BytesDownloaded += response.ContentLength;

            // Dequeue
            queue.Dequeue();
            lastRequest = DateTime.Now;

            // Logging
            // System.Kronos.WriteLine($"Request @ {DateTime.Now}: {response.StatusCode}");

            // Return data
            await using var stream = response.GetResponseStream() ??
                                     throw new ProtocolViolationException("There is no response stream");
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary> Get nations becoming, usurping and being removed from the position of WA Delegate </summary>
        public async Task<List<string>> DelegateChangesFrom(double start, double end)
        {
            var waHappenings = new List<string>();
            var decrement = 200;
            var lastId = "";
            var i = 0;
            var m = 0;

            // Run while beforetime is after start of update
            while (end > start)
            {
                var url =
                    $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=member;beforetime={end};limit=200";
                var response = await Request(url);
                var found = response.FindAll("<EVENT id=\"[0-9]*\">(.*?)</EVENT>");
                var currentId = response.FindAll("<EVENT id=\"(.*?)\">").Last();
                if (found.Count > m) m = found.Count;

                // Add delegate changes
                foreach (var happening in found)
                    if (!waHappenings.Contains(happening) && happening.ToLower().Contains("wa delegate") &&
                        happening.ToLower().Contains("became"))
                        waHappenings.Add(happening);

                // Reset start to last timestamp received
                var lastTimeStamp = response.FindAll("<TIMESTAMP>(.*?)</TIMESTAMP>").Last();
                if (!int.TryParse(lastTimeStamp, out var timestamp)) timestamp = (int) end;
                end = timestamp + 1;

                // Prevent getting stuck in a loop
                if (found.Count == 0 || currentId == lastId) end -= decrement;
                lastId = currentId;

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

        public async Task<List<string>> TaggedImperialist()
        {
            if (taggedImperialist != null) return taggedImperialist;

            taggedImperialist = await RegionsWithTag("imperialist");
            return taggedImperialist;
        }

        /// <summary> Get the names of all regions with the "defender" tag </summary>
        public async Task<List<string>> TaggedDefender()
        {
            if (taggedDefender != null) return taggedDefender;

            taggedDefender = await RegionsWithTag("defender");
            return taggedDefender;
        }

        /// <summary> Get the names of all regions with the "independent" tag </summary>
        public async Task<List<string>> TaggedIndependent()
        {
            if (taggedIndependent != null) return taggedIndependent;

            taggedIndependent = await RegionsWithTag("independent");
            return taggedIndependent;
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