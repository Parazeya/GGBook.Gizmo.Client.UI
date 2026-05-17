using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Gizmo.Client.UI.Services;
using Gizmo.Client.UI.View.Services;
using Gizmo.Client.UI.View.States;
using Gizmo.UI.Services;
using Gizmo.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gizmo.Client.UI.Pages
{
    [Route(ClientRoutes.RegistrationBasicFieldsRoute)]
    public partial class RegistrationBasicFields : CustomDOMComponentBase
    {
        #region INJECT

        [Inject] ILocalizationService LocalizationService { get; set; }
        [Inject] UserRegistrationViewState UserRegistrationViewState { get; set; }
        [Inject] UserRegistrationConfirmationMethodViewService UserRegistrationConfirmationMethodService { get; set; }
        [Inject] UserRegistrationBasicFieldsViewService UserRegistrationBasicFieldsViewService { get; set; }
        [Inject] UserRegistrationBasicFieldsViewState ViewState { get; set; }
        [Inject] IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] IConfiguration Configuration { get; set; } = default!;
        [Inject] ILogger<RegistrationBasicFields> Logger { get; set; } = default!;

        #endregion

        #region GGBOOK STATE

        private GGBookClient? _ggBook;
        private GGBookClient GGBook => _ggBook ??= new GGBookClient(HttpClientFactory, Configuration);

        private List<(string Code, string Name)> _adOptions = new();
        private string? _selectedAdCode;
        private string _refCode = string.Empty;
        private bool _refChecking;
        private bool? _refValid;

        #endregion

        #region LIFECYCLE

        protected override async Task OnInitializedAsync()
        {
            this.SubscribeChange(ViewState);
            await LoadAdsAsync();
            await base.OnInitializedAsync();
        }

        public override void Dispose()
        {
            this.UnsubscribeChange(ViewState);
            base.Dispose();
        }

        #endregion

        #region HANDLERS

        public void OnCloseButtonClickHandler()
        {
            UserRegistrationBasicFieldsViewService.Reset();
        }

        public async Task CheckRefCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(_refCode)) return;

            _refChecking = true;
            _refValid = null;
            StateHasChanged();

            try
            {
                if (!GGBook.IsConfigured) { _refValid = false; return; }

                var resp = await GGBook.PostJsonAsync("/user/ref/code/check", new { code = _refCode });
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("valid", out var valid))
                        _refValid = valid.GetBoolean();
                    else
                        _refValid = false;
                }
                else
                {
                    _refValid = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ref code check failed.");
                _refValid = false;
            }
            finally
            {
                _refChecking = false;
                StateHasChanged();
            }
        }

        public async Task HandleSubmitAsync()
        {
            // Store pending GGBook data before navigation happens inside SubmitAsync
            GGBookRegistrationContext.Set(
                _selectedAdCode,
                (_refValid == true && !string.IsNullOrWhiteSpace(_refCode)) ? _refCode : null
            );

            await UserRegistrationBasicFieldsViewService.SubmitAsync();
        }

        #endregion

        #region PRIVATE

        private async Task LoadAdsAsync()
        {
            if (!GGModConfig.Ads || !GGBook.IsConfigured) return;
            try
            {
                var resp = await GGBook.GetAsync("/ads/list");
                if (!resp.IsSuccessStatusCode) return;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    return;

                foreach (var item in data.EnumerateArray())
                {
                    var code = item.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(code))
                        _adOptions.Add((code, name));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load ads list from GGBook.");
            }
        }

        #endregion
    }
}
