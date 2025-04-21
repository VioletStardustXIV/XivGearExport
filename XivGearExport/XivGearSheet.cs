using System.Collections.Generic;

namespace XivGearExport
{
    internal class XivGearSheet
    {
        public string job { get; set; }
        public int level { get; set; }
        public int partyBonus { get; set; }
        public string race { get; set; }
        public string name { get; set; }
        public string description { get; set; }

        public IList<XivGearSet> sets { get; set; }
    }
}
