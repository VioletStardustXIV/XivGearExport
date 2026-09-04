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
        private const string XivGearImportSetPrefix = "https://xivgear.app/#/nore/importset/";
        private const string XivGearReadOnlySetPrefix = "https://xivgear.app/sl/";

        public void Export(XivGearItems items, PlayerInfo playerInfo, Configuration config, string setName = "Exported Set")
        {
            var set = new XivGearSet
            {
                Items = items,
                Name = setName,
            };

            var sheet = new XivGearSheet
            {
                Name = setName,
                Description = "Exported from the XivGearExporter Dalamud plugin.",
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

        // EncodeSpeechMarks encodes speech marks, making them work with `Util.OpenLink`
        private static string EncodeSpeechMarks( string stringToUpdate)
        {
            return stringToUpdate.Replace("\"", "%22");
        }

        private void ExportToXivGearEditMode(XivGearSheet sheet, bool openLink, bool printUrl)
        {
            try
            {
                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(sheet);
                log.Info(serialized);

                // Note: technically this does not actually produce a compliant URL. In particular, it will leave
                // e.g. `{` unencoded. However, this makes the URLs significantly more readable than the alternative
                // i.e. using Uri.EscapeDataString(serialized);
                // This does seem to work for every real browser that I tried, so I don't really see the harm in this.
                // That being said, I think a better approach here is using a base64 endpoint, but that would need to
                // be implemented first. I'll get back to this when there is such an endpoint.
                var urlToOpen = XivGearImportSetPrefix + EncodeSpeechMarks(HttpUtility.UrlPathEncode(serialized));
                
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
