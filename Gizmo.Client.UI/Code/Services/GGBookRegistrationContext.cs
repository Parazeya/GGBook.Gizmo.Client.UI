namespace Gizmo.Client.UI.Services
{
    /// <summary>
    /// Static store for GGBook ad/referral data collected during registration.
    /// Persists until the user logs in, at which point the data is sent and cleared.
    /// </summary>
    public static class GGBookRegistrationContext
    {
        public static string? PendingAdCode    { get; private set; }
        public static string? PendingRefCode   { get; private set; }
        public static bool    HasPending       => PendingAdCode is not null || PendingRefCode is not null;

        public static void Set(string? adCode, string? refCode)
        {
            PendingAdCode  = string.IsNullOrEmpty(adCode)  ? null : adCode;
            PendingRefCode = string.IsNullOrEmpty(refCode) ? null : refCode;
        }

        public static void Clear()
        {
            PendingAdCode  = null;
            PendingRefCode = null;
        }
    }
}
