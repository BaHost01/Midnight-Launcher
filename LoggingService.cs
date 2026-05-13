using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Midnight_Launcher;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

public static class LoggingService
{
    private static readonly string LogFolder = "logs";
    private static readonly string LogFile = Path.Combine(LogFolder, $"launcher_{DateTime.Now:yyyy-MM-dd}.log");
    private static readonly object _lock = new object();

    static LoggingService()
    {
        try
        {
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);

            // Initialize log with system info
            var header = new StringBuilder();
            header.AppendLine("====================================================================");
            header.AppendLine($"Midnight Launcher Log - {DateTime.Now}");
            header.AppendLine($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
            header.AppendLine($".NET Version: {RuntimeInformation.FrameworkDescription}");
            header.AppendLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            header.AppendLine("====================================================================");
            
            WriteRaw(header.ToString());
        }
        catch { }
    }

    public static void Debug(string message) => Log(LogLevel.Debug, message);
    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warn(string message) => Log(LogLevel.Warn, message);
    public static void Error(string message, Exception? ex = null) 
    {
        var fullMessage = ex != null ? $"{message} | Exception: {ex.Message}\nStacktrace: {ex.StackTrace}" : message;
        Log(LogLevel.Error, fullMessage);
    }
    public static void Fatal(string message, Exception? ex = null) => Log(LogLevel.Fatal, message);

    private static void Log(LogLevel level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{level.ToString().ToUpper().PadRight(5)}] {message}";

        lock (_lock)
        {
            try
            {
                File.AppendAllText(LogFile, logEntry + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
            catch { }
        }
    }

    private static void WriteRaw(string content)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(LogFile, content + Environment.NewLine);
            }
            catch { }
        }
    }
}
