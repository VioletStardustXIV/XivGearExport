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
        IClientState clientState, Exporter exporter, ExcelSheet<Lumina.Excel.Sheets.Tribe> races, ExcelSheet<Lumina.Excel.Sheets.Materia> materia,
        ExcelSheet<Lumina.Excel.Sheets.ClassJob> classJobs)
    {
        this.pluginInterface = pluginInterface;
        this.chatGui = chatGui;
        this.contextMenu = contextMenu;
        this.configuration = configuration;
        this.clientState = clientState;
        this.exporter = exporter;
        this.Races = races;
        this.Materia = materia;
        this.ClassJobs = classJobs;

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
        if (soulStone.ItemId == 0)
        {
            chatGui.PrintError("Cannot create xivgear.app set for non-job or non-combat job.");
            return;
        }
        
        try
        {
            var playerInfo = PlayerInfo.GetPlayerInfo(clientState, ClassJobs, Races);
            var gearItems = XivGearItems.CreateItemsFromGearset(gearset, Materia);
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