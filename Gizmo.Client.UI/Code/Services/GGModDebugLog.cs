using System;
using System.Collections.Generic;

namespace Gizmo.Client.UI.Services
{
    public static class GGModDebugLog
    {
        private const int MaxEntries = 300;

        public static readonly List<GGModLogEntry> Entries = new();

        public static event EventHandler? Updated;

        public static void Log(string message, GGModLogLevel level = GGModLogLevel.Info)
        {
            if (!GGModConfig.Debug) return;
            Entries.Add(new GGModLogEntry(DateTime.Now, level, message));
            if (Entries.Count > MaxEntries) Entries.RemoveAt(0);
            Updated?.Invoke(null, EventArgs.Empty);
        }

        public static void LogHttp(string method, string path, string? requestBody, int responseStatus, string? responseBody)
        {
            if (!GGModConfig.Debug) return;
            Entries.Add(new GGModLogEntry(DateTime.Now, method, path, requestBody, responseStatus, responseBody));
            if (Entries.Count > MaxEntries) Entries.RemoveAt(0);
            Updated?.Invoke(null, EventArgs.Empty);
        }

        public static void Info(string message)  => Log(message, GGModLogLevel.Info);
        public static void Ok(string message)    => Log(message, GGModLogLevel.Ok);
        public static void Warn(string message)  => Log(message, GGModLogLevel.Warn);
        public static void Error(string message) => Log(message, GGModLogLevel.Error);

        public static void Clear()
        {
            Entries.Clear();
            Updated?.Invoke(null, EventArgs.Empty);
        }

        public static bool PanelVisible { get; private set; }

        public static void TogglePanel()
        {
            if (!GGModConfig.Debug) return;
            PanelVisible = !PanelVisible;
            Updated?.Invoke(null, EventArgs.Empty);
        }
    }

    public sealed class GGModLogEntry
    {
        public GGModLogEntry(DateTime time, GGModLogLevel level, string message)
        {
            Time    = time;
            Level   = level;
            Message = message;
        }

        public GGModLogEntry(DateTime time, string method, string path, string? requestBody, int responseStatus, string? responseBody)
        {
            Time           = time;
            Level          = responseStatus >= 400 || responseStatus <= 0 ? GGModLogLevel.Error : GGModLogLevel.Ok;
            Message        = $"{method} {path}";
            RequestMethod  = method;
            RequestPath    = path;
            RequestBody    = requestBody;
            ResponseStatus = responseStatus;
            ResponseBody   = responseBody;
        }

        public DateTime      Time           { get; }
        public GGModLogLevel Level          { get; }
        public string        Message        { get; }

        public string? RequestMethod  { get; }
        public string? RequestPath    { get; }
        public string? RequestBody    { get; }
        public int?    ResponseStatus { get; }
        public string? ResponseBody   { get; }

        public bool IsHttpEntry => RequestPath != null;

        public string LevelLabel => Level switch
        {
            GGModLogLevel.Ok    => "OK",
            GGModLogLevel.Warn  => "WARN",
            GGModLogLevel.Error => "ERR",
            _                   => "INFO",
        };

        public string LevelColor => Level switch
        {
            GGModLogLevel.Ok    => "#4ecdc4",
            GGModLogLevel.Warn  => "#f7b731",
            GGModLogLevel.Error => "#ff4443",
            _                   => "#aab0bc",
        };
    }

    public enum GGModLogLevel { Info, Ok, Warn, Error }
}
