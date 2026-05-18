using System;

namespace Gizmo.Client.UI.Services
{
    /// <summary>
    /// Runtime feature flags fetched from GGBook /client/config on startup.
    /// All flags default to false until Apply() or SetUnavailable() is called.
    /// </summary>
    public static class GGModConfig
    {
        public static bool IsResolved      { get; private set; }
        public static bool ReferralSystem  { get; private set; }
        public static bool Ads             { get; private set; }
        public static bool Cases           { get; private set; }
        public static bool Tasks           { get; private set; }
        public static bool SteamTopup      { get; private set; }
        public static bool Promocodes      { get; private set; }

        // Fires once when Apply() or SetUnavailable() completes — subscribers re-render nav/pages.
        public static event EventHandler? ConfigResolved;

        public static void Apply(bool referralSystem, bool ads, bool cases, bool tasks, bool steamtopup, bool promocodes)
        {
            ReferralSystem = referralSystem;
            Ads            = ads;
            Cases          = cases;
            Tasks          = tasks;
            SteamTopup     = steamtopup;
            Promocodes     = promocodes;
            IsResolved     = true;
            ConfigResolved?.Invoke(null, EventArgs.Empty);
        }

        public static void SetUnavailable()
        {
            IsResolved = true;
            // All feature flags stay false
            ConfigResolved?.Invoke(null, EventArgs.Empty);
        }
    }
}
