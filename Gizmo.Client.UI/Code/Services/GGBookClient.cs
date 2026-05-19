using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Gizmo.Client.UI.Services
{
    /// <summary>
    /// Helper for GGBook API calls. Construct directly — not registered in DI.
    /// </summary>
    public sealed class GGBookClient
    {
        private readonly IHttpClientFactory _factory;
        private readonly string _baseUrl;
        private readonly string _storageUrl;
        private readonly string _userToken;
        private readonly string _clubToken;

        public bool IsConfigured => !string.IsNullOrEmpty(_baseUrl);
        public bool Debug        { get; }

        public GGBookClient(IHttpClientFactory factory, IConfiguration config)
        {
            _factory    = factory;
            _baseUrl    = (config["GGMod:GGBookBaseUrl"]    ?? "").TrimEnd('/');
            _storageUrl = (config["GGMod:GGBookStorageUrl"] ?? "").TrimEnd('/');
            _userToken  = config["GGMod:UserToken"] ?? "";
            _clubToken  = config["GGMod:ClubToken"] ?? "";
            Debug       = config["GGMod:Debug"]?.Equals("true", System.StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private HttpClient CreateClient()
        {
            var client = _factory.CreateClient();
            if (!string.IsNullOrWhiteSpace(_userToken))
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Basic " + _userToken);
            if (!string.IsNullOrWhiteSpace(_clubToken))
                client.DefaultRequestHeaders.TryAddWithoutValidation("Club", _clubToken);
            return client;
        }

        private void LogRequestHeaders(HttpClient client, string method, string path)
        {
            if (!GGModConfig.Debug) return;
            var auth = client.DefaultRequestHeaders.Contains("Authorization") ? "present" : "missing";
            var club = client.DefaultRequestHeaders.Contains("Club")          ? "present" : "missing";
            GGModDebugLog.Info($"  {method} {_baseUrl}{path} | Auth={auth} Club={club}");
        }

        public Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct = default)
            => CreateClient().GetAsync(_baseUrl + path, ct);

        public Task<HttpResponseMessage> PostJsonAsync(string path, object payload, CancellationToken ct = default)
        {
            var client  = CreateClient();
            LogRequestHeaders(client, "POST (json)", path);
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            return client.PostAsync(_baseUrl + path, content, ct);
        }

        public Task<HttpResponseMessage> PostFormAsync(string path, Dictionary<string, string> fields, CancellationToken ct = default)
        {
            var client  = CreateClient();
            LogRequestHeaders(client, "POST (form)", path);
            var content = new FormUrlEncodedContent(fields);
            return client.PostAsync(_baseUrl + path, content, ct);
        }

        public string CasePicUrl(string? caseId, string? picture) =>
            !string.IsNullOrEmpty(picture) && !string.IsNullOrEmpty(_storageUrl) && !string.IsNullOrEmpty(caseId)
                ? $"{_storageUrl}/storage/pictures/clubCaseRoulette/{caseId}/{picture}"
                : "";

        public string RewardPicUrl(string? rewardId, string? picture, string? thumb = null) =>
            !string.IsNullOrEmpty(picture) && !string.IsNullOrEmpty(_storageUrl) && !string.IsNullOrEmpty(rewardId)
                ? $"{_storageUrl}/storage/pictures/clubCaseRewards/{rewardId}/{picture}{(thumb is not null ? "?thumb=" + thumb : "")}"
                : "";
    }
}
