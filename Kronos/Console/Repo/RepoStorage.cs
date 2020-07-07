using System.IO;
using Console.UI;

namespace Console.Repo
{
    public static class RepoStorage
    {
        private const string UserInfo = "UserInfo";
        private const string ConfigPath = "config.txt";

        public static string GetUserInfo()
        {
            if (!File.Exists(ConfigPath)) File.Create(ConfigPath);

            var config = File.ReadAllText(ConfigPath);
            var configLines = config.Split("\n");

            var userInfo = "";
            foreach (var line in configLines)
                if (line.Contains(UserInfo))
                {
                    userInfo = line.Replace($"{UserInfo}: ", "");
                    break;
                }

            if (userInfo.Length > 0) return userInfo;

            userInfo = UIConsole.GetUserInfo();
            var writer = File.AppendText(ConfigPath);
            writer.WriteAsync($"{UserInfo}: {userInfo}");
            writer.Close();
            return userInfo;
        }
    }
}