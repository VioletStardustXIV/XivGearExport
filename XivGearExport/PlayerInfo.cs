using Dalamud.Plugin.Services;
using Lumina.Excel;

namespace XivGearExport
{
    public class PlayerInfo
    {
        public required string Job { get; set; }
        public required string Race { get; set; }
        public int Level { get; set; } = 100;
        
        public int PartyBonus { get; set; } = 5;
        
        public static PlayerInfo GetPlayerInfo(IClientState clientState, ExcelSheet<Lumina.Excel.Sheets.ClassJob> classJobs, ExcelSheet<Lumina.Excel.Sheets.Tribe> races)
        {
            var player = clientState.LocalPlayer;
            if (player == null)
            {
                throw new XivExportException("player was null, cannot get player info");
            }
            
            var jobRow = player.ClassJob.RowId;
            var job = classJobs.GetRow(jobRow);
            var jobAbbreviation = job.Abbreviation.ExtractText();

            var playerCustomizeInfo = player.Customize;
            var race = playerCustomizeInfo[(int)Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex.Tribe];

            // Note: feminine vs masculine here is for grammatical gender, in English it's the same
            var raceName = races.GetRowAt(race).Feminine.ExtractText();
            raceName = XivGearSheet.ConvertRaceNameToXivGearRaceName(raceName);

            var playerInfo = new PlayerInfo
            {
                Job = jobAbbreviation,
                Race = raceName,
                Level = 100,
                PartyBonus = 5,
            };

            if (jobAbbreviation == "BLU")
            {
                playerInfo.Level = 80;
                playerInfo.PartyBonus = 1;
            }

            return playerInfo;
        }
    }
}
