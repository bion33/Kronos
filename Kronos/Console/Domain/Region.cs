namespace Console.Domain
{
    /// <summary> Region data class </summary>
    public class Region
    {
        public string delegateAuthority;        // String with several characters each representing an officer authority held by this region's WA Delegate
        public int delegateVotes;               // WA votes for this region's delegate
        public bool founderless;                // Whether this region is without founder
        public double majorUpdateTime;          // Unix timestamp for the last major update of this region
        public double minorUpdateTime;          // Unix timestamp for the last minor update of this region
        public string name;                     // Human readable region name
        public int nationCount;                 // Nations within this region
        public int nationCumulative;            // Amount of nations in this region and all regions which update before this region combined
        public bool password;                   // Whether this region is password-protected
        public string readableMajorUpdateTime;  // HH:MM:SS offset timestamp for major
        public string readableMinorUpdateTime;  // HH:MM:SS offset timestamp for minor
        public bool tagged;                     // Whether this region is tagged "invader"
        public string url;                      // The url for this region
    }
}