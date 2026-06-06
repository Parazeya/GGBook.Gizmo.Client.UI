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

namespace Gizmo.Client.UI.Pages
{
    // ── DTOs ────────────────────────────────────────────────────────────────────

    internal sealed class TasksListResponse
    {
        [JsonPropertyName("tasks")]            public List<TaskGroupDto>      Tasks            { get; init; } = new();
        [JsonPropertyName("progress")]         public TaskProgressResponse    Progress         { get; init; } = new();
        [JsonPropertyName("unclaimedRewards")] public UnclaimedRewardsWrapper UnclaimedRewards { get; init; } = new();
        [JsonPropertyName("isVerificated")]    public bool                    IsVerificated    { get; init; }
    }

    internal sealed class TaskGroupDto
    {
        [JsonPropertyName("id")]                   public string            Id                   { get; init; } = "";
        [JsonPropertyName("name")]                 public string            Name                 { get; init; } = "";
        [JsonPropertyName("type")]                 public string            Type                 { get; init; } = "day";
        [JsonPropertyName("verificationRequired")] public bool              VerificationRequired { get; init; }
        [JsonPropertyName("list")]                 public List<TaskItemDto>? List                { get; init; }
    }

    internal sealed class TaskItemDto
    {
        [JsonPropertyName("id")]             public string           Id             { get; init; } = "";
        [JsonPropertyName("task")]           public string           Task           { get; init; } = "";
        [JsonPropertyName("taskType")]       public string           TaskType       { get; init; } = "";
        [JsonPropertyName("taskValue")]      public int              TaskValue      { get; init; }
        [JsonPropertyName("taskQuantity")]   public int              TaskQuantity   { get; init; } = 1;
        [JsonPropertyName("rewardType")]     public string           RewardType     { get; init; } = "";
        [JsonPropertyName("rewardValue")]    public string           RewardValue    { get; init; } = "";
        [JsonPropertyName("rewardDuration")] public int              RewardDuration { get; init; }
        [JsonPropertyName("rewardDetails")]  public RewardDetailsDto? RewardDetails { get; init; }
        [JsonPropertyName("taskDetails")]    public TaskDetailsDto?   TaskDetails   { get; init; }
    }

    internal sealed class RewardDetailsDto
    {
        [JsonPropertyName("name")]    public string  Name    { get; init; } = "";
        [JsonPropertyName("picture")] public string? Picture { get; init; }
    }

    internal sealed class TaskDetailsDto
    {
        [JsonPropertyName("name")]    public string  Name    { get; init; } = "";
        [JsonPropertyName("picture")] public string? Picture { get; init; }
    }

    internal sealed class TaskProgressResponse
    {
        [JsonPropertyName("result")] public List<TaskProgressGroupDto> Result { get; init; } = new();
    }

    internal sealed class TaskProgressGroupDto
    {
        [JsonPropertyName("group")] public string                 Group { get; init; } = "";
        [JsonPropertyName("items")] public List<TaskProgressItem> Items { get; init; } = new();
    }

    internal sealed class TaskProgressItem
    {
        [JsonPropertyName("taskId")]     public string TaskId     { get; init; } = "";
        [JsonPropertyName("isObtained")] public int    IsObtained { get; init; }
    }

    internal sealed class UnclaimedRewardsWrapper
    {
        [JsonPropertyName("result")] public List<UnclaimedRewardDto> Result { get; init; } = new();
    }

    internal sealed class UnclaimedRewardDto
    {
        [JsonPropertyName("id")]             public string Id             { get; init; } = "";
        [JsonPropertyName("taskId")]         public string TaskId         { get; init; } = "";
        [JsonPropertyName("rewardType")]     public string RewardType     { get; init; } = "";
        [JsonPropertyName("rewardValue")]    public string RewardValue    { get; init; } = "";
        [JsonPropertyName("rewardDuration")] public int    RewardDuration { get; init; }
    }

    // ── Domain models ────────────────────────────────────────────────────────────

    public sealed class TaskGroupModel
    {
        public string              Id                   { get; init; } = "";
        public string              Name                 { get; init; } = "";
        public string              Type                 { get; init; } = "day";
        public bool                VerificationRequired { get; init; }
        public List<TaskItemModel> Items                { get; init; } = new();
    }

    public sealed class TaskItemModel
    {
        public string  Id             { get; init; } = "";
        public string  GroupId        { get; init; } = "";
        public string  TaskName       { get; init; } = "";
        public string  TaskType       { get; init; } = "";
        public int     TaskValue      { get; init; }
        public int     TaskQuantity   { get; init; } = 1;
        public string  RewardType     { get; init; } = "";
        public string  RewardValue    { get; init; } = "";
        public int     RewardDuration { get; init; }
        public string? RewardName     { get; init; }
    }

