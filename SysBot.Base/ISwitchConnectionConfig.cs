namespace SysBot.Base;

public interface ISwitchConnectionConfig : IConsoleBotManaged<ISwitchConnectionSync, ISwitchConnectionAsync>, IConsoleBotConfig, IConsoleBotConnector<ISwitchConnectionSync, ISwitchConnectionAsync>
{
    SwitchProtocol Protocol { get; }

    bool UseCRLF { get; }
}
