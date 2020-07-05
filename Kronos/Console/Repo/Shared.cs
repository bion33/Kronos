using System.Collections.Generic;
using Console.UI;

namespace Console.Repo
{
    public static class Shared
    {
        private static string userAgent;
        public static string UserAgent => userAgent ??= RepoStorage.GetUserInfo();
        
        private static RepoApi api;
        public static RepoApi Api => api ??= new RepoApi();
    }
}