    // ── Component ────────────────────────────────────────────────────────────────

    [ModuleGuid("B2C3D4E5-F6A7-8901-BCDE-F12345678901")]
    [PageUIModule(Title = "Задания"), ModuleDisplayOrder(4)]
    [DefaultRoute("/tasks"), Route("/tasks")]
    public partial class TasksIndex : ComponentBase
    {
        [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] private IConfiguration     Configuration    { get; set; } = default!;
        [Inject] private UserViewState      UserViewState    { get; set; } = default!;

        private GGBookClient? _ggBook;
        private GGBookClient GGBook => _ggBook ??= new GGBookClient(HttpClientFactory, Configuration);

        private string TasksCacheKey => $"tasks_{UserViewState.Id}";

        private bool    _loading;
        private string? _error;

        private List<TaskGroupModel>      _groups    = new();
        private List<TaskProgressGroupDto> _progress = new();
        private List<UnclaimedRewardDto>  _unclaimed = new();
        private bool                      _isVerificated;
        private int                       _activeTab;

        private bool    _claimingAll;
        private string? _claimAllError;
        private bool    _claimAllSuccess;
        private readonly HashSet<string>      _claimingIds  = new();
        private readonly Dictionary<string, string> _claimErrors = new();

        protected override async Task OnInitializedAsync() => await LoadAsync();

        private async Task LoadAsync()
        {
            if (!GGBook.IsConfigured) { _error = GGModL10n.Get(GGModL10n.ErrApiNotCfg); return; }

            if (GGModCache.TryGet(TasksCacheKey, out var cached))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<TasksListResponse>(cached);
                    if (data is not null)
                    {
                        _groups        = data.Tasks.Where(g => g.List?.Count > 0).Select(GroupFromDto).ToList();
                        _progress      = data.Progress.Result;
                        _unclaimed     = data.UnclaimedRewards.Result;
                        _isVerificated = data.IsVerificated;
                        _activeTab     = 0;
                        return;
                    }
                }
                catch { }
            }

