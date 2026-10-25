namespace SysBot.Base;

public interface IConsoleBotConnector<out TSync, out TAsync>
{
    TSync CreateSync();

    TAsync CreateAsynchronous();
}
