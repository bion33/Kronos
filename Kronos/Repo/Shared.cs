using System;

namespace Kronos.Repo
{
    /// <summary> Singleton-like variables which different classes may depend upon </summary>
    public static class Shared
    {
        public static string ExePath => AppContext.BaseDirectory;
        public static long BytesDownloaded;
    }
}