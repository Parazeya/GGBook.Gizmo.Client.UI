using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Gizmo.Client.UI.View.States;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Gizmo.Client.UI
{
    public partial class SteamTopup : ComponentBase
    {
        [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] private IConfiguration Configuration { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private UserViewState UserViewState { get; set; } = default!;

        private const decimal ConversionRate = 0.06m;
        private const int MinAmount = 100;
        private const int MaxAmount = 9350;

        private static decimal? _cachedKztRate;

        private bool _modalVisible;
        private bool _helpTooltipVisible;
        private string _steamLogin = string.Empty;
        private string _amountStr = string.Empty;
        private bool _loading;
        private bool _loadingLogins;
        private string _errorMessage = string.Empty;
        private List<string> _recentLogins = new();

        private decimal Amount => decimal.TryParse(_amountStr, out var v) ? v : 0;
        private decimal ConversionFee => Math.Round(Amount * ConversionRate, 2);
        private decimal Total => Amount + ConversionFee;
        private decimal KztAmount => _cachedKztRate.HasValue ? Math.Round(Amount * _cachedKztRate.Value, 2) : 0;

        protected override void OnInitialized()
        {
            if (_cachedKztRate is null)
                _ = FetchKztRateAsync();
        }

        private HttpClient BuildGGBookClient()
        {
            var client = HttpClientFactory.CreateClient();
            var userToken = Configuration["GGMod:UserToken"];
            var clubToken = Configuration["GGMod:ClubToken"];
            if (!string.IsNullOrWhiteSpace(userToken))
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + userToken);
            if (!string.IsNullOrWhiteSpace(clubToken))
                client.DefaultRequestHeaders.Add("Club", clubToken);
            return client;
        }

        private async Task FetchKztRateAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var client = HttpClientFactory.CreateClient();
                var response = await client.GetAsync("https://open.er-api.com/v6/latest/USD", cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                        rates.TryGetProperty("RUB", out var rubEl) &&
                        rates.TryGetProperty("KZT", out var kztEl))
                    {
                        var rubPerUsd = rubEl.GetDouble();
                        var kztPerUsd = kztEl.GetDouble();
                        if (rubPerUsd > 0)
                        {
                            var rate = (decimal)(kztPerUsd / rubPerUsd);
                            if (rate > 2m && rate < 20m)
                            {
                                _cachedKztRate = rate;
                                await InvokeAsync(StateHasChanged);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private async Task LoadRecentLoginsAsync()
        {
            var baseUrl = Configuration["GGMod:GGBookBaseUrl"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseUrl)) return;

            _loadingLogins = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var client = BuildGGBookClient();
                var userId = UserViewState.Id;
                var response = await client.GetAsync(baseUrl.TrimEnd('/') + $"/steam/lastlogins?userId={userId}", cts.Token);
                if (!response.IsSuccessStatusCode) return;

                var body = await response.Content.ReadAsStringAsync(cts.Token);
                var logins = ParseLoginsResponse(body);
                if (logins.Count > 0)
                {
                    _recentLogins = logins;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch { }
            finally
            {
                _loadingLogins = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private static List<string> ParseLoginsResponse(string body)
        {
            var result = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Try array directly: ["login1", "login2"]
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in root.EnumerateArray())
                        if (el.ValueKind == JsonValueKind.String && el.GetString() is { Length: > 0 } s)
                            result.Add(s);
                    return result;
                }

                // Try object with known keys: { logins: [...] } or { data: [...] }
                foreach (var key in new[] { "logins", "data", "items", "result" })
                {
                    if (root.TryGetProperty(key, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in arr.EnumerateArray())
                            if (el.ValueKind == JsonValueKind.String && el.GetString() is { Length: > 0 } s)
                                result.Add(s);
                        if (result.Count > 0) return result;
                    }
                }
            }
            catch { }
            return result;
        }

        private void ToggleModal()
        {
            _modalVisible = !_modalVisible;
            if (_modalVisible)
            {
                _steamLogin = string.Empty;
                _amountStr = string.Empty;
                _errorMessage = string.Empty;
                _helpTooltipVisible = false;
                _recentLogins = new();
                _ = LoadRecentLoginsAsync();
            }
        }

        private void CloseModal()
        {
            _modalVisible = false;
            _errorMessage = string.Empty;
            _helpTooltipVisible = false;
        }

        private void SetAmount(int amount) => _amountStr = amount.ToString();

        private void SelectLogin(string login) => _steamLogin = login;

        private async Task OpenSteamHelpAsync()
        {
            await JSRuntime.InvokeVoidAsync("open", "https://store.steampowered.com/account");
        }

        private static string? ExtractApiError(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                    return err.GetString();
                if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    return msg.GetString();
            }
            catch { }
            return null;
        }

        private async Task TopupSteam()
        {
            if (string.IsNullOrWhiteSpace(_steamLogin) || string.IsNullOrWhiteSpace(_amountStr))
            {
                _errorMessage = "Заполните все поля";
                return;
            }

            if (!decimal.TryParse(_amountStr, out var amount) || amount <= 0)
            {
                _errorMessage = "Введите корректную сумму";
                return;
            }

            if (amount < MinAmount || amount > MaxAmount)
            {
                _errorMessage = $"Сумма должна быть от {MinAmount} до {MaxAmount} ₽";
                return;
            }

            _loading = true;
            _errorMessage = string.Empty;
            StateHasChanged();

            try
            {
                var baseUrl = Configuration["GGMod:GGBookBaseUrl"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    _errorMessage = "URL API не настроен (GGMod:GGBookBaseUrl)";
                    return;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var client = BuildGGBookClient();

                var userId = UserViewState.Id;
                var requestJson = JsonSerializer.Serialize(new { login = _steamLogin, amount = (long)amount, userId });
                using var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(baseUrl.TrimEnd('/') + "/steam/topup", requestContent, cts.Token);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string redirectUrl = string.Empty;
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("pay", out var pay) &&
                            pay.TryGetProperty("payUrl", out var payUrlEl))
                        {
                            redirectUrl = payUrlEl.GetString() ?? string.Empty;
                        }
                    }
                    catch { }

                    if (!string.IsNullOrWhiteSpace(redirectUrl))
                    {
                        await JSRuntime.InvokeVoidAsync("open", redirectUrl);
                        CloseModal();
                    }
                    else
                    {
                        _errorMessage = "Не удалось получить ссылку на оплату. Попробуйте позже.";
                    }
                }
                else
                {
                    _errorMessage = ExtractApiError(body) ?? $"Ошибка сервера ({(int)response.StatusCode})";
                }
            }
            catch (OperationCanceledException)
            {
                _errorMessage = "Превышено время ожидания ответа (5 сек)";
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
            finally
            {
                _loading = false;
                StateHasChanged();
            }
        }
    }
}
