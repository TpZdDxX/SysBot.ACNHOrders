namespace SysBot.Base;

public interface IConsoleConnection
{
    string Name { get; }

    string Label { get; set; }

    bool Connected { get; }

    int MaximumTransferSize { get; set; }

    int BaseDelay { get; set; }

    int DelayFactor { get; set; }

    void Connect();

    void Reset();

    void Disconnect();

    void Log(string message);

    void LogInfo(string message);

    void LogError(string message);
}
