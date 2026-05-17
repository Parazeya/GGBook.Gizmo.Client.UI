using System.Collections.Generic;

namespace Gizmo.Client.UI.Services
{
    public interface IProcessManagerService
    {
        IReadOnlyList<ManagedProcessInfo> GetProcesses();
        void KillProcess(int pid);
    }

    public sealed record ManagedProcessInfo(
        int Pid,
        string Name,
        string Title,
        long MemoryBytes);
}
