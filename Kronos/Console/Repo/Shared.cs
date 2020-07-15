namespace Console.Repo
{
    /// <summary> Singleton-like variables which different classes may depend upon </summary>
    public static class Shared
    {
        /// <summary> User-Agent for requests to the NS server </summary>
        public static string UserAgent => userAgent ??= RepoStorage.GetUserInfo();
        private static string userAgent;
    }
}