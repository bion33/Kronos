using System.IO;
using Console.UI;

namespace Console.Repo
{
    /// <summary> Get & store information on disk </summary>
    public static class RepoStorage
    {
        private const string UserInfo = "UserInfo";
        private const string ConfigPath = "config.txt";

        /// <summary>
        ///     Retrieves the user's information (nation name, email, ...) from the configuration file. If said file
        ///     doesn't exist or doesn't contain that information, ask the user and store the response in the config file.
        /// </summary>
        public static string GetUserInfo()
        {
            // Create config file if it doesn't exist yet.
            if (!File.Exists(ConfigPath)) File.Create(ConfigPath);

            // Read configuration
            var config = File.ReadAllText(ConfigPath);
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

            // If not, ask the user, and save it to the config file
            userInfo = UIConsole.GetUserInfo();
            var writer = File.AppendText(ConfigPath);
            writer.WriteAsync($"{UserInfo}: {userInfo}");
            writer.Close();

            return userInfo;
        }
    }
}