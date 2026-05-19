using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Gizmo.Client.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gizmo.Client.UI;

// GGMod — isolated from upstream App.razor.cs so upstream merges stay clean.
public partial class App
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; }
    [Inject] private IConfiguration Configuration { get; set; }
    [Inject] private ILogger<App> Logger { get; set; }

    private async Task FetchGGBookConfigAsync()
    {
        var ggBook = new GGBookClient(HttpClientFactory, Configuration);
        GGModConfig.SetDebug(ggBook.Debug);
        GGModDebugLog.Info("FetchGGBookConfigAsync: start");

        if (!ggBook.IsConfigured)
        {
            GGModConfig.SetUnavailable();
            return;
        }

        try
        {
            var response = await ggBook.GetAsync("/client/config");
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("GGBook /client/config returned {Status}.", (int)response.StatusCode);
                GGModDebugLog.Warn($"FetchGGBookConfigAsync: /client/config returned {(int)response.StatusCode}");
                GGModConfig.SetUnavailable();
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");

            GGModConfig.Apply(
                referralSystem: data.GetProperty("referralSystem").GetBoolean(),
                ads:            data.GetProperty("ads").GetBoolean(),
                cases:          data.GetProperty("cases").GetBoolean(),
                tasks:          data.GetProperty("tasks").GetBoolean(),
                steamtopup:     data.GetProperty("steamtopup").GetBoolean(),
                promocodes:     data.TryGetProperty("promocodes", out var promoEl) && promoEl.GetBoolean()
            );

            GGModDebugLog.Ok("FetchGGBookConfigAsync: config applied");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GGBook config fetch failed — disabling all GGMod features.");
            GGModDebugLog.Error($"FetchGGBookConfigAsync: exception — {ex.Message}");
            GGModConfig.SetUnavailable();
        }
    }
}
