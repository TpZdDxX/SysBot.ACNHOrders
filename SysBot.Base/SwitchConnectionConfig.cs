using System;
using System.Net;

namespace SysBot.Base;

public record SwitchConnectionConfig : ISwitchConnectionConfig, IConsoleBotManaged<ISwitchConnectionSync, ISwitchConnectionAsync>, IConsoleBotConfig, IConsoleBotConnector<ISwitchConnectionSync, ISwitchConnectionAsync>, IWirelessConnectionConfig
{
    public SwitchProtocol Protocol { get; set; }

    public string IP { get; set; } = string.Empty;

    public int Port { get; set; } = 6000;

    public bool UseCRLF => Protocol == SwitchProtocol.WiFi;

    public bool IsValid()
    {
        SwitchProtocol protocol = Protocol;
        if (1 == 0)
        {
        }
        bool result = protocol switch
        {
            SwitchProtocol.WiFi => IPAddress.TryParse(IP, out var _),
            SwitchProtocol.USB => Port < 65535,
            _ => false,
        };
        if (1 == 0)
        {
        }
        return result;
    }

    public bool Matches(string magic)
    {
        SwitchProtocol protocol = Protocol;
        if (1 == 0)
        {
        }
        bool result = protocol switch
        {
            SwitchProtocol.WiFi => IPAddress.TryParse(magic, out var address) && address.ToString() == IP,
            SwitchProtocol.USB => magic == Port.ToString(),
            _ => false,
        };
        if (1 == 0)
        {
        }
        return result;
    }

    public IConsoleBotConfig GetInnerConfig()
    {
        return this;
    }

    public override string ToString()
    {
        SwitchProtocol protocol = Protocol;
        if (1 == 0)
        {
        }
        string result = protocol switch
        {
            SwitchProtocol.WiFi => IP,
            SwitchProtocol.USB => Port.ToString(),
            _ => throw new ArgumentOutOfRangeException("SwitchProtocol"),
        };
        if (1 == 0)
        {
        }
        return result;
    }

    public ISwitchConnectionAsync CreateAsynchronous()
    {
        SwitchProtocol protocol = Protocol;
        if (1 == 0)
        {
        }
        ISwitchConnectionAsync result = protocol switch
        {
            SwitchProtocol.WiFi => (ISwitchConnectionAsync)new SwitchSocketAsync(this),
            SwitchProtocol.USB => new SwitchUSBAsync(Port),
            _ => throw new ArgumentOutOfRangeException("SwitchProtocol"),
        };
        if (1 == 0)
        {
        }
        return result;
    }

    public ISwitchConnectionSync CreateSync()
    {
        SwitchProtocol protocol = Protocol;
        if (1 == 0)
        {
        }
        ISwitchConnectionSync result = protocol switch
        {
            SwitchProtocol.WiFi => (ISwitchConnectionSync)new SwitchSocketSync(this),
            SwitchProtocol.USB => new SwitchUSBSync(Port),
            _ => throw new ArgumentOutOfRangeException("SwitchProtocol"),
        };
        if (1 == 0)
        {
        }
        return result;
    }
}
