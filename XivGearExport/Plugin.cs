using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.Inventory;
using XivGearExport.Windows;
using Lumina.Excel;
using System;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Extensions;

namespace XivGearExport;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IGameInventory GameInventory { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;

    private const string ConfigCommandName = "/xivgearexportconfig";
    private const string ConfigShortCommandName = "/xgeconfig";
    private const string ExportCommandName = "/xivgearexport";
    private const string ExportShortCommandName = "/xge";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("XivGearExport");
    private ConfigWindow ConfigWindow { get; init; }

    private ExcelSheet<Lumina.Excel.Sheets.Item> Items { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.MateriaGrade> MateriaGrades { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.Materia> Materia { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance> MandervilleWeaponEnhance { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust> ResistanceWeaponAdjust { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.ClassJob> ClassJobs { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.Tribe> Races { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.ClassJobCategory> ClassJobCategories { get; init; }
    private Exporter Exporter { get; set; }
    
    private ContextMenuHandler ContextMenuHandler { get; set; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        Items = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Item>() ?? throw new InvalidOperationException("Item sheet not found");
        Materia = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Materia>() ?? throw new InvalidOperationException("Materia sheet not found");
        MateriaGrades = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.MateriaGrade>() ?? throw new InvalidOperationException("MateriaGrade sheet not found");
        ResistanceWeaponAdjust = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust>() ?? throw new InvalidOperationException("ResistanceWeaponAdjust sheet not found");
        MandervilleWeaponEnhance = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance>() ?? throw new InvalidOperationException("MandervilleWeaponEnhance sheet not found");
        ClassJobs = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.ClassJob>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("ClassJobs sheet not found");
        ClassJobCategories = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.ClassJobCategory>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("ClassJobCategory sheet not found");
        Races = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Tribe>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("Tribes sheet not found");
        
        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Type /xivgearexportconfig or /xgeconfig to change the config."
        });
        
        CommandManager.AddHandler(ConfigShortCommandName, new CommandInfo(OnConfigCommand)
        {
            ShowInHelp = false
        });
        
        CommandManager.AddHandler(ExportCommandName, new CommandInfo(OnExportCommand)
        {
            HelpMessage = "Type /xivgearexport or /xge to export your gearset to xivgear.app."
        });
        
        CommandManager.AddHandler(ExportShortCommandName, new CommandInfo(OnExportCommand)
        {
            ShowInHelp = false
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

        var client = new System.Net.Http.HttpClient();
        Exporter = new Exporter(client, Log, ChatGui);

        ContextMenuHandler = new ContextMenuHandler(PluginInterface, ChatGui, ContextMenu, Configuration, ClientState, Exporter, Races, Materia, ClassJobs, MandervilleWeaponEnhance, ResistanceWeaponAdjust);
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ContextMenuHandler.Dispose();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(ConfigCommandName);
    }

    private void OnConfigCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private static bool HasCombatJobSoulCrystalEquipped(ReadOnlySpan<GameInventoryItem> items)
    {
        foreach (var item in items)
        {
            if (item.InventorySlot == 13 && item.ItemId != 0 && !PlayerInfo.IsDoHSoulstone(item.ItemId))
            {
                return true;
            }
        }
        return false;
    }

    private void OnExportCommand(string command, string args)
    {
        var equippedItems = GameInventory.GetInventoryItems(GameInventoryType.EquippedItems);
        var isJob = HasCombatJobSoulCrystalEquipped(equippedItems);

        if (!isJob)
        {
            ChatGui.PrintError("Cannot create xivgear.app set for non-job or non-combat job.");
            return;
        }
        
        try
        {
            var playerInfo = PlayerInfo.GetPlayerInfo(ClientState, ClassJobs, Races);
            var items = XivGearItems.CreateItemsFromGameInventoryItems(equippedItems, Materia, MandervilleWeaponEnhance, ResistanceWeaponAdjust);
            var setName = GetCurrentGearsetName();

            Exporter.Export(items, playerInfo, Configuration, setName);
        }
        catch (XivExportException ex)
        {
            ChatGui.PrintError("An error happened when trying to export this gear: " + ex.Message);
        }
    }

    private unsafe string GetCurrentGearsetName()
    {
        var module = RaptureGearsetModule.Instance();
        var currentGearsetIndex = module->CurrentGearsetIndex;
        if (!module->IsValidGearset(currentGearsetIndex))
        {
            return "Exported Set";
        }

        var gearset = module->GetGearset(currentGearsetIndex);
        if (gearset == null)
        {
            return "Exported Set";
        }

        return gearset->NameString;
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
