using System.Collections.Generic;

namespace XivGearExport
{
    internal class XivGearSheet
    {
        public required string job { get; set; }
        public int level { get; set; }
        public int partyBonus { get; set; }
        public required string race { get; set; }
        public required string name { get; set; }
        public required string description { get; set; }

        public required IList<XivGearSet> sets { get; set; }
    }
}
