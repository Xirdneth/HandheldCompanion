using HandheldCompanion.Controls;
using System.Collections.Generic;
using System.Diagnostics;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface IProcessManager
    {
        ProcessEx Empty { get; }
        bool IsInitialized { get; set; }

        event ProcessManager.ForegroundChangedEventHandler ForegroundChanged;
        event ProcessManager.InitializedEventHandler Initialized;
        event ProcessManager.ProcessStartedEventHandler ProcessStarted;
        event ProcessManager.ProcessStoppedEventHandler ProcessStopped;

        bool CheckXInput(Process process);
        ProcessEx GetEmptyProcessEx();
        ProcessEx GetForegroundProcess();
        ProcessEx GetLastSuspendedProcess();
        ProcessThread GetMainThread(Process process);
        ProcessEx GetProcess(int processId);
        List<ProcessEx> GetProcesses();
        List<ProcessEx> GetProcesses(string executable);
        bool HasProcess(int pId);
        void ResumeProcess(ProcessEx processEx);
        void Start();
        void Stop();
        void SuspendProcess(ProcessEx processEx);
    }
}