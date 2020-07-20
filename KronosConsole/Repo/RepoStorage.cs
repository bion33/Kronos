using System.IO;
using System.Text;
using KronosConsole.UI;

namespace KronosConsole.Repo
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

            // If not, ask the user, and save it to the config file
            using (var fileWrite = File.Open(ConfigPath, FileMode.Append, FileAccess.Write))
            {
                using var writer = new StreamWriter(fileWrite, Encoding.UTF8);
                var userInfo = UIConsole.GetUserInfo();
                writer.WriteAsync($"{UserInfo}: {userInfo}");
                return userInfo;
            }
        }
    }
}