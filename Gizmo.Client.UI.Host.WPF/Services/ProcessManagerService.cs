using Gizmo.Client.UI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Gizmo.Client.UI.Host.WPF.Services
{
    public sealed class ProcessManagerService : IProcessManagerService
    {
        private static readonly HashSet<string> _systemNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "lsass", "winlogon", "csrss", "wininit", "services", "smss",
            "dwm", "conhost", "taskmgr", "System", "Idle", "Registry",
            "MsMpEng", "NisSrv", "SecurityHealthSystray", "SecurityHealthService",
            "SearchHost", "SearchIndexer", "ctfmon", "spoolsv", "fontdrvhost",
            "RuntimeBroker", "ShellExperienceHost", "StartMenuExperienceHost",
            "TextInputHost", "SgrmBroker", "WUDFHost", "WmiPrvSE",
        };

        private static readonly string _winDir =
            Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        public IReadOnlyList<ManagedProcessInfo> GetProcesses()
        {
            var result = new List<ManagedProcessInfo>();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.MainWindowHandle == IntPtr.Zero) continue;
                    if (string.IsNullOrWhiteSpace(p.MainWindowTitle)) continue;
                    if (_systemNames.Contains(p.ProcessName)) continue;
                    if (IsSystemPath(p)) continue;

                    result.Add(new ManagedProcessInfo(
                        p.Id,
                        p.ProcessName,
                        p.MainWindowTitle,
                        p.WorkingSet64));
                }
                catch { /* access denied or process exited */ }
            }
            return result.OrderBy(x => x.Name).ToList();
        }

        public void KillProcess(int pid)
        {
            try { Process.GetProcessById(pid).Kill(entireProcessTree: true); }
            catch { }
        }

        private static bool IsSystemPath(Process p)
        {
            try
            {
                var path = p.MainModule?.FileName ?? string.Empty;
                return path.StartsWith(_winDir, StringComparison.OrdinalIgnoreCase);
            }
            catch { return true; }
        }
    }
}
