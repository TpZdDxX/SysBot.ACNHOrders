using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace SysBot.Base;

public static class LogUtil
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public static readonly List<Action<string, string>> Forwarders = new List<Action<string, string>>();

    public static DateTime LastLogged { get; private set; } = DateTime.Now;

    static LogUtil()
    {
        LoggingConfiguration loggingConfiguration = new LoggingConfiguration();
        Directory.CreateDirectory("logs");
        FileTarget target = new FileTarget("logfile")
        {
            FileName = Path.Combine("logs", "SysBotLog.txt"),
            ArchiveEvery = FileArchivePeriod.Day,
            // Archives as SysBotLog.yyyy-MM-dd.txt
            ArchiveSuffixFormat = ".{1:yyyy-MM-dd}",
            ArchiveAboveSize = 104857600L,
            MaxArchiveFiles = 14,
            Encoding = Encoding.Unicode,
            WriteBom = true
        };
        loggingConfiguration.AddRule(LogLevel.Debug, LogLevel.Fatal, target);
        LogManager.Configuration = loggingConfiguration;
    }

    public static void LogText(string message)
    {
        Logger.Log(LogLevel.Info, message);
    }

    public static void LogError(string message, string identity)
    {
        Logger.Log(LogLevel.Error, identity + " " + message);
        Log(message, identity);
    }

    public static void LogInfo(string message, string identity)
    {
        Logger.Log(LogLevel.Info, identity + " " + message);
        Log(message, identity);
    }

    private static void Log(string message, string identity)
    {
        foreach (Action<string, string> forwarder in Forwarders)
        {
            try
            {
                forwarder(message, identity);
            }
            catch (Exception value)
            {
                Logger.Log(LogLevel.Error, "Failed to forward log from " + identity + " - " + message);
                Logger.Log(LogLevel.Error, value);
            }
        }
        LastLogged = DateTime.Now;
    }

    public static void LogSafe(Exception exception, string identity)
    {
        Logger.Log(LogLevel.Error, "Exception from " + identity + ":");
        Logger.Log(LogLevel.Error, exception);
        for (Exception? innerException = exception.InnerException; innerException != null; innerException = innerException.InnerException)
        {
            Logger.Log(LogLevel.Error, innerException);
        }
    }
}
