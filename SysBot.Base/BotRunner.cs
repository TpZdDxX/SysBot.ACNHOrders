using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Base;

public class BotRunner<T> where T : class, IConsoleBotConfig
{
    public readonly List<BotSource<T>> Bots = new List<BotSource<T>>();

    public bool IsRunning => Bots.Any((BotSource<T> z) => z.IsRunning);

    public bool RunOnce { get; private set; }

    public virtual void Add(RoutineExecutor<T> bot)
    {
        if (Bots.Any((BotSource<T> z) => z.Bot.Connection.Equals(bot.Connection)))
        {
            throw new ArgumentException("Connection has already been added.");
        }
        Bots.Add(new BotSource<T>(bot));
    }

    public virtual bool Remove(IConsoleBotConfig cfg, bool callStop)
    {
        BotSource<T>? bot = GetBot(cfg);
        if (bot == null)
        {
            return false;
        }
        if (callStop)
        {
            bot.Stop();
        }
        return Bots.Remove(bot);
    }

    public virtual void InitializeStart()
    {
        RunOnce = true;
    }

    public virtual void StartAll()
    {
        foreach (BotSource<T> bot in Bots)
        {
            bot.Start();
        }
    }

    public virtual void StopAll()
    {
        foreach (BotSource<T> bot in Bots)
        {
            bot.Stop();
        }
    }

    public virtual void PauseAll()
    {
        foreach (BotSource<T> bot in Bots)
        {
            bot.Pause();
        }
    }

    public virtual void ResumeAll()
    {
        foreach (BotSource<T> bot in Bots)
        {
            bot.Resume();
        }
    }

    public BotSource<T>? GetBot(IConsoleBotConfig config)
    {
        return Bots.Find((BotSource<T> z) => z.Bot.Config.Equals(config));
    }

    public BotSource<T>? GetBot(string ip)
    {
        return Bots.Find((BotSource<T> z) => z.Bot.Config.Matches(ip));
    }
}
