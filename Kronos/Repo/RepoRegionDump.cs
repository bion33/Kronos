using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Kronos.Domain;
using Kronos.Utilities;

namespace Kronos.Repo
{
    /// <summary> NS region data from dump </summary>
    public class RepoRegionDump
    {
        private const string DUMP_URL = "https://www.nationstates.net/pages/regions.xml.gz";

        private static RepoRegionDump dump;
        private readonly string dumpGz = Path.Combine(Shared.ExePath, $".Regions_{TimeUtil.DateForPath()}.xml.gz");
        private readonly string dumpXml = Path.Combine(Shared.ExePath, $".Regions_{TimeUtil.DateForPath()}.xml");
        private readonly string userAgent;
        private readonly Dictionary<string, string> userTags;
        private int numNations;

        private List<Region> regions;

        /// <summary> This class is a singleton </summary>
        private RepoRegionDump(string userAgent, Dictionary<string, string> userTags)
        {
            this.userAgent = userAgent;
            this.userTags = userTags != null ? userTags : new Dictionary<string, string>();
        }

        /// <summary> This class is a singleton </summary>
        public static RepoRegionDump Dump(string userAgent, Dictionary<string, string> userTags)
        {
            return dump ??= new RepoRegionDump(userAgent, userTags);
        }

        /// <summary> Make sure the latest region dump is downloaded and extracted, and old ones are removed </summary>
        private async Task EnsureDumpReady(bool interactiveLog = false)
        {
            // If dump xml exists, skip
            if (File.Exists(dumpXml)) return;

            if (interactiveLog) Console.Write("Getting regions dump... ");

            RemoveOldDumps();
            await GetDumpAsync(DUMP_URL);
            await ExtractGz();

            if (interactiveLog) Console.Write("[done].\n");
        }

        /// <summary> Remove outdated region dumps </summary>
        private void RemoveOldDumps()
        {
            var dir = new DirectoryInfo(".");
            foreach (var file in dir.EnumerateFiles(".Regions_*.xml")) file.Delete();
            foreach (var file in dir.EnumerateFiles(".Regions_*.xml.gz")) file.Delete();
        }

        /// <summary> Download regions dump archive </summary>
        private async Task GetDumpAsync(string url)
        {
            // If dump archive or xml exists, skip
            if (File.Exists(dumpGz) || File.Exists(dumpXml)) return;

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.UserAgent = userAgent;
            using var response = (HttpWebResponse) await request.GetResponseAsync();
            await using var file = new FileStream(dumpGz, FileMode.CreateNew);
            var stream = response.GetResponseStream();
            if (stream != null) await stream.CopyToAsync(file);

            // Add download size
            Shared.BytesDownloaded += response.Headers.ToByteArray().Length;
            if (response.ContentLength > 0) Shared.BytesDownloaded += response.ContentLength;
        }

        /// <summary> Extract regions dump archive to XML </summary>
        private async Task ExtractGz()
        {
            // If dump xml exists, skip
            if (File.Exists(dumpXml)) return;

            // Extract & save
            await using (var inStream = new FileStream(dumpGz, FileMode.Open, FileAccess.Read))
            {
                await using var zipStream = new GZipStream(inStream, CompressionMode.Decompress);
                await using var outStream = new FileStream(dumpXml, FileMode.Create, FileAccess.Write);

                var tempBytes = new byte[4096];
                int i;
                while ((i = zipStream.Read(tempBytes, 0, tempBytes.Length)) != 0) outStream.Write(tempBytes, 0, i);
            }

            // Delete archive
            File.Delete(dumpGz);
        }

