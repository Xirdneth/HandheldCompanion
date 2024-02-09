using System.Diagnostics;
using Serilog;

namespace HandheldCompanion.Managers;

public static class LogManager
{
    public static void LogInformation(string message, params object[] args)
    {
        Trace.TraceInformation(message, args);
        Log.Logger.Information(message, args);
    }

    public static void LogWarning(string message, params object[] args)
    {
        Trace.TraceWarning(message, args);
        Log.Logger.Warning(message, args);
    }

    public static void LogCritical(string message, params object[] args)
    {
        Trace.TraceError(message, args);
        Log.Logger.Fatal(message, args);
    }

    public static void LogDebug(string message, params object[] args)
    {
        Trace.TraceInformation(message, args);
        Log.Logger.Debug(message, args);
    }

    public static void LogError(string message, params object[] args)
    {
        Trace.TraceError(message, args);
        Log.Logger.Error(message, args);
    }

    public static void LogTrace(string message, params object[] args)
    {
        Trace.TraceInformation(message, args);
        Log.Logger.Verbose(message, args);
    }
}