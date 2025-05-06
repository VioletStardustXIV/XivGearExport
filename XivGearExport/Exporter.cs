using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

namespace XivGearExport
{
    public class Exporter(HttpClient httpClient, IPluginLog log, IChatGui chatGui)
    {
        private const string XivgearApiBase = "https://api.xivgear.app/shortlink/";
        private const string XivGearImportSetPrefix = "https://xivgear.app/?page=importset%7C";
        private const string XivGearReadOnlySetPrefix = "https://xivgear.app/?page=sl%7C";

        public void Export(XivGearItems items, PlayerInfo playerInfo, Configuration config)
        {
            var set = new XivGearSet
            {
                Items = items,
                Name = "Exported Set",
            };

            var sheet = new XivGearSheet
            {
                Name = "Exported Sheet",
                Description = "Exported from the XivGearExporter plugin.",
                Sets = [set],
                Job = playerInfo.Job,
                Level = playerInfo.Level,
                PartyBonus = playerInfo.PartyBonus,
                Race = playerInfo.Race,
            };

            if (config.ExportSetInEditMode)
            {
                ExportToXivGearEditMode(sheet, config.OpenUrlInBrowserAutomatically, config.PrintUrlToChat);
            }

            if (config.ExportSetInReadOnlyMode)
            {
                ExportToXivGearReadOnlyMode(sheet, config.OpenUrlInBrowserAutomatically, config.PrintUrlToChat);
            }
        }

        private async void ExportToXivGearReadOnlyMode(XivGearSheet sheet, bool openLink, bool printUrl)
        {
            try
            {
                using var client = new HttpClient();
                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(sheet);

                var stringContent = new StringContent(serialized, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(XivgearApiBase, stringContent);
                response.EnsureSuccessStatusCode();
                var setId = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(setId))
                {
                    chatGui.PrintError("got empty response from xivgear, cannot open set");
                }

                var urlToOpen = XivGearReadOnlySetPrefix + setId;
                if (openLink)
                {
                    Util.OpenLink(urlToOpen);
                }

                if(printUrl)
                {
                    chatGui.Print(urlToOpen);
                }
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or HttpRequestException)
            {
                chatGui.PrintError("Something went wrong when exporting the set:\n" + ex.Message);
            }
        }

        private void ExportToXivGearEditMode(XivGearSheet sheet, bool openLink, bool printUrl)
        {
            try
            {
                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(sheet);
                log.Info(serialized);

                var urlEncodedSheet = HttpUtility.UrlEncode(serialized);

                var urlToOpen = XivGearImportSetPrefix + urlEncodedSheet;
                if (openLink)
                {
                    Util.OpenLink(urlToOpen);
                }

                if (printUrl)
                {
                    chatGui.Print(urlToOpen);
                }
            }
            catch (Exception ex) 
            {
                throw new XivExportException(ex.Message);
            }
        }
    }
}