        /// <summary> Get parsed regions from dump </summary>
        public async Task<List<Region>> Regions(bool interactiveLog = false)
        {
            // Make sure regions dump is downloaded & extracted
            await EnsureDumpReady(interactiveLog);

            // Skip if already did this before
            if (regions != null) return regions;

            if (interactiveLog) Console.Write("Parsing regions... ");

            var api = RepoApi.Api(userAgent);
            regions = new List<Region>();
            numNations = 0;

            // Parse XML
            var regionsXml = XElement.Load(dumpXml).Elements("REGION");
            foreach (var element in regionsXml)
            {
                var name = element.XPathSelectElement("NAME").Value;
                var nations = int.Parse(element.XPathSelectElement("NUMNATIONS").Value);

                // Count nations
                numNations += nations;

                // Parse embassies
                var embassies = new List<Embassy>();
                foreach (var embassy in element.Element("EMBASSIES")!.Elements("EMBASSY"))
                {
                    var urlName = embassy.Value.ToLower().Replace(" ", "_");

                    EmbassyClass ec;
                    if (userTags.ContainsKey(urlName))
                        ec = userTags[urlName] switch
                        {
                            "PriorityRegions"    => EmbassyClass.PriorityRegions,
                            "RaiderRegions"      => EmbassyClass.RaiderRegions,
                            "IndependentRegions" => EmbassyClass.IndependentRegions,
                            "DefenderRegions"    => EmbassyClass.DefenderRegions,
                            _                    => EmbassyClass.None
                        };
                    else ec = EmbassyClass.None;

                    if (!embassy.Attributes().Any(a =>
                                                      a.Name.ToString().ToLower() == "type" &&
                                                      a.Value.ToLower() == "invited"))
                    {
                        embassies.Add(new Embassy
                        {
                            Name = embassy.Value,
                            Pending = embassy.Attributes().Any(a =>
                                                                   a.Name.ToString().ToLower() == "type" &&
                                                                   a.Value.ToLower() == "pending"),
                            EmbassyType = ec
                        });
                    }
                }

                // Get region info from XML, and complete it with data from the API
                regions.Add(new Region
                {
                    Name = name,
                    Url = $"https://www.nationstates.net/region={name.ToLower().Replace(" ", "_")}",
                    NationCount = nations,
                    NationCumulative = numNations,
                    DelegateVotes = int.Parse(element.XPathSelectElement("DELEGATEVOTES").Value),
                    DelegateAuthority = element.XPathSelectElement("DELEGATEAUTH").Value,
                    MajorUpdateTime = int.Parse(element.XPathSelectElement("LASTUPDATE").Value),
                    Founderless = (await api.TaggedFounderless()).Contains(name),
                    Password = (await api.TaggedPassword()).Contains(name),
                    Tagged = (await api.TaggedInvader()).Contains(name),
                    Embassies = embassies
                });
            }

            await AddMinorUpdateTimes();
            AddReadableUpdateTimes();
            
            if (interactiveLog) Console.Write("[done].\n");

            return regions;
        }

        /// <summary> Calculate minor for every region (can't get that from dump) </summary>
        private async Task AddMinorUpdateTimes()
        {
            var minorDuration = await RepoApi.Api(userAgent).EndOfMinor() - TimeUtil.UnixLastMinorStart();
            var minorTick = minorDuration / numNations;

            for (var i = 0; i < regions.Count; i++)
                regions[i].MinorUpdateTime = i == 0
                    ? regions[i].NationCount * minorTick + TimeUtil.UnixLastMinorStart()
                    : regions[i - 1].MinorUpdateTime + regions[i - 1].NationCount * minorTick;
        }

        /// <summary> Calculate readable update time (as HH:MM:SS since the start of each update) </summary>
        private void AddReadableUpdateTimes()
        {
            for (var i = 0; i < regions.Count; i++)
            {
                regions[i].ReadableMajorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].MajorUpdateTime);
                regions[i].ReadableMinorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].MinorUpdateTime);
            }
        }

        /// <summary> Calculate length of last major </summary>
        public async Task<double> MajorTook()
        {
            await EnsureDumpReady();
            await Regions();

            return regions.Last().MajorUpdateTime - regions.First().MajorUpdateTime;
        }

        /// <summary> Calculate length of last minor </summary>
        public async Task<double> MinorTook()
        {
            await EnsureDumpReady();
            await Regions();

            return regions.Last().MinorUpdateTime - regions.First().MinorUpdateTime;
        }

        /// <summary> Calculate length of last major </summary>
        public async Task<double> MajorTick()
        {
            await EnsureDumpReady();
            await Regions();

            return await MajorTook() / (await NumNations() + 0.0);
        }

        /// <summary> Calculate seconds/nation for minor </summary>
        public async Task<double> MinorTick()
        {
            await EnsureDumpReady();
            await Regions();

            return await MinorTook() / (await NumNations() + 0.0);
        }

        /// <summary>
        ///     Make sure the dump is parsed, then return the amount of nations on NS at that point in time. This can be
        ///     gotten up-to-date from the API, but the dump-version must be used in dump-based calculations.
        /// </summary>
        public async Task<int> NumNations()
        {
            await EnsureDumpReady();
            await Regions();

            return numNations;
        }
    }
}