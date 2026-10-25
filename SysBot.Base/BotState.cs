using System;

namespace SysBot.Base;

[Serializable]
public abstract class BotState<TEnum, TConnection> : IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync>, IConsoleBotConfig, IConsoleBotConnector<IConsoleConnection, IConsoleConnectionAsync> where TEnum : struct, Enum where TConnection : IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync>, new()
{
    public TConnection Connection { get; set; } = new TConnection();

    public TEnum InitialRoutine { get; set; }

    public TEnum CurrentRoutineType { get; protected set; }

    public TEnum NextRoutineType { get; protected set; }

    public IConsoleBotConfig GetInnerConfig()
    {
        return Connection;
    }

    public bool IsValid()
    {
        return Connection.IsValid();
    }

    public bool Matches(string magic)
    {
        return Connection.Matches(magic);
    }

    public IConsoleConnection CreateSync()
    {
        return Connection.CreateSync();
    }

    public IConsoleConnectionAsync CreateAsynchronous()
    {
        return Connection.CreateAsynchronous();
    }

    public abstract void IterateNextRoutine();

    public abstract void Initialize();

    public abstract void Pause();

    public abstract void Resume();

    public void Initialize(TEnum type)
    {
        NextRoutineType = type;
        InitialRoutine = type;
    }
}
