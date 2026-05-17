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
// Touch-points in App.razor.cs: OnAfterRenderAsync (one await), OnInitialized / Dispose (event wiring).
public partial class App
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; }
    [Inject] private IConfiguration Configuration { get; set; }
    [Inject] private ILogger<App> Logger { get; set; }

    private bool _ggBookPendingFired;

    private async Task FetchGGBookConfigAsync()
    {
        var ggBook = new GGBookClient(HttpClientFactory, Configuration);
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
                steamtopup:     data.GetProperty("steamtopup").GetBoolean()
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GGBook config fetch failed — disabling all GGMod features.");
            GGModConfig.SetUnavailable();
        }
    }

    private void OnUserViewStateChanged(object? sender, EventArgs e)
    {
        if (!_ggBookPendingFired && UserViewState.Id > 0 && GGBookRegistrationContext.HasPending)
        {
            _ggBookPendingFired = true;
            _ = Task.Run(FirePendingRegistrationAsync);
        }

        // Reset flag when user logs out so the next registration cycle works
        if (UserViewState.Id == 0)
            _ggBookPendingFired = false;
    }

    private async Task FirePendingRegistrationAsync()
    {
        var adCode  = GGBookRegistrationContext.PendingAdCode;
        var refCode = GGBookRegistrationContext.PendingRefCode;
        GGBookRegistrationContext.Clear();

        var ggBook = new GGBookClient(HttpClientFactory, Configuration);
        if (!ggBook.IsConfigured) return;

        var userId = UserViewState.Id;

        try
        {
            if (adCode is not null)
                await ggBook.PostJsonAsync("/user/ad", new { userId, value = adCode });

            if (refCode is not null)
                await ggBook.PostJsonAsync("/user/ref/create", new { userId, value = refCode });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GGBook post-registration calls failed (userId={UserId}).", userId);
        }
    }
}
