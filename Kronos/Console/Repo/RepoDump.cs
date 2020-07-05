using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Console.Domain;
using Console.Utilities;

namespace Console.Repo
{
    public class RepoDump
    {
        private const string DumpUrl = "https://www.nationstates.net/pages/regions.xml.gz";
        private readonly string dumpGz = $".Regions_{TimeUtil.PosixToday()}.xml.gz";
        private readonly string dumpXml = $".Regions_{TimeUtil.PosixToday()}.xml";

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
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = Shared.UserAgent;
            using HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
            await using var file = new FileStream(dumpGz, FileMode.CreateNew);
            var stream = response.GetResponseStream();
            if (stream != null) await stream.CopyToAsync(file);
        }

        private async Task ExtractGz(string gzLocation, string outputLocation)
        {
            await using FileStream inStream = new FileStream(gzLocation, FileMode.Open, FileAccess.Read);
            await using GZipStream zipStream = new GZipStream(inStream, CompressionMode.Decompress);
            await using FileStream outStream = new FileStream(outputLocation, FileMode.Create, FileAccess.Write);
            byte[] tempBytes = new byte[4096];
            int i;
            while ((i = zipStream.Read(tempBytes, 0, tempBytes.Length)) != 0) {
                outStream.Write(tempBytes, 0, i);
            }
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

            var api = Shared.Api;
            var regions = new List<Region>();
            var regionsXml = XElement.Load(dumpXml).Elements("REGION");
            
            foreach (var element in regionsXml)
            {
                var name = element.XPathSelectElement("NAME").Value;
                regions.Add(new Region()
                {
                    name = name, 
                    url = $"https://www.nationstates.net/region={name.ToLower().Replace(" ", "_")}",
                    nationCount = int.Parse(element.XPathSelectElement("NUMNATIONS").Value),
                    delegateVotes = int.Parse(element.XPathSelectElement("DELEGATEVOTES").Value),
                    delegateAuthority = element.XPathSelectElement("DELEGATEAUTH").Value,
                    founderless = (await api.TaggedFounderless()).Contains(name),
                    password = (await api.TaggedPassword()).Contains(name),
                    tagged = (await api.TaggedInvader()).Contains(name)
                });
            }

            return regions;
        }
    }
}