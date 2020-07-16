namespace Console.Repo
{
    /// <summary> Singleton-like variables which different classes may depend upon </summary>
    public static class Shared
    {
        private static string userAgent;

        /// <summary> User-Agent for requests to the NS server </summary>
        public static string UserAgent => userAgent ??= RepoStorage.GetUserInfo();
    }
}