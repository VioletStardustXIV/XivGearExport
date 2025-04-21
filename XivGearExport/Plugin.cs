using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.Inventory;
using XivGearExport.Windows;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismPrismSetConvert.AgentData;
using Lumina.Excel;
using System;
using System.Net.Http;
using Lumina.Excel.Sheets;
using Serilog;
using static FFXIVClientStructs.FFXIV.Client.Game.UI.NpcTrade;
using Item = Lumina.Excel.Sheets.Item;

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
    private XivGearExport.Exporter Exporter { get; set; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();


        Materia = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Materia>() ?? throw new InvalidOperationException("Materia sheet not found");
        ClassJobs = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.ClassJob>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("ClassJobs sheet not found");
        Races = DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Tribe>(language: Lumina.Data.Language.English) ?? throw new InvalidOperationException("Tribes sheet not found");

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Type /xivgearexportconfig to fiddle with the config."
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
        Exporter = new XivGearExport.Exporter(client, Log, ChatGui);
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

    private void OnExportCommand(string command, string args)
    {
        var player = ClientState.LocalPlayer;
        if (player != null)
        {
            var jobRow = player.ClassJob.RowId;
            var jobAbbreviation = ClassJobs.GetRow(jobRow).Abbreviation.ExtractText();

            var playerCustomizeInfo = player.Customize;
            var race = playerCustomizeInfo[(int)Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex.Tribe];

            // Note: feminine vs masculine here is for grammatical gender, in English it's the same
            var raceName = Races.GetRowAt(race).Feminine.ExtractText();
            ChatGui.Print(raceName);

            PlayerInfo playerInfo = new PlayerInfo
            {
                job = jobAbbreviation,
                race = raceName,
            };

            var items = XivGearItems.CreateItemsFromGameInventoryItems(GameInventory.GetInventoryItems(GameInventoryType.EquippedItems), Materia);

            try
            {
                Exporter.Export(items, playerInfo, Configuration);
            }
            catch (XivExportException ex)
            {
                ChatGui.PrintError("An error happened when trying to export this gear:\n" + ex.Message);
            }
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