            _loading         = true;
            _error           = null;
            _claimAllError   = null;
            _claimAllSuccess = false;
            _claimErrors.Clear();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp = await GGBook.GetAsync($"/tasks/list?userId={UserViewState.Id}", cts.Token);
                var body = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode) { _error = ExtractApiError(body) ?? string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), (int)resp.StatusCode); return; }
                var data = JsonSerializer.Deserialize<TasksListResponse>(body);
                if (data is null) { _error = GGModL10n.Get(GGModL10n.ErrEmptyResponse); return; }
                _groups        = data.Tasks.Where(g => g.List?.Count > 0).Select(GroupFromDto).ToList();
                _progress      = data.Progress.Result;
                _unclaimed     = data.UnclaimedRewards.Result;
                _isVerificated = data.IsVerificated;
                _activeTab     = 0;
                GGModCache.Set(TasksCacheKey, body);
            }
            catch (OperationCanceledException) { _error = GGModL10n.Get(GGModL10n.ErrTimeout); }
            catch                              { _error = GGModL10n.Get(GGModL10n.ErrNetwork); }
            finally                            { _loading = false; }
        }

        private async Task ClaimAllAsync()
        {
            if (_claimingAll) return;
            _claimingAll     = true;
            _claimAllError   = null;
            _claimAllSuccess = false;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp = await GGBook.PostJsonAsync("/tasks/rewards/claim/all", new { userId = UserViewState.Id }, cts.Token);
                var body = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode) { _claimAllError = ExtractApiError(body) ?? string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), (int)resp.StatusCode); return; }
                _claimAllSuccess = true;
                GGModCache.Invalidate(TasksCacheKey);
                // Reload from server to get accurate state
                await LoadAsync();
            }
            catch (OperationCanceledException) { _claimAllError = GGModL10n.Get(GGModL10n.ErrTimeout); }
            catch                              { _claimAllError = GGModL10n.Get(GGModL10n.ErrNetwork); }
            finally                            { _claimingAll = false; }
        }

        private async Task ClaimSingleAsync(TaskItemModel item)
        {
            if (_claimingIds.Contains(item.Id)) return;
            _claimingIds.Add(item.Id);
            _claimErrors.Remove(item.Id);
            StateHasChanged();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp = await GGBook.PostJsonAsync("/tasks/rewards/claim/single",
                    new { userId = UserViewState.Id, taskId = item.Id, clubTaskId = item.GroupId }, cts.Token);
                var body = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode)
                {
                    _claimErrors[item.Id] = ExtractApiError(body) ?? string.Format(GGModL10n.Get(GGModL10n.ErrServerCodeFmt), (int)resp.StatusCode);
                    return;
                }
                GGModCache.Invalidate(TasksCacheKey);
                // Optimistic update: mark this task as claimed in local progress
                var pg = _progress.FirstOrDefault(p => p.Group == item.GroupId);
                if (pg is not null)
                {
                    var idx = pg.Items.FindIndex(i => i.TaskId == item.Id);
                    var obtained = new TaskProgressItem { TaskId = item.Id, IsObtained = 1 };
                    if (idx >= 0) pg.Items[idx] = obtained;
                    else         pg.Items.Add(obtained);
                }
                else
                {
                    // Group not in progress yet — add it
                    _progress.Add(new TaskProgressGroupDto
                    {
                        Group = item.GroupId,
                        Items = new List<TaskProgressItem>
                        {
                            new TaskProgressItem { TaskId = item.Id, IsObtained = 1 }
                        }
                    });
                }
                StateHasChanged();
            }
            catch (OperationCanceledException) { _claimErrors[item.Id] = GGModL10n.Get(GGModL10n.ErrTimeout); }
            catch                              { _claimErrors[item.Id] = GGModL10n.Get(GGModL10n.ErrNetwork); }
            finally
            {
                _claimingIds.Remove(item.Id);
                StateHasChanged();
            }
        }

        // ── Mapping ──────────────────────────────────────────────────────────────

        private static TaskGroupModel GroupFromDto(TaskGroupDto dto) => new()
        {
            Id                   = dto.Id,
            Name                 = dto.Name,
            Type                 = dto.Type,
            VerificationRequired = dto.VerificationRequired,
            Items                = dto.List?.Select(t => ItemFromDto(t, dto.Id)).ToList() ?? new(),
        };

        private static TaskItemModel ItemFromDto(TaskItemDto dto, string groupId) => new()
        {
            Id             = dto.Id,
            GroupId        = groupId,
            TaskName       = dto.TaskDetails?.Name ?? string.Empty,
            TaskType       = dto.TaskType,
            TaskValue      = dto.TaskValue,
            TaskQuantity   = dto.TaskQuantity,
            RewardType     = dto.RewardType,
            RewardValue    = dto.RewardValue,
            RewardDuration = dto.RewardDuration,
            RewardName     = dto.RewardDetails?.Name,
        };

        // ── Progress helpers ──────────────────────────────────────────────────────

        private bool IsClaimable(string groupId, string taskId)
        {
            var pg = _progress.FirstOrDefault(p => p.Group == groupId);
            var pr = pg?.Items.FirstOrDefault(i => i.TaskId == taskId);
            return pr is not null && pr.IsObtained == 0;
        }

        private bool IsDone(string groupId, string taskId)
        {
            var pg = _progress.FirstOrDefault(p => p.Group == groupId);
            var pr = pg?.Items.FirstOrDefault(i => i.TaskId == taskId);
            return pr is not null && pr.IsObtained == 1;
        }

        // ── Display helpers ───────────────────────────────────────────────────────

        private string GroupTabLabel(TaskGroupModel g) => g.Type switch
        {
            "day"   => GGModL10n.Get(GGModL10n.TabDaily),
            "week"  => GGModL10n.Get(GGModL10n.TabWeekly),
            "month" => GGModL10n.Get(GGModL10n.TabMonthly),
            _       => g.Name,
        };

        private string TaskTitle(TaskItemModel t) => t.TaskType switch
        {
            "play"             => string.Format(GGModL10n.Get(GGModL10n.TaskTypePlay),        t.TaskValue),
            "deposit"          => string.Format(GGModL10n.Get(GGModL10n.TaskTypeDeposit),     t.TaskValue),
            "spend"            => string.Format(GGModL10n.Get(GGModL10n.TaskTypeSpend),       t.TaskValue),
            "buy"              => !string.IsNullOrWhiteSpace(t.TaskName)
                               ? $"{GGModL10n.Get(GGModL10n.TaskTypeBuy)}: {t.TaskName}"
                               : GGModL10n.Get(GGModL10n.TaskTypeBuy),
            "points"           => string.Format(GGModL10n.Get(GGModL10n.TaskTypePoints),      t.TaskValue),
            "fixedtimespent"   => string.Format(GGModL10n.Get(GGModL10n.TaskTypeFixedTime),   t.TaskValue),
            "producttimespent" => string.Format(GGModL10n.Get(GGModL10n.TaskTypeProductTime), t.TaskValue),
            "totaltimespent"   => string.Format(GGModL10n.Get(GGModL10n.TaskTypeTotalTime),   t.TaskValue),
            _                  => !string.IsNullOrWhiteSpace(t.TaskName) ? t.TaskName : GGModL10n.Get(GGModL10n.TaskTypeDefault),
        };

        private string RewardTitle(TaskItemModel t) => t.RewardType switch
        {
            "points"    => string.Format(GGModL10n.Get(GGModL10n.RwdTaskPointsFmt), t.RewardValue),
            "time"      => string.Format(GGModL10n.Get(GGModL10n.RwdTaskTimeFmt),   t.RewardValue),
            "usergroup" => t.RewardDuration > 0
                           ? string.Format(GGModL10n.Get(GGModL10n.RwdTaskVipDurFmt), t.RewardDuration)
                           : GGModL10n.Get(GGModL10n.RwdTaskVip),
            "case"      => $"{GGModL10n.Get(GGModL10n.RwdTaskCase)} {t.RewardName ?? t.RewardValue}".TrimEnd(),
            "product"   => $"{GGModL10n.Get(GGModL10n.RwdTaskProduct)} {t.RewardName ?? ""}".TrimEnd(),
            _           => t.RewardType,
        };

        private static string TaskColor(string t) => t switch
        {
            "play"             => "#60a5fa",
            "deposit"          => "#34c38f",
            "spend"            => "#34c38f",
            "buy"              => "#f0c040",
            "points"           => "#f0c040",
            "fixedtimespent"   => "#a78bfa",
            "producttimespent" => "#a78bfa",
            "totaltimespent"   => "#a78bfa",
            _                  => "#4ecdc4",
        };

        private static string RewardColor(string t) => t switch
        {
            "points"    => "#f0c040",
            "time"      => "#a78bfa",
            "case"      => "#4ecdc4",
            "usergroup" => "#f472b6",
            "product"   => "#60a5fa",
            _           => "#aaa",
        };

        private static Icons TaskIcon(string t) => t switch
        {
            "play"             => Icons.Gamepad_Client,   // fa-gamepad
            "deposit"          => Icons.Deposit_Client,   // fa-wallet
            "spend"            => Icons.Coins_Client,     // fa-coins
            "buy"              => Icons.ShoppingCart_Client, // fa-bag-shopping
            "points"           => Icons.Trophy_Client,    // fa-trophy
            "fixedtimespent"   => Icons.Clock_Client,     // fa-clock
            "producttimespent" => Icons.Clock_Client,
            "totaltimespent"   => Icons.Clock_Client,
            _                  => Icons.Star_Client,
        };

        private static Icons RewardIcon(string t) => t switch
        {
            "points"    => Icons.Trophy_Client,    // fa-trophy
            "time"      => Icons.Clock_Client,     // fa-clock
            "case"      => Icons.Star2_Client,     // fa-box-open → closest is star (special prize)
            "usergroup" => Icons.User_Client,      // fa-user-crown
            "product"   => Icons.Star_Client,      // fa-gift
            _           => Icons.Star_Client,
        };

        private string RewardTitleFromUnclaimed(UnclaimedRewardDto r) => r.RewardType switch
        {
            "points"    => string.Format(GGModL10n.Get(GGModL10n.RwdTaskPointsFmt), r.RewardValue),
            "time"      => string.Format(GGModL10n.Get(GGModL10n.RwdTaskTimeFmt),   r.RewardValue),
            "usergroup" => r.RewardDuration > 0
                           ? string.Format(GGModL10n.Get(GGModL10n.RwdTaskVipDurFmt), r.RewardDuration)
                           : GGModL10n.Get(GGModL10n.RwdTaskVip),
            "case"      => GGModL10n.Get(GGModL10n.RwdTaskCase),
            "product"   => GGModL10n.Get(GGModL10n.RwdTaskProduct),
            _           => r.RewardType,
        };

        private static string? ExtractApiError(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var key in new[] { "error", "message" })
                    if (doc.RootElement.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                        return TranslateError(el.GetString());
            }
            catch { }
            return null;
        }

        private static string TranslateError(string? key) => key switch
        {
            "error.invalid_input_data"     => GGModL10n.Get(GGModL10n.ErrInvalidInput),
            "error.invalid_input"          => GGModL10n.Get(GGModL10n.ErrInvalidInput),
            "error.no_unclaimed_rewards"   => GGModL10n.Get(GGModL10n.ErrNoUnclaimedRewards),
            "error.reward_already_claimed" => GGModL10n.Get(GGModL10n.ErrRewardAlreadyClaimed),
            "error.reward_in_progress"     => GGModL10n.Get(GGModL10n.ErrRewardInProgress),
            _                              => key ?? GGModL10n.Get(GGModL10n.ErrUnknown),
        };
    }
}
