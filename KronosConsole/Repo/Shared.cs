using System.Collections.Generic;

namespace KronosConsole.Repo
{
    /// <summary> Singleton-like variables which different classes may depend upon </summary>
    public static class Shared
    {
        private static string userAgent;
        private static Dictionary<string, string> userTags;

        /// <summary> User-Agent for requests to the NS server </summary>
        public static string UserAgent => userAgent ??=
            "Application: Kronos (https://github.com/Krypton-Nova/Kronos); User: " + RepoStorage.GetUserInfo();

        /// <summary> Regions the user has associated with a particular tag </summary>
        public static Dictionary<string, string> UserTags => userTags ??= RepoStorage.GetUserTags();
    }
}