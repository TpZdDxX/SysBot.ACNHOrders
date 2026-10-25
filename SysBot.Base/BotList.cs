using System;

namespace SysBot.Base;

public class BotList<T> where T : IConsoleBotConfig
{
    public T[] Bots { get; set; } = Array.Empty<T>();
}
