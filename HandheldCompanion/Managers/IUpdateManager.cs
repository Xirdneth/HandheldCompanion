using System;

namespace HandheldCompanion.Managers
{
    public interface IUpdateManager
    {
        bool IsInitialized { get; set; }

        event UpdateManager.InitializedEventHandler Initialized;
        event UpdateManager.UpdatedEventHandler Updated;

        void DownloadUpdateFile(UpdateFile update);
        DateTime GetTime();
        void InstallUpdate(UpdateFile updateFile);
        void Start();
        void StartProcess();
        void Stop();
    }
}