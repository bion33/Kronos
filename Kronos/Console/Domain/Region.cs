using Console.Repo;

namespace Console.Domain
{
    public class Region
    {
        public string name;
        public string url;
        public int nationCount;
        public int delegateVotes;
        public string delegateAuthority;
        public bool founderless;
        public bool password;
        public bool tagged;
        public double majorUpdateTime;
        public string readableMajorUpdateTime;
        public double minorUpdateTime;
        public string readableMinorUpdateTime;
    }
}