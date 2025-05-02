using System.Collections.Generic;
using Newtonsoft.Json;

namespace XivGearExport
{
    internal class XivGearSheet
    {
        [JsonProperty("job")]
        public required string Job { get; set; }
        
        [JsonProperty("level")]
        public int Level { get; set; }
        
        [JsonProperty("partyBonus")]
        public int PartyBonus { get; set; }
        
        [JsonProperty("race")]
        public required string Race { get; set; }
        
        [JsonProperty("name")]
        public required string Name { get; set; }
        
        [JsonProperty("description")]
        public required string Description { get; set; }
        
        [JsonProperty("sets")]
        public required IList<XivGearSet> Sets { get; set; }
        
        // xivgear uses different race names to what's in Excel.
        // This function corrects the ones that are different.
        public static string ConvertRaceNameToXivGearRaceName(string raceName)
        {
            if (raceName == "Keeper of the Moon")
            {
                raceName = "Keepers of the Moon";
            }

            if (raceName == "Seeker of the Sun")
            {
                raceName = "Seekers of the Sun";
            }

            if (raceName == "Helions")
            {
                raceName = "Helion";
            }

            return raceName;
        }

    }
}
