using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace XivGearExport.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("XivGearExport Config###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 180);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw() { }

    public override void Draw()
    {
        var exportEditModeConfig = Configuration.ExportSetInEditMode;
        if (ImGui.Checkbox("Export Set In Edit Mode", ref exportEditModeConfig))
        {
            Configuration.ExportSetInEditMode = exportEditModeConfig;
            Configuration.Save();
        }

        var exportReadOnlyModeConfig = Configuration.ExportSetInReadOnlyMode;
        if (ImGui.Checkbox("Export Set In Read Only Mode", ref exportReadOnlyModeConfig))
        {
            Configuration.ExportSetInReadOnlyMode = exportReadOnlyModeConfig;
            Configuration.Save();
        }

        var openConfig = Configuration.OpenURLInBrowserAutomatically;
        if (ImGui.Checkbox("Open URL Automatically", ref openConfig))
        {
            Configuration.OpenURLInBrowserAutomatically = openConfig;
            Configuration.Save();
        }

        var printConfig = Configuration.PrintURLToChat;
        if (ImGui.Checkbox("Print URL to Chat", ref printConfig))
        {
            Configuration.PrintURLToChat = printConfig;
            Configuration.Save();
        }
    }
}
