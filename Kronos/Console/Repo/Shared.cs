using System;
using System.Collections.Generic;

namespace Console.Repo
{
    public static class Shared
    {
        private static RepoApi api;
        public static RepoApi Api
        {
            get {
                if (api == null) api = new RepoApi();
                return api;
            }
        }
        
        private static string userAgent;
        public static string UserAgent
        {
            get => userAgent;
            set { userAgent ??= value; }
        }
        
        private static List<string> invaders;
        public static List<string> Invaders
        {
            get => invaders;
            set { invaders ??= value; }
        }
        
        private static List<string> defenders;
        public static List<string> Defenders
        {
            get => defenders;
            set { defenders ??= value; }
        }
    }
}