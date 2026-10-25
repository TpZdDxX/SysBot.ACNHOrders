namespace SysBot.Base;

public interface IConsoleBotManaged<out TSync, out TAsync> : IConsoleBotConfig, IConsoleBotConnector<TSync, TAsync>
{
    IConsoleBotConfig GetInnerConfig();
}
