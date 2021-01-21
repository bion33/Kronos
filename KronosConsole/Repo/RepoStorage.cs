using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KronosConsole.UI;

namespace KronosConsole.Repo
{
    /// <summary> Get & store information on disk </summary>
    public static class RepoStorage
    {
        private const string ConfigPath = "config.txt";

        private const string UserInfo = "UserInfo";
        private const string RaiderRegions = "RaiderRegions";
        private const string IndependentRegions = "IndependentRegions";
        private const string DefenderRegions = "DefenderRegions";
        private const string PriorityRegions = "PriorityRegions";

        /// <summary>
        ///     Retrieves the user's information (nation name, email, ...) from the configuration file. If said file
        ///     doesn't exist or doesn't contain that information, ask the user and store the response in the config file.
        /// </summary>
        public static string GetUserInfo()
        {
            // Open file, or create if it doesn't exist yet
            using (var file = File.Open(ConfigPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                // Read configuration
                using var reader = new StreamReader(file, Encoding.UTF8);
                var config = reader.ReadToEnd();
                var configLines = config.Split("\n");
                var userInfo = "";
                foreach (var line in configLines)
                    if (line.Contains(UserInfo))
                    {
                        userInfo = line.Replace($"{UserInfo}: ", "");
                        break;
                    }

                // Return user info if it was present.
                if (userInfo.Length > 0) return userInfo;
            }

            // If no user info present (didn't return), ask the user, and save it to the config file
            using (var fileWrite = File.Open(ConfigPath, FileMode.Append, FileAccess.Write))
            {
                using var writer = new StreamWriter(fileWrite, Encoding.UTF8);
                var userInfo = UIConsole.GetUserInfo();
                writer.WriteAsync($"{UserInfo}: {userInfo}\n");
                return userInfo;
            }
        }

        /// <summary>
        ///     Retrieves the regions associated by the user with a particular tag from the configuration file. If said
        ///     file doesn't contain that information, add the required configuration fields.
        /// </summary>
        public static Dictionary<string, string> GetUserTags()
        {
            var tagRegions = new Dictionary<string, string>();
            string[] configLines;

            // Open file
            using (var file = File.Open(ConfigPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                // Read configuration
                using var reader = new StreamReader(file, Encoding.UTF8);
                var config = reader.ReadToEnd();
                configLines = config.Split("\n");

                foreach (var line in configLines)
                {
                    // Parse
                    var value = ParseValueList(line);

                    // Assign
                    switch (line.Split(":")[0])
                    {
                        case RaiderRegions:
                            value.ForEach(r => tagRegions[r] = RaiderRegions);
                            break;
                        case IndependentRegions:
                            value.ForEach(r => tagRegions[r] = IndependentRegions);
                            break;
                        case DefenderRegions:
                            value.ForEach(r => tagRegions[r] = DefenderRegions);
                            break;
                        case PriorityRegions:
                            value.ForEach(r => tagRegions[r] = PriorityRegions);
                            break;
                    }
                }
            }

            // Add missing settings to file
            using (var fileWrite = File.Open(ConfigPath, FileMode.Append, FileAccess.Write))
            {
                using var writer = new StreamWriter(fileWrite, Encoding.UTF8);

                var settings = new List<string> {RaiderRegions, IndependentRegions, DefenderRegions, PriorityRegions};
                var missing = settings.Where(s => configLines.FirstOrDefault(l => l.StartsWith(s)) == null);
                foreach (var setting in missing) writer.WriteAsync($"{setting}: \n");
            }

            return tagRegions;
        }

        /// <summary>
        ///     Parse a line of the format "SettingList: one, two, three" to a list
        /// </summary>
        private static List<string> ParseValueList(string value)
        {
            try
            {
                return value
                    .Replace(", ", ",")
                    .Replace(": ", ":")
                    .TrimEnd()
                    .Split(":")[1]
                    .TrimStart()
                    .Replace(" ", "_")
                    .ToLower()
                    .Split(",")
                    .ToList();
            }
            // Empty line
            catch (IndexOutOfRangeException)
            {
                return new List<string>();
            }
        }
    }
}