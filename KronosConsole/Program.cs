using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using KronosConsole.Repo;
using KronosConsole.UI;

namespace KronosConsole
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var version = GetVersion();
            
            // Greeting
            UIConsole.Show($"Kronos, at your service.\n{version}\n");
            var lu = LatestUpdate();
            if (!lu.Equals(version)) 
                UIConsole.Show($"The latest release of Kronos is {lu}. You can find it here: https://github.com/Krypton-Nova/Kronos/releases \n");

            var unusedUserAgent = Shared.UserAgent;

            // Run until Quit command is given.
            while (true)
            {
                // Pass initial options to Program, or request user input
                var commands = UIConsole.UserCommandInput(args);

                // Run commands sequentially
                foreach (var command in commands) await command.Run();

                // Reset initial options
                args = new string[0];
            }
        }

        private static string GetVersion()
        {
            try
            {
                using var reader = File.OpenText(@"README.md");
                string lines = reader.ReadToEnd();
                foreach (var l in lines.Split("\n"))
                {
                    if (!l.Contains("Latest release: ")) continue;
                    
                    return l.Split("Latest release: ")[1].Split(" ")[0];
                }
            }
            catch (FileNotFoundException) {}

            return "ERROR: Couldn't get current version from README.md!";
        }

        private static string LatestUpdate()
        {
            var response = new WebClient().DownloadString("https://raw.githubusercontent.com/Krypton-Nova/Kronos/master/README.md");

            foreach (var l in response.Split("\n"))
            {
                if (!l.Contains("Latest release: ")) continue;
                    
                return l.Split("Latest release: ")[1].Split(" ")[0];
            }

            return "ERROR: Couldn't get latest version!";
        }
    }
}