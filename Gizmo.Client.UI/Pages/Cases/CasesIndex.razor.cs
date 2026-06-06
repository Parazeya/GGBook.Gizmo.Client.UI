using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Gizmo.Client.UI.Services;
using Gizmo.Client.UI.View.States;
using Gizmo.UI;
using Gizmo.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Gizmo.Client.UI.Pages
{
    // ── API response DTOs ────────────────────────────────────────────────────────

    internal sealed class CasesListResponse
    {
        [JsonPropertyName("cases")] public List<CaseApiDto> Cases { get; init; } = new();
    }

    internal sealed class CaseApiDto
    {
        [JsonPropertyName("id")]               public string  Id               { get; init; } = "";
        [JsonPropertyName("name")]             public string  Name             { get; init; } = "";
        [JsonPropertyName("pointsPrice")]      public int     PointsPrice      { get; init; }
        [JsonPropertyName("paymentType")]      public int     PaymentType      { get; init; }
        [JsonPropertyName("availableKeys")]    public int     AvailableKeys    { get; init; }
        [JsonPropertyName("picture")]          public string? Picture          { get; init; }
        [JsonPropertyName("showRewardChance")] public bool    ShowRewardChance { get; init; }
        [JsonPropertyName("showRewardName")]   public bool    ShowRewardName   { get; init; }
        [JsonPropertyName("sort")]             public string? Sort             { get; init; }
    }

    internal sealed class CaseDetailResponse
    {
        [JsonPropertyName("case")]               public CaseApiDto?           Case               { get; init; }
        [JsonPropertyName("rewards")]            public List<RewardApiDto>    Rewards            { get; init; } = new();
        [JsonPropertyName("availableKeys")]      public int                   AvailableKeys      { get; init; }
        [JsonPropertyName("availableToOpen")]    public bool                  AvailableToOpen    { get; init; }
        [JsonPropertyName("keysAvailableToGet")] public KeysAvailableToGetDto? KeysAvailableToGet { get; init; }
    }

    internal sealed class KeysAvailableToGetDto
    {
        [JsonPropertyName("tasks")]     public bool Tasks     { get; init; }
        [JsonPropertyName("referrals")] public bool Referrals { get; init; }
    }

    internal sealed class RewardApiDto
    {
        [JsonPropertyName("id")]          public string  Id          { get; init; } = "";
        [JsonPropertyName("name")]        public string  Name        { get; init; } = "";
        [JsonPropertyName("chance")]      public double  Chance      { get; init; }
        [JsonPropertyName("rewardType")]  public string  RewardType  { get; init; } = "";
        [JsonPropertyName("value")]       public double? Value       { get; init; }
        [JsonPropertyName("enableStock")] public bool?   EnableStock { get; init; }
        [JsonPropertyName("stockAmount")] public double? StockAmount { get; init; }
        [JsonPropertyName("picture")]     public string? Picture     { get; init; }
        [JsonPropertyName("color")]       public string? Color       { get; init; }
        [JsonPropertyName("description")]  public string? Description  { get; init; }
        [JsonPropertyName("customValue")]  public string? CustomValue  { get; init; }
        [JsonPropertyName("position")]     public int     Position     { get; init; }
    }

    internal sealed class BuyKeyResponse
    {
        [JsonPropertyName("availableKeys")] public int AvailableKeys { get; init; }
    }

    internal sealed class UseKeyResponse
    {
        [JsonPropertyName("availableKeys")] public int           AvailableKeys { get; init; }
        [JsonPropertyName("reward")]        public RewardApiDto? Reward        { get; init; }
    }

    internal sealed class HistoryPageResponse
    {
        [JsonPropertyName("result")]     public List<HistoryItemDto> Result     { get; init; } = new();
        [JsonPropertyName("totalPages")] public int                  TotalPages { get; init; }
    }

    internal sealed class HistoryItemDto
    {
        [JsonPropertyName("created")] public string           Created { get; init; } = "";
        [JsonPropertyName("expand")]  public HistoryExpandDto? Expand { get; init; }
    }

    internal sealed class HistoryExpandDto
    {
        [JsonPropertyName("case")]   public HistoryCaseDto?   Case   { get; init; }
        [JsonPropertyName("reward")] public HistoryRewardDto? Reward { get; init; }
    }

    internal sealed class HistoryCaseDto
    {
        [JsonPropertyName("id")]      public string  Id      { get; init; } = "";
        [JsonPropertyName("name")]    public string  Name    { get; init; } = "";
        [JsonPropertyName("picture")] public string? Picture { get; init; }
    }

    internal sealed class HistoryRewardDto
    {
        [JsonPropertyName("id")]         public string  Id         { get; init; } = "";
        [JsonPropertyName("name")]       public string  Name       { get; init; } = "";
        [JsonPropertyName("picture")]    public string? Picture    { get; init; }
        [JsonPropertyName("rewardType")] public string  RewardType { get; init; } = "";
        [JsonPropertyName("color")]      public string? Color      { get; init; }
    }

    // ── Domain models ────────────────────────────────────────────────────────────

    public sealed class CaseModel
    {
        public string  Id               { get; init; } = "";
        public string  Name             { get; init; } = "";
        public string? PictureUrl       { get; init; }
        public int     PointsPrice      { get; init; }
        public int     PaymentType      { get; set;  }
        public int     AvailableKeys    { get; set;  }
        public bool    AvailableToOpen  { get; set;  } = true;
        public bool    ShowRewardChance { get; set;  }
        public bool    ShowRewardName   { get; set;  }
        public string  Sort             { get; set;  } = "";
        public bool    CanGetViaTask    { get; set;  }
        public bool    CanGetViaReferral{ get; set;  }
        public List<CaseReward> Rewards { get; set;  } = new();
    }

    public sealed class CaseReward
    {
        public string  Id          { get; init; } = "";
        public string  Name        { get; init; } = "";
        public string  Color       { get; init; } = "#3F8CFF";
        public double  Chance      { get; init; }
        public int     Position    { get; init; }
        public string? PictureUrl  { get; init; }
        public string  RewardType  { get; init; } = "gift";
        public string? Description { get; init; }
        public string? CustomValue { get; init; }
        public bool?   EnableStock { get; init; }
        public double? StockAmount { get; set;  }
    }

    public sealed class CaseHistoryEntry
    {
        public string   CaseName         { get; init; } = "";
        public string?  CasePictureUrl   { get; init; }
        public string   RewardName       { get; init; } = "";
        public string?  RewardPictureUrl { get; init; }
        public string   RewardType       { get; init; } = "gift";
        public string   RewardColor      { get; init; } = "#3F8CFF";
        public DateTime Date             { get; init; }
    }

    // ── Component ────────────────────────────────────────────────────────────────

    [ModuleGuid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
    [PageUIModule(Title = "Кейсы"), ModuleDisplayOrder(3)]
    [Route("/cases")]
    public partial class CasesIndex : ComponentBase
    {
        [Inject] private IJSRuntime         JSRuntime          { get; set; } = default!;
        [Inject] private IHttpClientFactory HttpClientFactory  { get; set; } = default!;
        [Inject] private IConfiguration     Configuration      { get; set; } = default!;
        [Inject] private UserViewState      UserViewState      { get; set; } = default!;

        private GGBookClient? _ggBook;
        private GGBookClient GGBook => _ggBook ??= new GGBookClient(HttpClientFactory, Configuration);

        private string CaseListCacheKey    => $"cases_list_{UserViewState.Id}";
        private string CaseHistCacheKey    => $"cases_hist_{UserViewState.Id}";

        private const int SpinDurationMs    = 7500;
        private const int ItemWidthPx       = 90;
        private const int ItemGapPx         = 3;
        private const int SlotPx            = ItemWidthPx + ItemGapPx; // 93
        private const int WheelSize         = 100;
        private const int WinnerTargetIndex = 70;
        private const int HistoryPageSize   = 20;

        private List<CaseModel> _cases       = new();
        private bool            _casesLoading;
        private string?         _casesError;

        private CaseModel? _selectedCase;
        private bool       _detailLoading;
        private string?    _detailError;

        private bool             _isSpinning;
        private CaseReward?      _wonReward;
        private bool             _showRewardOverlay;
        private List<CaseReward> _wheelItems         = new();
        private string           _rouletteTransform  = "translateX(0px)";
        private string           _rouletteTransition = "none";
        private readonly Random  _rng                = new();
        private int              _availableKeys;
        private string?          _spinError;

        private bool    _showBuyPanel;
        private int     _buyQty = 1;
        private bool    _buyLoading;
        private string? _buyError;

        private CaseReward? _rewardInfo;
        private bool        _showRewardInfo;

        private readonly List<CaseHistoryEntry> _history = new();
        private bool _historyLoading;
        private int  _historyPage;
        private int  _historyTotalPages;
        private bool HistoryHasMore => _historyPage < _historyTotalPages;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            await Task.WhenAll(LoadCasesAsync(), LoadHistoryFirstPageAsync());
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_wheelItems.Count > 0)
                await FreezeRouletteGifsAsync();
        }

        private async Task FreezeRouletteGifsAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", @"(function() {
                    // Remove stale JS-injected canvases from previous Blazor renders.
                    // Blazor reuses DOM nodes via incremental diffing, so canvas elements
                    // inserted by JS persist across re-renders and overlay new content.
                    document.querySelectorAll('.gg-roulette-item').forEach(function(item) {
                        item.querySelectorAll('canvas').forEach(function(c) { c.remove(); });
                        var img = item.querySelector('.gg-roulette-item-img');
                        if (img) { img.style.display = ''; img.removeAttribute('data-gif-frozen'); }
                    });
                    document.querySelectorAll('.gg-roulette-item-img').forEach(function(img) {
                        function freeze() {
                            if (!img.complete || img.naturalWidth === 0) return;
                            var canvas = document.createElement('canvas');
                            canvas.width = img.naturalWidth;
                            canvas.height = img.naturalHeight;
                            canvas.className = img.className;
                            canvas.style.position = 'absolute';
                            canvas.style.inset = '0';
                            canvas.style.width = '100%';
                            canvas.style.height = '100%';
                            canvas.style.objectFit = 'cover';
                            try {
                                canvas.getContext('2d').drawImage(img, 0, 0, canvas.width, canvas.height);
                                img.parentNode.insertBefore(canvas, img);
                                img.style.display = 'none';
                            } catch(e) {}
                        }
                        if (img.complete && img.naturalWidth > 0) { freeze(); }
                        else { img.addEventListener('load', freeze, { once: true }); }
                    });
                })()");
            }
            catch { }
        }

        // ── DTO mapping ──────────────────────────────────────────────────────────

        private CaseModel CaseFromDto(CaseApiDto dto) => new()
        {
            Id            = dto.Id,
            Name          = dto.Name,
            PointsPrice   = dto.PointsPrice,
            PaymentType   = dto.PaymentType,
            AvailableKeys = dto.AvailableKeys,
            PictureUrl    = GGBook.CasePicUrl(dto.Id, dto.Picture),
        };

        private CaseReward RewardFromDto(RewardApiDto dto) => new()
        {
            Id          = dto.Id,
            Name        = dto.Name,
            Chance      = dto.Chance,
            Position    = dto.Position,
            RewardType  = dto.RewardType,
            Color       = ColorFromType(dto.RewardType, dto.Color),
            PictureUrl  = GGBook.RewardPicUrl(dto.Id, dto.Picture),
            Description = dto.Description,
            CustomValue = dto.CustomValue,
            EnableStock = dto.EnableStock,
            StockAmount = dto.StockAmount,
        };

        // ── Cases list ───────────────────────────────────────────────────────────

        private async Task LoadCasesAsync()
        {
            if (!GGBook.IsConfigured) { _casesError = GGModL10n.Get(GGModL10n.ErrApiNotCfg); return; }

            if (GGModCache.TryGet(CaseListCacheKey, out var cached))
            {
                var data = JsonSerializer.Deserialize<CasesListResponse>(cached);
                _cases = data?.Cases.Select(CaseFromDto).ToList() ?? new();
                return;
            }

            _casesLoading = true;
            _casesError   = null;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp      = await GGBook.GetAsync($"/cases/list?userId={UserViewState.Id}", cts.Token);
                var body      = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode) { _casesError = ExtractApiError(body) ?? string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), (int)resp.StatusCode); return; }
                var data      = JsonSerializer.Deserialize<CasesListResponse>(body);
                _cases        = data?.Cases.Select(CaseFromDto).ToList() ?? new();
                GGModCache.Set(CaseListCacheKey, body);
            }
            catch (OperationCanceledException) { _casesError = GGModL10n.Get(GGModL10n.ErrTimeout); }
            catch                              { _casesError = GGModL10n.Get(GGModL10n.ErrNetwork); }
            finally                            { _casesLoading = false; }
        }

        // ── History ──────────────────────────────────────────────────────────────

        private async Task LoadHistoryFirstPageAsync()
        {
            _history.Clear();
            _historyPage       = 0;
            _historyTotalPages = 0;

            if (GGBook.IsConfigured && GGModCache.TryGet(CaseHistCacheKey, out var cached))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<HistoryPageResponse>(cached);
                    if (data is not null)
                    {
                        _historyPage       = 1;
                        _historyTotalPages = data.TotalPages;
                        foreach (var item in data.Result)
                        {
                            var c = item.Expand?.Case;
                            var r = item.Expand?.Reward;
                            _history.Add(new CaseHistoryEntry
                            {
                                CaseName         = c?.Name ?? "—",
                                CasePictureUrl   = GGBook.CasePicUrl(c?.Id, c?.Picture),
                                RewardName       = r?.Name ?? "—",
                                RewardPictureUrl = GGBook.RewardPicUrl(r?.Id, r?.Picture, "52x52"),
                                RewardType       = r?.RewardType ?? "gift",
                                RewardColor      = ColorFromType(r?.RewardType, r?.Color),
                                Date             = ParsePbDate(item.Created),
                            });
                        }
                    }
                }
                catch { }
                return;
            }

            await LoadHistoryNextPageAsync();
        }

        private async Task LoadHistoryNextPageAsync()
        {
            if (!GGBook.IsConfigured || _historyLoading) return;
            _historyLoading = true;
            _historyPage++;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp  = await GGBook.GetAsync(
                    $"/caseopening/user/logs?itemsPerPage={HistoryPageSize}&pageNumber={_historyPage}&userId={UserViewState.Id}",
                    cts.Token);
                if (!resp.IsSuccessStatusCode) { _historyPage--; return; }
                var body  = await resp.Content.ReadAsStringAsync(cts.Token);
                var data  = JsonSerializer.Deserialize<HistoryPageResponse>(body);
                if (data is not null)
                {
                    _historyTotalPages = data.TotalPages;
                    foreach (var item in data.Result)
                    {
                        var c = item.Expand?.Case;
                        var r = item.Expand?.Reward;
                        _history.Add(new CaseHistoryEntry
                        {
                            CaseName         = c?.Name ?? "—",
                            CasePictureUrl   = GGBook.CasePicUrl(c?.Id, c?.Picture),
                            RewardName       = r?.Name ?? "—",
                            RewardPictureUrl = GGBook.RewardPicUrl(r?.Id, r?.Picture, "52x52"),
                            RewardType       = r?.RewardType ?? "gift",
                            RewardColor      = ColorFromType(r?.RewardType, r?.Color),
                            Date             = ParsePbDate(item.Created),
                        });
                    }
                    if (_historyPage == 1)
                        GGModCache.Set(CaseHistCacheKey, body);
                }
            }
            catch { _historyPage--; }
            finally { _historyLoading = false; await InvokeAsync(StateHasChanged); }
        }

        // ── Case detail ──────────────────────────────────────────────────────────

        private async Task SelectCaseAsync(CaseModel c)
        {
            _selectedCase       = c;
            _wonReward          = null;
            _showRewardOverlay  = false;
            _isSpinning         = false;
            _rouletteTransform  = "translateX(0px)";
            _rouletteTransition = "none";
            _showBuyPanel       = false;
            _buyQty             = 1;
            _spinError          = null;
            _buyError           = null;
            _detailError        = null;
            _wheelItems         = new();
            await LoadCaseDetailAsync(c.Id);
        }

        private async Task LoadCaseDetailAsync(string caseId)
        {
            _detailLoading = true;
            _detailError   = null;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp  = await GGBook.GetAsync($"/cases/{caseId}/rewards?userId={UserViewState.Id}", cts.Token);
                var body  = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode) { _detailError = ExtractApiError(body) ?? string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), (int)resp.StatusCode); return; }
                var data  = JsonSerializer.Deserialize<CaseDetailResponse>(body);
                if (data is null) { _detailError = GGModL10n.Get(GGModL10n.ErrEmptyResponse); return; }
                if (_selectedCase is not null)
                {
                    var caseDto = data.Case;
                    _selectedCase.ShowRewardChance  = caseDto?.ShowRewardChance ?? false;
                    _selectedCase.ShowRewardName    = caseDto?.ShowRewardName   ?? false;
                    _selectedCase.Sort              = caseDto?.Sort             ?? "";
                    _selectedCase.PaymentType       = caseDto?.PaymentType      ?? 0;
                    _selectedCase.AvailableToOpen   = data.AvailableToOpen;
                    _selectedCase.CanGetViaTask     = data.KeysAvailableToGet?.Tasks     ?? false;
                    _selectedCase.CanGetViaReferral = data.KeysAvailableToGet?.Referrals ?? false;
                    _selectedCase.Rewards           = SortRewards(
                        data.Rewards.Select(RewardFromDto).ToList(), _selectedCase.Sort);
                    _availableKeys                  = data.AvailableKeys;
                }
                _wheelItems = BuildWheel(_selectedCase?.Rewards ?? new());
            }
            catch (OperationCanceledException) { _detailError = GGModL10n.Get(GGModL10n.ErrTimeout); }
            catch                              { _detailError = GGModL10n.Get(GGModL10n.ErrNetwork); }
            finally                            { _detailLoading = false; }
        }

        private void Back()
        {
            _selectedCase      = null;
            _wonReward         = null;
            _showRewardOverlay = false;
            _showBuyPanel      = false;
            _spinError         = null;
            _buyError          = null;
        }

        private async Task OpenCaseFromHistoryAsync(CaseHistoryEntry h)
        {
            var c = _cases.FirstOrDefault(x => x.Name == h.CaseName);
            if (c is not null) await SelectCaseAsync(c);
        }

        private void CloseOverlay()
        {
            _showRewardOverlay  = false;
            _wonReward          = null;
            _rouletteTransform  = "translateX(0px)";
            _rouletteTransition = "none";
            if (_selectedCase is not null)
                _wheelItems = BuildWheel(_selectedCase.Rewards);
        }

        private void OpenRewardInfo(CaseReward r) { _rewardInfo = r; _showRewardInfo = true; }
        private void CloseRewardInfo()             { _showRewardInfo = false; _rewardInfo = null; }

        // ── Roulette ─────────────────────────────────────────────────────────────

        private List<CaseReward> BuildWheel(List<CaseReward> rewards)
        {
            if (!rewards.Any()) return new();
            double total = rewards.Sum(r => r.Chance);
            var wheel    = new List<CaseReward>(WheelSize);
            for (int i = 0; i < WheelSize; i++)
            {
                double pick = _rng.NextDouble() * total;
                double cum  = 0;
                var chosen  = rewards.Last();
                foreach (var r in rewards) { cum += r.Chance; if (pick < cum) { chosen = r; break; } }
                wheel.Add(chosen);
            }
            return wheel;
        }

        private async Task SpinAsync()
        {
            if (_isSpinning || _selectedCase is null || _availableKeys <= 0 || !_selectedCase.AvailableToOpen) return;
            _isSpinning        = true;
            _showRewardOverlay = false;
            _spinError         = null;

            UseKeyResponse? apiResult = null;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var resp      = await GGBook.PostJsonAsync(
                    $"/cases/{_selectedCase.Id}/key/use",
                    new { userId = UserViewState.Id },
                    cts.Token);
                var respBody  = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode) { _spinError = ExtractApiError(respBody) ?? string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), (int)resp.StatusCode); _isSpinning = false; return; }
                apiResult = JsonSerializer.Deserialize<UseKeyResponse>(respBody);
            }
            catch (OperationCanceledException) { _spinError = GGModL10n.Get(GGModL10n.ErrTimeout); _isSpinning = false; return; }
            catch                              { _spinError = GGModL10n.Get(GGModL10n.ErrNetwork);             _isSpinning = false; return; }

            if (apiResult?.Reward is null) { _spinError = GGModL10n.Get(GGModL10n.ErrNoRewardData); _isSpinning = false; return; }

            _availableKeys = apiResult.AvailableKeys;
            var winner     = RewardFromDto(apiResult.Reward);

            // Decrement stock locally for the won reward, then recheck availability
            var wonInList = _selectedCase.Rewards.FirstOrDefault(r => r.Id == winner.Id);
            if (wonInList?.EnableStock == true)
            {
                wonInList.StockAmount = (wonInList.StockAmount ?? 1) - 1;
                _selectedCase.AvailableToOpen = !HasExhaustedLimitedReward(_selectedCase.Rewards);
            }

            _wheelItems = BuildWheel(_selectedCase.Rewards);
            int targetIdx = WinnerTargetIndex + _rng.Next(-3, 4);
            if (targetIdx >= 0 && targetIdx < _wheelItems.Count)
                _wheelItems[targetIdx] = winner;

            _rouletteTransition = "none";
            _rouletteTransform  = "translateX(0px)";
            StateHasChanged();
            await Task.Delay(60);

            // Measure actual rendered dimensions — CSS uses rem units which vary by breakpoint
            double vpWidth  = 400;
            double itemW    = ItemWidthPx;
            double slotW    = SlotPx;
            double padLeft  = 0;
            try
            {
                var json = await JSRuntime.InvokeAsync<string>("eval", @"JSON.stringify((() => {
                    const vp = document.getElementById('giz-roulette-vp');
                    if (!vp) return { vpW:400, itemW:90, slotW:93, padL:0 };
                    const vpW   = vp.getBoundingClientRect().width;
                    const items = vp.querySelectorAll('.gg-roulette-item');
                    if (items.length < 2) return { vpW, itemW:90, slotW:93, padL:0 };
                    const r0 = items[0].getBoundingClientRect();
                    const r1 = items[1].getBoundingClientRect();
                    return { vpW, itemW: r0.width, slotW: r1.left - r0.left, padL: r0.left - vp.getBoundingClientRect().left };
                })())");
                var dims = JsonSerializer.Deserialize<JsonElement>(json);
                vpWidth = dims.GetProperty("vpW").GetDouble();
                itemW   = dims.GetProperty("itemW").GetDouble();
                slotW   = dims.GetProperty("slotW").GetDouble();
                padLeft = dims.GetProperty("padL").GetDouble();
            }
            catch { }

            double targetPx = -(targetIdx * slotW + padLeft + itemW / 2 - vpWidth / 2) + _rng.Next(-10, 11);
            _rouletteTransition = $"transform {SpinDurationMs}ms cubic-bezier(0.02, 0.05, 0.01, 1)";
            _rouletteTransform  = $"translateX({targetPx:F0}px)";
            StateHasChanged();

            await Task.Delay(SpinDurationMs + 300);

            GGModCache.Invalidate(CaseHistCacheKey);

            _history.Insert(0, new CaseHistoryEntry
            {
                CaseName         = _selectedCase.Name,
                CasePictureUrl   = _selectedCase.PictureUrl,
                RewardName       = winner.Name,
                RewardColor      = winner.Color,
                RewardType       = winner.RewardType,
                RewardPictureUrl = winner.PictureUrl,
                Date             = DateTime.Now,
            });

            _wonReward         = winner;
            _showRewardOverlay = true;
            _isSpinning        = false;
            StateHasChanged();
        }

        // ── Buy keys ─────────────────────────────────────────────────────────────

        private async Task BuyKeysAsync()
        {
            if (_selectedCase is null || _buyLoading) return;
            _buyLoading = true;
            _buyError   = null;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp      = await GGBook.PostJsonAsync(
                    $"/cases/{_selectedCase.Id}/key/buy",
                    new { userId = UserViewState.Id, quantity = _buyQty },
                    cts.Token);
                var respBody  = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode) { _buyError = ExtractApiError(respBody) ?? string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), (int)resp.StatusCode); return; }
                var data      = JsonSerializer.Deserialize<BuyKeyResponse>(respBody);
                if (data is not null) _availableKeys = data.AvailableKeys;
                _showBuyPanel = false;
                _buyQty       = 1;
            }
            catch (OperationCanceledException) { _buyError = GGModL10n.Get(GGModL10n.ErrTimeout); }
            catch                              { _buyError = GGModL10n.Get(GGModL10n.ErrNetwork); }
            finally                            { _buyLoading = false; }
        }

        // ── Static helpers ───────────────────────────────────────────────────────

        private static bool IsRewardOutOfStock(CaseReward r) =>
            r.EnableStock == true && (r.StockAmount ?? 1) <= 0;

        private static bool HasExhaustedLimitedReward(List<CaseReward> rewards) =>
            rewards.Any(IsRewardOutOfStock);

        private static List<CaseReward> SortRewards(List<CaseReward> rewards, string sort) => sort switch
        {
            "chance_asc" => rewards.OrderBy(r => r.Chance).ToList(),
            "manual"     => rewards.OrderBy(r => r.Position).ToList(),
            _            => rewards.OrderByDescending(r => r.Chance).ToList(),
        };

        private static string ColorFromType(string? type, string? apiColor = null)
        {
            if (!string.IsNullOrEmpty(apiColor)) return apiColor;
            return type switch
            {
                "points"      => "#F0C040",
                "deposit"     => "#4ecdc4",
                "time"        => "#a78bfa",
                "producttime" => "#60a5fa",
                "usergroup"   => "#f472b6",
                "drink"       => "#4ecdc4",
                _             => "#6b7a8d",
            };
        }

        private static string? ExtractApiError(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                foreach (var key in new[] { "error", "message" })
                    if (root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                        return TranslateError(el.GetString());
            }
            catch { }
            return null;
        }

        private static string TranslateError(string? key) => key switch
        {
            "error.not_enough_points"        => GGModL10n.Get(GGModL10n.ErrNotEnoughPoints),
            "error.case_unavailable_keys"    => GGModL10n.Get(GGModL10n.ErrCaseNoKeys),
            "error.case_stock_exhausted"     => GGModL10n.Get(GGModL10n.ErrCaseStockExhausted),
            "error.case_unavailable_to_open" => GGModL10n.Get(GGModL10n.ErrCaseUnavailable),
            "error.case_open_in_progress"    => GGModL10n.Get(GGModL10n.ErrCaseOpenInProgress),
            "error.case_unavailable_to_buy"  => GGModL10n.Get(GGModL10n.ErrCaseBuyUnavailable),
            "error.cannot_redeem_points"     => GGModL10n.Get(GGModL10n.ErrCannotRedeemPts),
            "error.case_not_found"           => GGModL10n.Get(GGModL10n.ErrCaseNotFound),
            "error.invalid_input_data"       => GGModL10n.Get(GGModL10n.ErrInvalidInput),
            "error.undefined"                => GGModL10n.Get(GGModL10n.ErrCaseInternal),
            "error.case_user_group_restricted" => GGModL10n.Get(GGModL10n.ErrCaseUserGroupRestricted),
            "error.case_misconfigured"       => GGModL10n.Get(GGModL10n.ErrCaseMisconfigured),
            "error.case_no_reward_selected"  => GGModL10n.Get(GGModL10n.ErrCaseNoRewardSelected),
            "error.case_user_not_found"      => GGModL10n.Get(GGModL10n.ErrCaseUserNotFound),
            "error.case_no_rewards"          => GGModL10n.Get(GGModL10n.ErrCaseNoRewards),
            _                                => key ?? GGModL10n.Get(GGModL10n.ErrUnknown),
        };

        private static string KeysText(int n) => GGModL10n.KeysText(n);

        private static string GetRewardTypeName(string? type) => type switch
        {
            "time"        => GGModL10n.Get(GGModL10n.RwdTypeTime),
            "producttime" => GGModL10n.Get(GGModL10n.RwdTypeProductTime),
            "deposit"     => GGModL10n.Get(GGModL10n.RwdTypeDeposit),
            "usergroup"   => GGModL10n.Get(GGModL10n.RwdTypeUserGroup),
            "drink"       => GGModL10n.Get(GGModL10n.RwdTypeDrink),
            "points"      => GGModL10n.Get(GGModL10n.RwdTypePoints),
            "custom"      => GGModL10n.Get(GGModL10n.RwdTypeGift),
            _             => GGModL10n.Get(GGModL10n.RwdTypeGift),
        };

        private static Icons GetRewardIcon(string? type) => type switch
        {
            "points"      => Icons.Trophy_Client,    // fa-trophy
            "time"        => Icons.Clock_Client,     // fa-clock
            "producttime" => Icons.Schedule_Client,  // fa-calendar-clock
            "deposit"     => Icons.Deposit_Client,   // fa-money-check-alt
            "usergroup"   => Icons.User_Client,      // fa-user-crown
            "drink"       => Icons.Drink,
            "custom"      => Icons.Star_Client,
            _             => Icons.Star_Client,      // fa-gift (case/product/default)
        };

        private static DateTime ParsePbDate(string s)
        {
            // PocketBase sends "2026-05-15 17:15:58.159Z" — space instead of T
            if (DateTime.TryParse(s.Replace(' ', 'T'), null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToLocalTime();
            return DateTime.Now;
        }

        private static string FormatDate(DateTime dt)
        {
            var now = DateTime.Now;
            if (dt.Date == now.Date)             return dt.ToString("HH:mm");
            if (dt.Date == now.Date.AddDays(-1)) return $"{GGModL10n.Get(GGModL10n.Yesterday)} {dt:HH:mm}";
            return dt.ToString("dd.MM HH:mm");
        }
    }
}
