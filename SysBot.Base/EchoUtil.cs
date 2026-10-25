using System;
using System.Collections.Generic;

namespace SysBot.Base;

public static class EchoUtil
{
    public static readonly List<Action<string>> Forwarders = new List<Action<string>>();

    public static void Echo(string message)
    {
        foreach (Action<string> forwarder in Forwarders)
        {
            try
            {
                forwarder(message);
            }
            catch (Exception ex)
            {
                LogUtil.LogInfo($"Exception: {ex} occurred while trying to echo: {message} to the forwarder: {forwarder}", "Echo");
                LogUtil.LogSafe(ex, "Echo");
            }
        }
        LogUtil.LogInfo(message, "Echo");
    }
}
