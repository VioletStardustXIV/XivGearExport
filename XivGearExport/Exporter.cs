using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Serilog;

namespace XivGearExport
{

    internal class Exporter
    {
        private static readonly string XivgearApiBase = "https://api.xivgear.app/shortlink/";
        private static readonly string XivGearImportSetPrefix = "https://xivgear.app/?page=importset%7C";
        private static readonly string XivGearReadOnlySetPrefix = "https://xivgear.app/?page=sl%7C";

        private readonly HttpClient httpClient;
        private readonly IPluginLog log;
        private readonly IChatGui chatGui;

        public Exporter(HttpClient httpClient, IPluginLog log, IChatGui chatGui)
        {
            this.httpClient = httpClient;
            this.log = log;
            this.chatGui = chatGui;
        }

        public void Export(XivGearItems items, PlayerInfo playerInfo, Configuration config)
        {
            XivGearSet set = new XivGearSet
            {
                items = items,
                name = "Exported Set",
            };

            XivGearSheet sheet = new XivGearSheet
            {
                name = "Exported Sheet",
                description = "Exported from the XivGearExporter plugin.",
                sets = [set],
                job = playerInfo.job,
                level = 100,
                partyBonus = 5,
                race = playerInfo.race,
            };

            if(config != null)
            {
                if (config.ExportSetInEditMode)
                {
                    ExportToXivGearEditMode(sheet, config.OpenURLInBrowserAutomatically, config.PrintURLToChat);
                }

                if (config.ExportSetInReadOnlyMode)
                {
                    ExportToXivGearReadOnlyMode(sheet, config.OpenURLInBrowserAutomatically, config.PrintURLToChat);
                }
            }
        }

        public async void ExportToXivGearReadOnlyMode(XivGearSheet sheet, bool openLink, bool printUrl)
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
            catch (HttpRequestException ex)
            {
                throw new XivExportException(ex.Message);
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException || ex is InvalidOperationException)
            {
                throw new XivExportException(ex.Message);
            }
        }

        public void ExportToXivGearEditMode(XivGearSheet sheet, bool openLink, bool printUrl)
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
