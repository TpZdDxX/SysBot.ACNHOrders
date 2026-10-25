using System;

namespace SysBot.Base;

public sealed class SwitchUSBSync : SwitchUSB, ISwitchConnectionSync, IConsoleConnectionSync, IConsoleConnection
{
    public SwitchUSBSync(int port)
        : base(port)
    {
    }

    public byte[] ReadBytes(uint offset, int length)
    {
        return Read(offset, length, SwitchOffsetType.Heap.GetReadMethod(crlf: false));
    }

    public byte[] ReadBytesMain(ulong offset, int length)
    {
        return Read(offset, length, SwitchOffsetType.Main.GetReadMethod(crlf: false));
    }

    public byte[] ReadBytesAbsolute(ulong offset, int length)
    {
        return Read(offset, length, SwitchOffsetType.Absolute.GetReadMethod(crlf: false));
    }

    public void WriteBytes(byte[] data, uint offset)
    {
        Write(data, offset, SwitchOffsetType.Heap.GetWriteMethod(crlf: false));
    }

    public void WriteBytesMain(byte[] data, ulong offset)
    {
        Write(data, offset, SwitchOffsetType.Main.GetWriteMethod(crlf: false));
    }

    public void WriteBytesAbsolute(byte[] data, ulong offset)
    {
        Write(data, offset, SwitchOffsetType.Absolute.GetWriteMethod(crlf: false));
    }

    public ulong GetMainNsoBase()
    {
        Send(SwitchCommand.GetMainNsoBase(crlf: false));
        byte[] value = ReadBulkUSB();
        return BitConverter.ToUInt64(value, 0);
    }

    public ulong GetHeapBase()
    {
        Send(SwitchCommand.GetHeapBase(crlf: false));
        byte[] value = ReadBulkUSB();
        return BitConverter.ToUInt64(value, 0);
    }
}
