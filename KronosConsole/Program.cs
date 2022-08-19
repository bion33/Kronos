using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using KronosConsole.Repo;
using KronosConsole.UI;

namespace KronosConsole
{
    internal static class Program
    {
        /// <summary> Entry point for the console application </summary>
        private static async Task Main(string[] args)
        {
            var version = GetVersion();

            // Greeting
            UIConsole.Show($"Kronos, at your service.\n{version}\n");
            var lu = await LatestUpdate();
            if (!lu.Equals(version))
                UIConsole.Show(
                    $"The latest release of Kronos is {lu}. You can find it here: https://github.com/Krypton-Nova/Kronos/releases \n");

            // Read settings
            var unusedUserAgent = Shared.UserAgent;
            var unusedUserTags = Shared.UserTags;

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

        /// <summary> Get this version of Kronos from the README.md file </summary>
        private static string GetVersion(string path = @"README.md")
        {
            try
            {
                using var reader = File.OpenText(Path.Combine(Shared.ExePath, path));
                var lines = reader.ReadToEnd();
                foreach (var l in lines.Split("\n"))
                {
                    if (!l.Contains("Latest release: ")) continue;

                    return l.Split("Latest release: ")[1].Split(" ")[0];
                }
            }
            catch (FileNotFoundException)
            {
            }

            return "ERROR: Couldn't get current version from README.md!";
        }

        /// <summary> Get the latest version of Kronos from the README.md file in the repository </summary>
        private static async Task<string> LatestUpdate()
        {
            string resource = await new Url("https://raw.githubusercontent.com/Krypton-Nova/Kronos/master/README.md")
                .GetStringAsync();
            
            foreach (var l in resource.Split("\n"))
            {
                if (!l.Contains("Latest release: ")) continue;

                return l.Split("Latest release: ")[1].Split(" ")[0];
            }

            return "ERROR: Couldn't get latest version!";
        }
    }
}