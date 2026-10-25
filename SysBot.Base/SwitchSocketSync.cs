using System;
using System.Threading;

namespace SysBot.Base;

public sealed class SwitchSocketSync : SwitchSocket, ISwitchConnectionSync, IConsoleConnectionSync, IConsoleConnection
{
    public SwitchSocketSync(IWirelessConnectionConfig cfg)
        : base(cfg)
    {
    }

    public override void Connect()
    {
        Log("Connecting to device...");
        Connection.Connect(Info.IP, Info.Port);
        Connected = true;
        Log("Connected!");
    }

    public override void Reset()
    {
        Disconnect();
        Connect();
    }

    public override void Disconnect()
    {
        Log("Disconnecting from device...");
        Connection.Disconnect(reuseSocket: false);
        Connected = false;
        Log("Disconnected!");
    }

    private int Read(byte[] buffer)
    {
        return Connection.Receive(buffer);
    }

    public int Send(byte[] buffer)
    {
        return Connection.Send(buffer);
    }

    private byte[] ReadResponse(int length)
    {
        Thread.Sleep(MaximumTransferSize / DelayFactor + BaseDelay);
        byte[] array = new byte[length * 2 + 1];
        int num = Read(array);
        return Decoder.ConvertHexByteStringToBytes(array);
    }

    public ulong GetMainNsoBase()
    {
        Send(SwitchCommand.GetMainNsoBase());
        byte[] array = ReadResponse(8);
        Array.Reverse(array, 0, 8);
        return BitConverter.ToUInt64(array, 0);
    }

    public ulong GetHeapBase()
    {
        Send(SwitchCommand.GetHeapBase());
        byte[] array = ReadResponse(8);
        Array.Reverse(array, 0, 8);
        return BitConverter.ToUInt64(array, 0);
    }

    public byte[] ReadBytes(uint offset, int length)
    {
        return Read(offset, length, SwitchOffsetType.Heap);
    }

    public byte[] ReadBytesMain(ulong offset, int length)
    {
        return Read(offset, length, SwitchOffsetType.Main);
    }

    public byte[] ReadBytesAbsolute(ulong offset, int length)
    {
        return Read(offset, length, SwitchOffsetType.Absolute);
    }

    public void WriteBytes(byte[] data, uint offset)
    {
        Write(data, offset, SwitchOffsetType.Heap);
    }

    public void WriteBytesMain(byte[] data, ulong offset)
    {
        Write(data, offset, SwitchOffsetType.Main);
    }

    public void WriteBytesAbsolute(byte[] data, ulong offset)
    {
        Write(data, offset, SwitchOffsetType.Absolute);
    }

    private byte[] Read(ulong offset, int length, SwitchOffsetType type)
    {
        Func<ulong, int, byte[]> readMethod = type.GetReadMethod();
        if (length <= MaximumTransferSize)
        {
            byte[] buffer = readMethod(offset, length);
            Send(buffer);
            return ReadResponse(length);
        }
        byte[] array = new byte[length];
        for (int i = 0; i < length; i += MaximumTransferSize)
        {
            int num = MaximumTransferSize;
            int num2 = length - i;
            if (num2 < MaximumTransferSize)
            {
                num = num2;
            }
            byte[] buffer2 = readMethod(offset + (uint)i, num);
            Send(buffer2);
            byte[] array2 = ReadResponse(num);
            array2.CopyTo(array, i);
        }
        return array;
    }

    private void Write(byte[] data, ulong offset, SwitchOffsetType type)
    {
        Func<ulong, byte[], byte[]> writeMethod = type.GetWriteMethod();
        if (data.Length <= MaximumTransferSize)
        {
            byte[] buffer = writeMethod(offset, data);
            Send(buffer);
            return;
        }
        int num = data.Length;
        for (int i = 0; i < num; i += MaximumTransferSize)
        {
            byte[] arg = data.SliceSafe(i, MaximumTransferSize);
            byte[] buffer2 = writeMethod(offset + (uint)i, arg);
            Send(buffer2);
            Thread.Sleep(MaximumTransferSize / DelayFactor + BaseDelay);
        }
    }
}
