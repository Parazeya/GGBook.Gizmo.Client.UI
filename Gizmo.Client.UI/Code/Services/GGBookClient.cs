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
    public sealed class GGBookClient
    {
        private readonly IHttpClientFactory _factory;
        private readonly string _baseUrl;
        private readonly string _storageUrl;
        private readonly string _userToken;
        private readonly string _clubToken;

        public bool IsConfigured    => !string.IsNullOrEmpty(_baseUrl);
        public bool Debug           { get; }
        public bool PerformanceMode { get; }

        public GGBookClient(IHttpClientFactory factory, IConfiguration config)
        {
            _factory        = factory;
            _baseUrl        = (config["GGMod:GGBookBaseUrl"]    ?? "").TrimEnd('/');
            _storageUrl     = (config["GGMod:GGBookStorageUrl"] ?? "").TrimEnd('/');
            _userToken      = config["GGMod:UserToken"] ?? "";
            _clubToken      = config["GGMod:ClubToken"] ?? "";
            Debug           = config["GGMod:Debug"]?.Equals("true",           System.StringComparison.OrdinalIgnoreCase) ?? false;
            PerformanceMode = config["GGMod:PerformanceMode"]?.Equals("true", System.StringComparison.OrdinalIgnoreCase) ?? false;
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

        public async Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct = default)
        {
            var client   = CreateClient();
            var response = await client.GetAsync(_baseUrl + path, ct);
            if (GGModConfig.Debug)
            {
                await response.Content.LoadIntoBufferAsync();
                var body = await response.Content.ReadAsStringAsync();
                GGModDebugLog.LogHttp("GET", path, null, (int)response.StatusCode, body);
            }
            return response;
        }

        public async Task<HttpResponseMessage> PostJsonAsync(string path, object payload, CancellationToken ct = default)
        {
            var client   = CreateClient();
            var reqBody  = JsonSerializer.Serialize(payload);
            var content  = new StringContent(reqBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_baseUrl + path, content, ct);
            if (GGModConfig.Debug)
            {
                await response.Content.LoadIntoBufferAsync();
                var respBody = await response.Content.ReadAsStringAsync();
                GGModDebugLog.LogHttp("POST", path, reqBody, (int)response.StatusCode, respBody);
            }
            return response;
        }

        public async Task<HttpResponseMessage> PostFormAsync(string path, Dictionary<string, string> fields, CancellationToken ct = default)
        {
            var client   = CreateClient();
            var reqBody  = string.Join("&", fields.Keys.Select(k => $"{k}={fields[k]}"));
            var content  = new FormUrlEncodedContent(fields);
            var response = await client.PostAsync(_baseUrl + path, content, ct);
            if (GGModConfig.Debug)
            {
                await response.Content.LoadIntoBufferAsync();
                var respBody = await response.Content.ReadAsStringAsync();
                GGModDebugLog.LogHttp("POST", path, reqBody, (int)response.StatusCode, respBody);
            }
            return response;
        }

        public string CasePicUrl(string? caseId, string? picture) =>
            !string.IsNullOrEmpty(picture) && !string.IsNullOrEmpty(_storageUrl) && !string.IsNullOrEmpty(caseId)
                ? $"{_storageUrl}/storage/pictures/clubCaseRoulette/{caseId}/{picture}?v=4"
                : "";

        public string RewardPicUrl(string? rewardId, string? picture, string? thumb = null) =>
            !string.IsNullOrEmpty(picture) && !string.IsNullOrEmpty(_storageUrl) && !string.IsNullOrEmpty(rewardId)
                ? $"{_storageUrl}/storage/pictures/clubCaseRewards/{rewardId}/{picture}?v=4{(thumb is not null ? "&thumb=" + thumb : "")}"
                : "";
    }
}
