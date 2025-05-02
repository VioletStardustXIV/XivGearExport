using Dalamud.Configuration;
using System;

namespace XivGearExport;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ExportSetInEditMode { get; set; } = true;
    public bool ExportSetInReadOnlyMode { get; set; } = false;
    public bool OpenUrlInBrowserAutomatically { get; set; } = true;
    public bool PrintUrlToChat { get; set; } = false;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
