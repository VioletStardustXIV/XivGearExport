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
    private ExcelSheet<Lumina.Excel.Sheets.Tribe> racesSheet;
    private ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet;
    private ExcelSheet<Lumina.Excel.Sheets.ClassJob> classJobsSheet;
    private ExcelSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance> mandervilleSheet;
    private ExcelSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust> bozjaSheet;


    public ContextMenuHandler(IDalamudPluginInterface pluginInterface, IChatGui chatGui, IContextMenu contextMenu, Configuration configuration, 
        IClientState clientState, Exporter exporter, ExcelSheet<Lumina.Excel.Sheets.Tribe> racesSheet, ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet,
        ExcelSheet<Lumina.Excel.Sheets.ClassJob> classJobsSheet, ExcelSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance> mandervilleSheet,
            ExcelSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust> bozjaSheet)
    {
        this.pluginInterface = pluginInterface;
        this.chatGui = chatGui;
        this.contextMenu = contextMenu;
        this.configuration = configuration;
        this.clientState = clientState;
        this.exporter = exporter;
        this.racesSheet = racesSheet;
        this.materiaSheet = materiaSheet;
        this.classJobsSheet = classJobsSheet;
        this.mandervilleSheet = mandervilleSheet;
        this.bozjaSheet = bozjaSheet;

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
        foreach (var classJob in classJobsSheet)
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
        var soulStone = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.SoulStone);
        if (soulStone.ItemId == 0 || PlayerInfo.IsDoHSoulstone(soulStone.ItemId))
        {
            chatGui.PrintError("Cannot create xivgear.app set for non-job or non-combat job.");
            return;
        }
        
        try
        {
            var playerInfo = PlayerInfo.GetPlayerInfo(clientState, classJobsSheet, racesSheet);
            // For menu export, we need to get the job of the soulstone in the set, not the player's current job.
            playerInfo.Job = SoulstoneIdToJobAbbreviation(soulStone.ItemId);
            
            var gearItems = XivGearItems.CreateItemsFromGearset(gearset, materiaSheet, mandervilleSheet, bozjaSheet);
            exporter.Export(gearItems, playerInfo, configuration, gearset->NameString);
        }
        catch (XivExportException ex)
        {
            chatGui.PrintError("An error happened when trying to export this gear: " + ex.Message);
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