using System.Collections.Generic;
using System.Linq;
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

        private static async System.Threading.Tasks.Task LogResponseAsync(HttpResponseMessage response, string path)
        {
            if (!GGModConfig.Debug) return;
            await response.Content.LoadIntoBufferAsync();
            var body = await response.Content.ReadAsStringAsync();
            var snippet = body.Length > 300 ? body[..300] + "…" : body;
            var level = response.IsSuccessStatusCode ? GGModLogLevel.Ok : GGModLogLevel.Error;
            GGModDebugLog.Log($"  ← {(int)response.StatusCode} {path} | {snippet}", level);
        }

        public async Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct = default)
        {
            var client = CreateClient();
            GGModDebugLog.Info($"  GET {_baseUrl}{path}");
            var response = await client.GetAsync(_baseUrl + path, ct);
            await LogResponseAsync(response, path);
            return response;
        }

        public async Task<HttpResponseMessage> PostJsonAsync(string path, object payload, CancellationToken ct = default)
        {
            var client  = CreateClient();
            var body    = JsonSerializer.Serialize(payload);
            GGModDebugLog.Info($"  POST {_baseUrl}{path} ← {body}");
            var content  = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_baseUrl + path, content, ct);
            await LogResponseAsync(response, path);
            return response;
        }

        public async Task<HttpResponseMessage> PostFormAsync(string path, Dictionary<string, string> fields, CancellationToken ct = default)
        {
            var client  = CreateClient();
            var bodyStr = string.Join("&", fields.Keys.Select(k => $"{k}={fields[k]}"));
            GGModDebugLog.Info($"  POST (form) {_baseUrl}{path} ← {bodyStr}");
            var content  = new FormUrlEncodedContent(fields);
            var response = await client.PostAsync(_baseUrl + path, content, ct);
            await LogResponseAsync(response, path);
            return response;
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
