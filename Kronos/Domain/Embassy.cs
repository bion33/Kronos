namespace Kronos.Domain
{
    /// <summary> Embassy data class </summary>
    public class Embassy
    {
        public EmbassyClass EmbassyType;
        public bool Pending;
        public string Name;
    }

    /// <summary> Classifies embassies according to user tags </summary>
    public enum EmbassyClass
    {
        None,
        PriorityRegions,
        RaiderRegions,
        IndependentRegions,
        DefenderRegions
    }
}