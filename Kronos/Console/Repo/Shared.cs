namespace Console.Repo
{
    public static class Shared
    {
        private static string userAgent;
        public static string UserAgent => userAgent ??= RepoStorage.GetUserInfo();
    }
}