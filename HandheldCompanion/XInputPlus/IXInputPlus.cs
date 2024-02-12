using System.Diagnostics;

namespace HandheldCompanion
{
    public interface IXInputPlus
    {
        void ExtractXInputPlusLibraries();
        void InjectXInputPlus(Process targetProcess, bool x64bit);
        bool Is64bitProcess(Process process);
        bool RegisterApplication(Profile profile);
        bool UnregisterApplication(Profile profile);
        bool WriteXInputPlusINI(string directoryPath, bool x64bit);
    }
}