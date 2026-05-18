using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Gizmo.Client.UI.Services;
using Gizmo.Client.UI.View.States;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace Gizmo.Client.UI
{
    public partial class PromoCode : ComponentBase
    {
        [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] private IConfiguration Configuration { get; set; } = default!;
        [Inject] private UserViewState UserViewState { get; set; } = default!;

        private bool _modalVisible;
        private string _code = string.Empty;
        private bool _loading;
        private string _errorMessage = string.Empty;
        private string _successMessage = string.Empty;

        private void ToggleModal()
        {
            _modalVisible = !_modalVisible;
            if (_modalVisible)
            {
                _code = string.Empty;
                _errorMessage = string.Empty;
                _successMessage = string.Empty;
            }
        }

        private void CloseModal()
        {
            _modalVisible = false;
            _errorMessage = string.Empty;
            _successMessage = string.Empty;
        }

        private async Task UsePromocodeAsync()
        {
            if (string.IsNullOrWhiteSpace(_code))
            {
                _errorMessage = GGModL10n.Get(GGModL10n.ErrPromoEmpty);
                return;
            }

            _loading = true;
            _errorMessage = string.Empty;
            _successMessage = string.Empty;
            StateHasChanged();

            try
            {
                var ggBook = new GGBookClient(HttpClientFactory, Configuration);
                if (!ggBook.IsConfigured)
                {
                    _errorMessage = GGModL10n.Get(GGModL10n.ErrApiNotCfg);
                    return;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await ggBook.PostJsonAsync(
                    "/user/promocode",
                    new { name = _code.Trim(), userId = UserViewState.Id },
                    cts.Token);

                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    _successMessage = FormatSuccess(body);
                else
                    _errorMessage = ExtractError(body);
            }
            catch (OperationCanceledException)
            {
                _errorMessage = GGModL10n.Get(GGModL10n.ErrTimeout);
            }
            catch
            {
                _errorMessage = GGModL10n.Get(GGModL10n.ErrNetwork);
            }
            finally
            {
                _loading = false;
                StateHasChanged();
            }
        }

        private static string FormatSuccess(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                var value = root.TryGetProperty("value", out var v) ? v.ToString() : "";

                return type switch
                {
                    "time"        => string.Format(GGModL10n.Get(GGModL10n.PromoSuccessTime),    value),
                    "points"      => string.Format(GGModL10n.Get(GGModL10n.PromoSuccessPoints),  value),
                    "case"        => string.Format(GGModL10n.Get(GGModL10n.PromoSuccessCase),    value),
                    "productTime" => string.Format(GGModL10n.Get(GGModL10n.PromoSuccessProduct), value),
                    "userGroup"   => string.Format(GGModL10n.Get(GGModL10n.PromoSuccessGroup),   value),
                    _             => GGModL10n.Get(GGModL10n.PromoSuccessDefault),
                };
            }
            catch
            {
                return "Промокод успешно применён";
            }
        }

        private static string ExtractError(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var code = root.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";

                return code switch
                {
                    "error.invalid_input"            => GGModL10n.Get(GGModL10n.ErrPromoInvalidInput),
                    "error.user_not_found"           => GGModL10n.Get(GGModL10n.ErrPromoUserNotFound),
                    "error.promocode_not_found"      => GGModL10n.Get(GGModL10n.ErrPromoNotFound),
                    "error.promocode_already_used"   => GGModL10n.Get(GGModL10n.ErrPromoAlreadyUsed),
                    "error.promocode_event_used"     => GGModL10n.Get(GGModL10n.ErrPromoEventUsed),
                    "referral.verification_required" => GGModL10n.Get(GGModL10n.ErrPromoVerifyRequired),
                    "error.user_info_fetching"       => GGModL10n.Get(GGModL10n.ErrPromoUserInfo),
                    "error.undefined"                => GGModL10n.Get(GGModL10n.ErrPromoUndefined),
                    _ => string.IsNullOrEmpty(code) ? GGModL10n.Get(GGModL10n.ErrUnknown) : string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), code),
                };
            }
            catch
            {
                return "Ошибка сервера";
            }
        }
    }
}
