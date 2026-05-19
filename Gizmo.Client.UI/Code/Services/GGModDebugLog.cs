using System;
using System.Collections.Generic;

namespace Gizmo.Client.UI.Services
{
    public static class GGModDebugLog
    {
        private const int MaxEntries = 200;

        public static readonly List<GGModLogEntry> Entries = new();

        public static event EventHandler? Updated;

        public static void Log(string message, GGModLogLevel level = GGModLogLevel.Info)
        {
            if (!GGModConfig.Debug) return;
            Entries.Add(new GGModLogEntry(DateTime.Now, level, message));
            if (Entries.Count > MaxEntries)
                Entries.RemoveAt(0);
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

        public DateTime     Time    { get; }
        public GGModLogLevel Level   { get; }
        public string        Message { get; }

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
