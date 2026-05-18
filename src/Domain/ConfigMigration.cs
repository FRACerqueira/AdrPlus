// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Domain
{
    internal sealed record ConfigMigration
    {
        public string Sample { get; set; } = "";
        public int Prefix { get; set; }
        public int LenPrefix { get; set; }
        public int Number { get; set; }
        public int LenNumber { get; set; }
        public int Version { get; set; }
        public int LenVersion { get; set; }
        public int Revision { get; set; }
        public int LenRevision { get; set; }
        public int Title { get; set; }
    };
}
