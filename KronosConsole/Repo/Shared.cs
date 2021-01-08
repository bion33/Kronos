namespace KronosConsole.Repo
{
    /// <summary> Singleton-like variables which different classes may depend upon </summary>
    public static class Shared
    {
        private static string userAgent;

        /// <summary> User-Agent for requests to the NS server </summary>
        public static string UserAgent => userAgent ??=
            "Application: Kronos (https://github.com/Krypton-Nova/Kronos); User: " + RepoStorage.GetUserInfo();
    }
}