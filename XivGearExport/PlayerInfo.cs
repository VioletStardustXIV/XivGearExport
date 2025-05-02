namespace XivGearExport
{
    internal class PlayerInfo
    {
        public required string Job { get; set; }
        public required string Race { get; set; }
        public int Level { get; set; } = 100;
        
        public int PartyBonus { get; set; } = 5;
    }
}
