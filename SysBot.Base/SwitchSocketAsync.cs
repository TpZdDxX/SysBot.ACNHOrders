using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

public sealed class SwitchSocketAsync(IWirelessConnectionConfig cfg) : SwitchSocket(cfg), ISwitchConnectionAsync, IConsoleConnectionAsync, IConsoleConnection, IAsyncConnection
{
    private readonly AutoResetEvent connectionDone = new AutoResetEvent(initialState: false);

    private readonly AutoResetEvent disconnectDone = new AutoResetEvent(initialState: false);

    public override void Connect()
    {
        if (Connected)
        {
            Log("Already connected prior, skipping initial connection.");
            return;
        }
        Log("Connecting to device...");
        Connection.Connect(Info.IP, Info.Port);
        Connected = true;
        Log("Connected!");
        Label = Name;
    }

    public override void Reset()
    {
        string iP = Info.IP;
        if (Connected)
        {
            Disconnect();
        }
        InitializeSocket();
        Log("Connecting to device...");
        IPAddress[] hostAddresses = Dns.GetHostAddresses(iP);
        IPAddress[] array = hostAddresses;
        foreach (IPAddress address in array)
        {
            IPEndPoint remoteEP = new IPEndPoint(address, Info.Port);
            Connection.BeginConnect(remoteEP, ConnectCallback, Connection);
            Connected = true;
            Log("Connected!");
        }
    }

    public override void Disconnect()
    {
        Log("Disconnecting from device...");
        Connection.Shutdown(SocketShutdown.Both);
        Connection.BeginDisconnect(reuseSocket: true, DisconnectCallback, Connection);
        Connected = false;
        Log("Disconnected! Resetting Socket.");
        InitializeSocket();
    }

    public void ConnectCallback(IAsyncResult ar)
    {
        Socket socket = (Socket)ar.AsyncState!;
        socket.EndConnect(ar);
        connectionDone.Set();
        LogUtil.LogInfo("Connected.", Name);
    }

    public void DisconnectCallback(IAsyncResult ar)
    {
        Socket socket = (Socket)ar.AsyncState!;
        socket.EndDisconnect(ar);
        disconnectDone.Set();
        LogUtil.LogInfo("Disconnected.", Name);
    }

    private int Read(byte[] buffer)
    {
        int i;
        for (i = Connection.Receive(buffer, 0, 1, SocketFlags.None); buffer[i - 1] != 10; i += Connection.Receive(buffer, i, 1, SocketFlags.None))
        {
        }
        return i;
    }

    // Bot loop and Discord commands share this socket, so a reply could be read by the wrong caller
    private readonly SemaphoreSlim CommandLock = new SemaphoreSlim(1, 1);

    public async Task<int> SendAsync(byte[] buffer, CancellationToken token)
    {
        await CommandLock.WaitAsync(token).ConfigureAwait(continueOnCapturedContext: false);
        try
        {
            return await Task.Run(() => Connection.Send(buffer), token).ConfigureAwait(continueOnCapturedContext: false);
        }
        finally
        {
            CommandLock.Release();
        }
    }

