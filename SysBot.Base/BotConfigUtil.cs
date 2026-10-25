using System.Net;

namespace SysBot.Base;

public static class BotConfigUtil
{
    public static T GetConfig<T>(string ip, int port) where T : IWirelessConnectionConfig, new()
    {
        T result = new T();
        string iP = IPAddress.Parse(ip).ToString();
        result.IP = iP;
        result.Port = port;
        return result;
    }
}
