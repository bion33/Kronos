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
        Queue<string> queue = new Queue<string>();

        public RepoApi()
        {
            Shared.UserAgent = $"Kronos (https://github.com/Krypton-Nova/Kronos-NET). User info: {RepoStorage.GetUserInfo()}";
        }
        
        public async Task<string> GetAsync(string url)
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

        public async Task<List<string>> GetDelegateChangesBetween(double start, double end)
        {
            var api = Shared.Api;

            List<string> waHappenings = new List<string>();
            var more = true;
            var i = 0;

            while (more)
            {
                more = false;
                var url = $"https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=member;sincetime={start};beforetime={end - (i * 2400)};limit=200";
                var response = await GetAsync(url);
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

        public async Task<List<string>> GetRegionsByTag(string tag)
        {
            var url = $"https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags={tag}";
            var response = await GetAsync(url);
            response = response.Replace("\n", "");
            var found = RegexUtil.Find(response, "<REGIONS>(.*?)</REGIONS>");
            found = found.Replace("['", "").Replace("']", "");
            var tagged = found.Split(",").ToList();
            return tagged;
        }
        
        public async Task<List<string>> GetInvaders()
        {
            if (Shared.Invaders != null) return Shared.Invaders;

            Shared.Invaders = await GetRegionsByTag("invader");
            return Shared.Invaders;
        }
        
        public async Task<List<string>> GetDefenders()
        {
            if (Shared.Defenders != null) return Shared.Defenders;
            
            Shared.Defenders = await GetRegionsByTag("defender");
            return Shared.Defenders;
        }
    }
}