    private async Task<int> SendAndReadAsync(byte[] command, byte[] buffer, CancellationToken token)
    {
        await CommandLock.WaitAsync(token).ConfigureAwait(continueOnCapturedContext: false);
        try
        {
            try
            {
                await Task.Run(() => Connection.Send(command), token).ConfigureAwait(continueOnCapturedContext: false);
                return Read(buffer);
            }
            // A late reply would be read by the next command, so ask again on a fresh socket
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                LogError("Switch stopped replying, reconnecting.");
                Connection.Close();
                InitializeSocket();
                Connected = false;
                Connect();
                await Task.Run(() => Connection.Send(command), token).ConfigureAwait(continueOnCapturedContext: false);
                return Read(buffer);
            }
        }
        finally
        {
            CommandLock.Release();
        }
    }

    private async Task<byte[]> ReadBytesFromCmdAsync(byte[] cmd, int length, CancellationToken token)
    {
        byte[] buffer = new byte[length * 2 + 1];
        await SendAndReadAsync(cmd, buffer, token).ConfigureAwait(continueOnCapturedContext: false);
        return Decoder.ConvertHexByteStringToBytes(buffer);
    }

    public async Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token)
    {
        return await Read(offset, length, SwitchOffsetType.Heap, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token)
    {
        return await Read(offset, length, SwitchOffsetType.Main, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token)
    {
        return await Read(offset, length, SwitchOffsetType.Absolute, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<byte[]> ReadBytesMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token)
    {
        return await ReadMulti(offsetSizes, SwitchOffsetType.Heap, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<byte[]> ReadBytesMainMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token)
    {
        return await ReadMulti(offsetSizes, SwitchOffsetType.Main, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<byte[]> ReadBytesAbsoluteMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token)
    {
        return await ReadMulti(offsetSizes, SwitchOffsetType.Absolute, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token)
    {
        await Write(data, offset, SwitchOffsetType.Heap, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token)
    {
        await Write(data, offset, SwitchOffsetType.Main, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token)
    {
        await Write(data, offset, SwitchOffsetType.Absolute, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
    {
        byte[] baseBytes = await ReadBytesFromCmdAsync(SwitchCommand.GetMainNsoBase(), 8, token).ConfigureAwait(continueOnCapturedContext: false);
        Array.Reverse(baseBytes, 0, 8);
        return BitConverter.ToUInt64(baseBytes, 0);
    }

    public async Task<ulong> GetHeapBaseAsync(CancellationToken token)
    {
        byte[] baseBytes = await ReadBytesFromCmdAsync(SwitchCommand.GetHeapBase(), 8, token).ConfigureAwait(continueOnCapturedContext: false);
        Array.Reverse(baseBytes, 0, 8);
        return BitConverter.ToUInt64(baseBytes, 0);
    }

    public async Task<string> GetTitleID(CancellationToken token)
    {
        byte[] bytes = await ReadRaw(SwitchCommand.GetTitleID(), 17, token).ConfigureAwait(continueOnCapturedContext: false);
        return Encoding.ASCII.GetString(bytes).Trim();
    }

    private async Task<byte[]> Read(ulong offset, int length, SwitchOffsetType type, CancellationToken token)
    {
        Func<ulong, int, byte[]> method = type.GetReadMethod();
        if (length <= MaximumTransferSize)
        {
            byte[] cmd = method(offset, length);
            return await ReadBytesFromCmdAsync(cmd, length, token).ConfigureAwait(continueOnCapturedContext: false);
        }
        byte[] result = new byte[length];
        for (int i = 0; i < length; i += MaximumTransferSize)
        {
            int len = MaximumTransferSize;
            int delta = length - i;
            if (delta < MaximumTransferSize)
            {
                len = delta;
            }
            byte[] cmd2 = method(offset + (uint)i, len);
            (await ReadBytesFromCmdAsync(cmd2, len, token).ConfigureAwait(continueOnCapturedContext: false)).CopyTo(result, i);
            await Task.Delay(MaximumTransferSize / DelayFactor + BaseDelay, token).ConfigureAwait(continueOnCapturedContext: false);
        }
        return result;
    }

    private async Task<byte[]> ReadMulti(IReadOnlyDictionary<ulong, int> offsetSizes, SwitchOffsetType type, CancellationToken token)
    {
        Func<IReadOnlyDictionary<ulong, int>, byte[]> method = type.GetReadMultiMethod();
        byte[] cmd = method(offsetSizes);
        int totalSize = offsetSizes.Values.Sum();
        return await ReadBytesFromCmdAsync(cmd, totalSize, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task Write(byte[] data, ulong offset, SwitchOffsetType type, CancellationToken token)
    {
        Func<ulong, byte[], byte[]> method = type.GetWriteMethod();
        if (data.Length <= MaximumTransferSize)
        {
            byte[] cmd = method(offset, data);
            await SendAsync(cmd, token).ConfigureAwait(continueOnCapturedContext: false);
            return;
        }
        int byteCount = data.Length;
        for (int i = 0; i < byteCount; i += MaximumTransferSize)
        {
            byte[] slice = data.SliceSafe(i, MaximumTransferSize);
            byte[] cmd2 = method(offset + (uint)i, slice);
            await SendAsync(cmd2, token).ConfigureAwait(continueOnCapturedContext: false);
            await Task.Delay(MaximumTransferSize / DelayFactor + BaseDelay, token).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    public async Task<byte[]> ReadRaw(byte[] command, int length, CancellationToken token)
    {
        byte[] buffer = new byte[length];
        await SendAndReadAsync(command, buffer, token).ConfigureAwait(continueOnCapturedContext: false);
        return buffer;
    }

    public async Task SendRaw(byte[] command, CancellationToken token)
    {
        await SendAsync(command, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token)
    {
        return await ReadBytesFromCmdAsync(SwitchCommand.PointerPeek(jumps, size), size, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task PointerPoke(byte[] data, IEnumerable<long> jumps, CancellationToken token)
    {
        await SendAsync(SwitchCommand.PointerPoke(jumps, data), token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<ulong> PointerAll(IEnumerable<long> jumps, CancellationToken token)
    {
        byte[] offsetBytes = await ReadBytesFromCmdAsync(SwitchCommand.PointerAll(jumps), 8, token).ConfigureAwait(continueOnCapturedContext: false);
        Array.Reverse(offsetBytes, 0, 8);
        return BitConverter.ToUInt64(offsetBytes, 0);
    }

    public async Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token)
    {
        byte[] offsetBytes = await ReadBytesFromCmdAsync(SwitchCommand.PointerRelative(jumps), 8, token).ConfigureAwait(continueOnCapturedContext: false);
        Array.Reverse(offsetBytes, 0, 8);
        return BitConverter.ToUInt64(offsetBytes, 0);
    }

    public async Task<byte[]> PixelPeek(CancellationToken token)
    {
        byte[] buffer = new byte[1024001];
        int len = await SendAndReadAsync(SwitchCommand.PixelPeek(), buffer, token).ConfigureAwait(continueOnCapturedContext: false);
        byte[] bytes = buffer.Take(len).ToArray();
        return Decoder.ConvertHexByteStringToBytes(bytes);
    }
}
