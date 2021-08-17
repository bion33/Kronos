using System.Collections.Generic;

namespace Kronos.Domain
{
    /// <summary> Region data class </summary>
    public class Region
    {
        public string
            DelegateAuthority; // String with several characters each representing an officer authority held by this region's WA Delegate

        public int DelegateVotes; // WA votes for this region's delegate

        public List<Embassy> Embassies; // List of embassies and pending embassies
        public bool Founderless;        // Whether this region is without founder
        public double MajorUpdateTime;  // Unix timestamp for the last major update of this region
        public double MinorUpdateTime;  // Unix timestamp for the last minor update of this region
        public string Name;             // Human readable region name
        public int NationCount;         // Nations within this region

        public int
            NationCumulative; // Amount of nations in this region and all regions which update before this region combined

        public bool Password;                  // Whether this region is password-protected
        public string ReadableMajorUpdateTime; // HH:MM:SS offset timestamp for major
        public string ReadableMinorUpdateTime; // HH:MM:SS offset timestamp for minor
        public bool Tagged;                    // Whether this region is tagged "invader"
        public string Url;                     // The url for this region

        public override string ToString()
        {
            return Name;
        }
    }
}