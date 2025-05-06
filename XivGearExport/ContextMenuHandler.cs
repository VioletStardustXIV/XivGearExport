using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;

namespace XivGearExport;

using Dalamud.Game.Gui.ContextMenu;

public class ContextMenuHandler
{
    private IDalamudPluginInterface pluginInterface;
    private IChatGui chatGui;
    private IContextMenu contextMenu;
    private Configuration configuration;
    private IClientState clientState;
    private Exporter exporter;
    private ExcelSheet<Lumina.Excel.Sheets.Tribe> Races;
    private ExcelSheet<Lumina.Excel.Sheets.Materia> Materia;
    private ExcelSheet<Lumina.Excel.Sheets.ClassJob> ClassJobs;


    public ContextMenuHandler(IDalamudPluginInterface pluginInterface, IChatGui chatGui, IContextMenu contextMenu, Configuration configuration, 
        IClientState clientState, Exporter exporter, ExcelSheet<Lumina.Excel.Sheets.Tribe> Races, ExcelSheet<Lumina.Excel.Sheets.Materia> Materia,
        ExcelSheet<Lumina.Excel.Sheets.ClassJob> ClassJobs)
    {
        this.pluginInterface = pluginInterface;
        this.chatGui = chatGui;
        this.contextMenu = contextMenu;
        this.configuration = configuration;
        this.clientState = clientState;
        this.exporter = exporter;
        this.Races = Races;
        this.Materia = Materia;
        this.ClassJobs = ClassJobs;

        this.contextMenu.OnMenuOpened += OnOpenContextMenu;
    }
    
    private void OnOpenContextMenu(IMenuOpenedArgs menuOpenedArgs)
    {
        if (!pluginInterface.UiBuilder.ShouldModifyUi || !IsMenuValid(menuOpenedArgs))
        {
            return;
        }

        menuOpenedArgs.AddMenuItem(new MenuItem
        {
            PrefixChar = 'X',
            Name = "Export Set To xivgear.app",
            OnClicked = ExportGearSet 
        });
    }

    private string SoulstoneIdToJobAbbreviation(uint soulstoneId)
    {
        foreach (var classJob in ClassJobs)
        {
            if (classJob.ItemSoulCrystal.RowId == soulstoneId)
            {
                return classJob.Abbreviation.ExtractText();
            }
        }

        return "";
    }

    private unsafe void ExportGearSet (IMenuItemClickedArgs args)
    {
        if (args.Target is not MenuTargetDefault)
        {
            return;
        }

        var agent = (nint)AgentGearSet.Instance();
        // don't worry about it kitten
        // okay
        // yay
        var gearSetId = *(uint*)(agent + 0x40 + 0xC * 1 + 0x4);
        
        var pointer = (AgentGearSet*)args.AgentPtr;
        var gearset = pointer->UIModuleInterface->GetRaptureGearsetModule()->GetGearset((int)gearSetId);
        var soulstone = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.SoulStone);
        if (soulstone.ItemId == 0)
        {
            chatGui.PrintError("Cannot create xivgear.app set for non-job or non-combat job.");
            return;
        }
        
        var player = clientState.LocalPlayer;
        if (player == null)
        {
            chatGui.PrintError("Something went wrong when creating xivgear.app set (player was null).");
            return;
        }
        var playerCustomizeInfo = player.Customize;
        var race = playerCustomizeInfo[(int)Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex.Tribe];

        // Note: feminine vs masculine here is for grammatical gender, in English it's the same
        var raceName = Races.GetRowAt(race).Feminine.ExtractText();
        raceName = XivGearSheet.ConvertRaceNameToXivGearRaceName(raceName);
        
        var jobAbbreviation = SoulstoneIdToJobAbbreviation(soulstone.ItemId);
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
        
        var gearItems = XivGearItems.CreateItemsFromGearset(gearset, Materia);
        
        try
        {
            exporter.Export(gearItems, playerInfo, configuration);
        }
        catch (XivExportException ex)
        {
            chatGui.PrintError("An error happened when trying to export this gear:\n" + ex.Message);
        }
    }
    
    private bool IsMenuValid(IMenuArgs menuOpenedArgs)
    {
        if (!configuration.EnableGearsetMenuItem)
        {
            return false;
        }
        
        if (menuOpenedArgs.Target is not MenuTargetDefault menuTargetDefault)
        {
            return false;
        }

        return menuOpenedArgs.AddonName switch
        {
            "GearSetList" => true,
            _ => false
        };
    }
    
    public void Dispose()
    {
        contextMenu.OnMenuOpened -= OnOpenContextMenu;
    }
    
}