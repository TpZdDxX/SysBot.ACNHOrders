using System.Net.Sockets;

namespace SysBot.Base;

public abstract class SwitchSocket : IConsoleConnection
{
    protected readonly IWirelessConnectionConfig Info;

    private readonly ProtocolType Protocol;

    private readonly SocketType Type;

    protected Socket Connection { get; private set; }

    public string Name { get; }

    public string Label { get; set; }

    public bool Connected { get; protected set; }

    public int MaximumTransferSize { get; set; } = 448;

    public int BaseDelay { get; set; } = 64;

    public int DelayFactor { get; set; } = 256;

    protected SwitchSocket(IWirelessConnectionConfig wi, SocketType type = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
    {
        Type = type;
        Protocol = protocol;
        Connection = new Socket(type, protocol);
        Info = wi;
        Name = (Label = wi.IP);
    }

    public void Log(string message)
    {
        LogInfo(message);
    }

    public void LogInfo(string message)
    {
        LogUtil.LogInfo(message, Label);
    }

    public void LogError(string message)
    {
        LogUtil.LogError(message, Label);
    }

    public abstract void Connect();

    public abstract void Reset();

    public abstract void Disconnect();

    public void InitializeSocket()
    {
        Connection = new Socket(Type, Protocol);
    }
}
