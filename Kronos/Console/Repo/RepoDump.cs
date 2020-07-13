using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Console.Domain;
using Console.Utilities;

namespace Console.Repo
{
    public class RepoDump
    {
        private const string DumpUrl = "https://www.nationstates.net/pages/regions.xml.gz";

        private static RepoDump dump;
        private readonly string dumpGz = $".Regions_{TimeUtil.DateForPath()}.xml.gz";
        private readonly string dumpXml = $".Regions_{TimeUtil.DateForPath()}.xml";

        private List<Region> regions;
        private int numNations = 0;

        private RepoDump()
        {
        }

        public static RepoDump Dump => dump ??= new RepoDump();

        private async Task EnsureDumpReady()
        {
            if (File.Exists(dumpXml)) return;

            RemoveOldDumps();
            await GetDumpAsync(DumpUrl);
            await ExtractGz(dumpGz, dumpXml);
        }

        private void RemoveOldDumps()
        {
            var dir = new DirectoryInfo(".");
            foreach (var file in dir.EnumerateFiles(".Regions_*.xml")) file.Delete();
        }

        private async Task GetDumpAsync(string url)
        {
            if (File.Exists(dumpGz) || File.Exists(dumpXml)) return;

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.UserAgent = Shared.UserAgent;
            using var response = (HttpWebResponse) await request.GetResponseAsync();
            await using var file = new FileStream(dumpGz, FileMode.CreateNew);
            var stream = response.GetResponseStream();
            if (stream != null) await stream.CopyToAsync(file);
        }

        private async Task ExtractGz(string gzLocation, string outputLocation)
        {
            if (File.Exists(dumpXml)) return;

            await using var inStream = new FileStream(gzLocation, FileMode.Open, FileAccess.Read);
            await using var zipStream = new GZipStream(inStream, CompressionMode.Decompress);
            await using var outStream = new FileStream(outputLocation, FileMode.Create, FileAccess.Write);
            var tempBytes = new byte[4096];
            int i;
            while ((i = zipStream.Read(tempBytes, 0, tempBytes.Length)) != 0) outStream.Write(tempBytes, 0, i);
            File.Delete(dumpGz);
        }

        public async Task<double> EndOfMajor()
        {
            await EnsureDumpReady();

            return int.Parse(XElement.Load(dumpXml).LastNode.XPathSelectElement("LASTUPDATE").Value);
        }

        public async Task<List<Region>> Regions()
        {
            await EnsureDumpReady();

            if (regions != null) return regions;

            var api = RepoApi.Api;
            regions = new List<Region>();
            var regionsXml = XElement.Load(dumpXml).Elements("REGION");
            numNations = 0;

            foreach (var element in regionsXml)
            {
                var name = element.XPathSelectElement("NAME").Value;
                var nations = int.Parse(element.XPathSelectElement("NUMNATIONS").Value);
                numNations += nations;

                regions.Add(new Region
                {
                    name = name,
                    url = $"https://www.nationstates.net/region={name.ToLower().Replace(" ", "_")}",
                    nationCount = nations,
                    nationCumulative = numNations,
                    delegateVotes = int.Parse(element.XPathSelectElement("DELEGATEVOTES").Value),
                    delegateAuthority = element.XPathSelectElement("DELEGATEAUTH").Value,
                    founderless = (await api.TaggedFounderless()).Contains(name),
                    password = (await api.TaggedPassword()).Contains(name),
                    tagged = (await api.TaggedInvader()).Contains(name),
                    majorUpdateTime = int.Parse(element.XPathSelectElement("LASTUPDATE").Value)
                });
            }

            await AddMinorUpdateTimes();
            AddReadableUpdateTimes();
            
            return regions;
        }

        private async Task AddMinorUpdateTimes()
        {
            var minorDuration = await RepoApi.Api.EndOfMinor() - TimeUtil.PosixLastMinorStart();
            var minorTick = minorDuration / numNations;
            
            for (var i = 0; i < regions.Count; i++)
                regions[i].minorUpdateTime = i == 0
                    ? regions[i].nationCount * minorTick + TimeUtil.PosixLastMinorStart()
                    : regions[i - 1].minorUpdateTime + regions[i].nationCount * minorTick;
        }

        private void AddReadableUpdateTimes()
        {
            for (var i = 0; i < regions.Count; i++)
            {
                regions[i].readableMajorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].majorUpdateTime);
                regions[i].readableMinorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].minorUpdateTime);
            }
        }
        
        public async Task<double> MajorTook()
        {
            await EnsureDumpReady();
            await Regions();

            return regions.Last().majorUpdateTime - regions.First().majorUpdateTime;
        }
        
        public async Task<double> MinorTook()
        {
            await EnsureDumpReady();
            await Regions();

            return regions.Last().minorUpdateTime - regions.First().minorUpdateTime;
        }

        public async Task<double> MajorTick()
        {
            await EnsureDumpReady();
            await Regions();

            return await MajorTook() / (await NumNations() + 0.0);
        }

        public async Task<double> MinorTick()
        {
            await EnsureDumpReady();
            await Regions();

            return await MinorTook() / (await NumNations() + 0.0);
        }

        public async Task<int> NumNations()
        {
            await EnsureDumpReady();
            await Regions();

            return numNations;
        }
    }
}