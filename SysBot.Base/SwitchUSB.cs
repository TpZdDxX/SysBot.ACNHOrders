using System;
using System.Collections.Generic;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace SysBot.Base;

public abstract class SwitchUSB : IConsoleConnection
{
    private readonly int Port;

    private UsbDevice? SwDevice;

    private UsbEndpointReader? reader;

    private UsbEndpointWriter? writer;

    private readonly object _sync = new object();

    private static readonly object _registry = new object();

    public string Name { get; }

    public string Label { get; set; }

    public bool Connected { get; protected set; }

    public int MaximumTransferSize { get; set; } = 448;

    public int BaseDelay { get; set; } = 1;

    public int DelayFactor { get; set; } = 1000;

    protected SwitchUSB(int port)
    {
        Port = port;
        Name = (Label = $"USB-{port}");
    }

    public void Log(string message)
    {
        LogInfo(message);
    }

    public void LogInfo(string message)
    {
        LogUtil.LogInfo(message, Label);
    }

    public void LogError(string message)
    {
        LogUtil.LogError(message, Label);
    }

    public void Reset()
    {
        Disconnect();
        Connect();
    }

    public void Connect()
    {
        SwDevice = TryFindUSB();
        if (SwDevice == null)
        {
            throw new Exception("USB device not found.");
        }
        if (!(SwDevice is IUsbDevice usbDevice))
        {
            throw new Exception("Device is using a WinUSB driver. Use libusbK and create a filter.");
        }
        lock (_sync)
        {
            if (!usbDevice.UsbRegistryInfo.IsAlive)
            {
                usbDevice.ResetDevice();
            }
            if (usbDevice.IsOpen)
            {
                usbDevice.Close();
            }
            usbDevice.Open();
            usbDevice.SetConfiguration(1);
            if (!usbDevice.ClaimInterface(0))
            {
                usbDevice.ReleaseInterface(0);
                usbDevice.ClaimInterface(0);
            }
            reader = SwDevice.OpenEndpointReader(ReadEndpointID.Ep01);
            writer = SwDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
        }
    }

    private UsbDevice? TryFindUSB()
    {
        lock (_registry)
        {
            foreach (UsbRegistry allLibUsbDevice in UsbDevice.AllLibUsbDevices)
            {
                if (allLibUsbDevice.Vid == 1406 && allLibUsbDevice.Pid == 12288)
                {
                    allLibUsbDevice.DeviceProperties.TryGetValue("Address", out var value);
                    if (!(Port.ToString() != value?.ToString()))
                    {
                        return allLibUsbDevice.Device;
                    }
                }
            }
        }
        return null;
    }

    public void Disconnect()
    {
        lock (_sync)
        {
            UsbDevice? swDevice = SwDevice;
            if (swDevice != null && swDevice.IsOpen)
            {
                if (swDevice is IUsbDevice usbDevice)
                {
                    if (!usbDevice.UsbRegistryInfo.IsAlive)
                    {
                        usbDevice.ResetDevice();
                    }
                    usbDevice.ReleaseInterface(0);
                }
                swDevice.Close();
            }
            reader?.Dispose();
            writer?.Dispose();
        }
    }

    public int Send(byte[] buffer)
    {
        lock (_sync)
        {
            return SendInternal(buffer);
        }
    }

    public int Read(byte[] buffer)
    {
        lock (_sync)
        {
            return ReadInternal(buffer);
        }
    }

    protected byte[] Read(ulong offset, int length, Func<ulong, int, byte[]> method)
    {
        byte[] buffer = method(offset, length);
        SendInternal(buffer);
        return ReadBulkUSB();
    }

    protected byte[] ReadMulti(IReadOnlyDictionary<ulong, int> offsetSizes, Func<IReadOnlyDictionary<ulong, int>, byte[]> method)
    {
        byte[] buffer = method(offsetSizes);
        SendInternal(buffer);
        return ReadBulkUSB();
    }

    protected byte[] ReadBulkUSB()
    {
        Thread.Sleep(1);
        lock (_sync)
        {
            if (reader == null)
            {
                throw new Exception("USB device not found or not connected.");
            }
            byte[] array = new byte[4];
            reader.Read(array, 5000, out var _);
            int num = BitConverter.ToInt32(array, 0);
            byte[] array2 = new byte[num];
            int transferLength2;
            for (int i = 0; i < num; i += transferLength2)
            {
                Thread.Sleep(1);
                ErrorCode errorCode = reader.Read(array2, i, Math.Min(reader.ReadBufferSize, num - i), 5000, out transferLength2);
                if (errorCode != ErrorCode.None)
                {
                    Disconnect();
                    throw new Exception(UsbDevice.LastErrorString);
                }
            }
            return array2;
        }
    }

    protected void Write(byte[] data, ulong offset, Func<ulong, byte[], byte[]> method)
    {
        if (data.Length > MaximumTransferSize)
        {
            WriteLarge(data, offset, method);
        }
        else
        {
            WriteSmall(data, offset, method);
        }
    }

    public void WriteSmall(byte[] data, ulong offset, Func<ulong, byte[], byte[]> method)
    {
        lock (_sync)
        {
            byte[] buffer = method(offset, data);
            SendInternal(buffer);
            Thread.Sleep(1);
        }
    }

    private int ReadInternal(byte[] buffer)
    {
        byte[] buffer2 = new byte[4];
        if (reader == null)
        {
            throw new Exception("USB device not found or not connected.");
        }
        reader.Read(buffer2, 5000, out var _);
        reader.Read(buffer, 5000, out var transferLength2);
        return transferLength2;
    }

    private int SendInternal(byte[] buffer)
    {
        if (writer == null)
        {
            throw new Exception("USB device not found or not connected.");
        }
        uint value = (uint)(buffer.Length + 2);
        ErrorCode errorCode = writer.Write(BitConverter.GetBytes(value), 2000, out var _);
        if (errorCode != ErrorCode.None)
        {
            Disconnect();
            throw new Exception(UsbDevice.LastErrorString);
        }
        errorCode = writer.Write(buffer, 2000, out var transferLength2);
        if (errorCode != ErrorCode.None)
        {
            Disconnect();
            throw new Exception(UsbDevice.LastErrorString);
        }
        return transferLength2;
    }

    private void WriteLarge(byte[] data, ulong offset, Func<ulong, byte[], byte[]> method)
    {
        int num = data.Length;
        for (int i = 0; i < num; i += MaximumTransferSize)
        {
            byte[] data2 = data.SliceSafe(i, MaximumTransferSize);
            Write(data2, offset + (uint)i, method);
            Thread.Sleep(MaximumTransferSize / DelayFactor + BaseDelay);
        }
    }
}
