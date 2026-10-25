namespace SysBot.Base;

public interface ISwitchConnectionSync : IConsoleConnectionSync, IConsoleConnection
{
    ulong GetMainNsoBase();

    ulong GetHeapBase();

    byte[] ReadBytesMain(ulong offset, int length);

    byte[] ReadBytesAbsolute(ulong offset, int length);

    void WriteBytesMain(byte[] data, ulong offset);

    void WriteBytesAbsolute(byte[] data, ulong offset);
}
