#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

public sealed class SwitchUSBAsync : SwitchUSB, ISwitchConnectionAsync, IConsoleConnectionAsync, IConsoleConnection
{
    public SwitchUSBAsync(int port)
        : base(port)
    {
    }

    public Task<int> SendAsync(byte[] data, CancellationToken token)
    {
        Debug.Assert(data.Length < MaximumTransferSize);
        return Task.Run(() => Send(data), token);
    }

    public Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token)
    {
        return Task.Run(() => Read(offset, length, SwitchOffsetType.Heap.GetReadMethod(crlf: false)), token);
    }

    public Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token)
    {
        return Task.Run(() => Read(offset, length, SwitchOffsetType.Main.GetReadMethod(crlf: false)), token);
    }

    public Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token)
    {
        return Task.Run(() => Read(offset, length, SwitchOffsetType.Absolute.GetReadMethod(crlf: false)), token);
    }

    public Task<byte[]> ReadBytesMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token)
    {
        return Task.Run(() => ReadMulti(offsetSizes, SwitchOffsetType.Heap.GetReadMultiMethod(crlf: false)), token);
    }

    public Task<byte[]> ReadBytesMainMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token)
    {
        return Task.Run(() => ReadMulti(offsetSizes, SwitchOffsetType.Main.GetReadMultiMethod(crlf: false)), token);
    }

    public Task<byte[]> ReadBytesAbsoluteMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token)
    {
        return Task.Run(() => ReadMulti(offsetSizes, SwitchOffsetType.Absolute.GetReadMultiMethod(crlf: false)), token);
    }

    public Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Write(data, offset, SwitchOffsetType.Heap.GetWriteMethod(crlf: false));
        }, token);
    }

    public Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Write(data, offset, SwitchOffsetType.Main.GetWriteMethod(crlf: false));
        }, token);
    }

    public Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Write(data, offset, SwitchOffsetType.Absolute.GetWriteMethod(crlf: false));
        }, token);
    }

    public Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.GetMainNsoBase(crlf: false));
            byte[] value = ReadBulkUSB();
            return BitConverter.ToUInt64(value, 0);
        }, token);
    }

    public Task<ulong> GetHeapBaseAsync(CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.GetHeapBase(crlf: false));
            byte[] value = ReadBulkUSB();
            return BitConverter.ToUInt64(value, 0);
        }, token);
    }

    public Task<string> GetTitleID(CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.GetTitleID(crlf: false));
            byte[] value = ReadBulkUSB();
            return BitConverter.ToUInt64(value, 0).ToString("X16").Trim();
        }, token);
    }

    public Task<byte[]> ReadRaw(byte[] command, int length, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(command);
            return ReadBulkUSB();
        }, token);
    }

    public Task SendRaw(byte[] command, CancellationToken token)
    {
        return Task.Run(() => Send(command), token);
    }

    public Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.PointerPeek(jumps, size, crlf: false));
            return ReadBulkUSB();
        }, token);
    }

    public Task PointerPoke(byte[] data, IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run(() => Send(SwitchCommand.PointerPoke(jumps, data, crlf: false)), token);
    }

    public Task<ulong> PointerAll(IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.PointerAll(jumps, crlf: false));
            byte[] value = ReadBulkUSB();
            return BitConverter.ToUInt64(value, 0);
        }, token);
    }

    public Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.PointerRelative(jumps, crlf: false));
            byte[] value = ReadBulkUSB();
            return BitConverter.ToUInt64(value, 0);
        }, token);
    }

    public Task<byte[]> PixelPeek(CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.PixelPeek(crlf: false));
            return ReadBulkUSB();
        }, token);
    }
}
