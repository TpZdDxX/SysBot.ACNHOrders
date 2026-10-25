namespace SysBot.Base;

public interface IConsoleBotConfig
{
    bool IsValid();

    bool Matches(string magic);
}
