using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.Inventory;
using XivGearExport.Windows;
using Lumina.Excel;
using System;

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

    private const string ConfigCommandName = "/xivgearexportconfig";
    private const string ExportCommandName = "/xivgearexport";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("XivGearExport");
    private ConfigWindow ConfigWindow { get; init; }

    private ExcelSheet<Lumina.Excel.Sheets.Materia> Materia { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.ClassJob> ClassJobs { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.Tribe> Races { get; init; }
    private ExcelSheet<Lumina.Excel.Sheets.ClassJobCategory> ClassJobCategories { get; init; }
    private Exporter Exporter { get; set; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();


        Materia = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Materia>() ?? throw new InvalidOperationException("Materia sheet not found");
        ClassJobs = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.ClassJob>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("ClassJobs sheet not found");
        ClassJobCategories = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.ClassJobCategory>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("ClassJobCategory sheet not found");
        Races = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Tribe>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("Tribes sheet not found");

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Type /xivgearexportconfig to change the config."
        });


        CommandManager.AddHandler(ExportCommandName, new CommandInfo(OnExportCommand)
        {
            HelpMessage = "Type /xivgearexport to export your gearset to xivgear.app."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

        var client = new System.Net.Http.HttpClient();
        Exporter = new Exporter(client, Log, ChatGui);
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(ConfigCommandName);
    }

    private void OnConfigCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private static bool HasSoulCrystalEquipped(ReadOnlySpan<GameInventoryItem> items)
    {
        foreach (GameInventoryItem item in items)
        {
            if (item.InventorySlot == 13 &&  item.ItemId != 0)
            {
                return true;
            }
        }
        return false;
    }

    private void OnExportCommand(string command, string args)
    {
        var player = ClientState.LocalPlayer;
        if (player == null)
        {
            return;
        }
        var equippedItems = GameInventory.GetInventoryItems(GameInventoryType.EquippedItems);
        var isJob = HasSoulCrystalEquipped(equippedItems);

        if (!isJob)
        {
            ChatGui.PrintError("Cannot create xivgear.app set for non-job or non-combat job.");
            return;
        }

        var jobRow = player.ClassJob.RowId;
        var job = ClassJobs.GetRow(jobRow);

        var jobAbbreviation = job.Abbreviation.ExtractText();

        var playerCustomizeInfo = player.Customize;
        var race = playerCustomizeInfo[(int)Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex.Tribe];

        // Note: feminine vs masculine here is for grammatical gender, in English it's the same
        var raceName = Races.GetRowAt(race).Feminine.ExtractText();
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

        var items = XivGearItems.CreateItemsFromGameInventoryItems(equippedItems, Materia);

        try
        {
            Exporter.Export(items, playerInfo, Configuration);
        }
        catch (XivExportException ex)
        {
            ChatGui.PrintError("An error happened when trying to export this gear:\n" + ex.Message);
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
