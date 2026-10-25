using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

public abstract class SwitchRoutineExecutor<T> : RoutineExecutor<T> where T : class, IConsoleBotConfig
{
    public readonly bool UseCRLF;

    protected readonly ISwitchConnectionAsync SwitchConnection;

    protected SwitchRoutineExecutor(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> cfg)
        : base(cfg)
    {
        UseCRLF = cfg.GetInnerConfig() is ISwitchConnectionConfig switchConnectionConfig && switchConnectionConfig.UseCRLF;
        if (!(Connection is ISwitchConnectionAsync switchConnection))
        {
            throw new Exception("Not a valid switch connection");
        }
        SwitchConnection = switchConnection;
    }

    public override async Task InitialStartup(CancellationToken token)
    {
        await EchoCommands(value: false, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task Click(SwitchButton b, int delay, CancellationToken token)
    {
        await Connection.SendAsync(SwitchCommand.Click(b, UseCRLF), token).ConfigureAwait(continueOnCapturedContext: false);
        await Task.Delay(delay, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task PressAndHold(SwitchButton b, int hold, int delay, CancellationToken token)
    {
        await Connection.SendAsync(SwitchCommand.Hold(b, UseCRLF), token).ConfigureAwait(continueOnCapturedContext: false);
        await Task.Delay(hold, token).ConfigureAwait(continueOnCapturedContext: false);
        await Connection.SendAsync(SwitchCommand.Release(b, UseCRLF), token).ConfigureAwait(continueOnCapturedContext: false);
        await Task.Delay(delay, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task DaisyChainCommands(int delay, IEnumerable<SwitchButton> buttons, CancellationToken token)
    {
        SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, delay, UseCRLF);
        byte[][] commands = buttons.Select((SwitchButton z) => SwitchCommand.Click(z, UseCRLF)).ToArray();
        byte[] chain = commands.SelectMany((byte[] x) => x).ToArray();
        await Connection.SendAsync(chain, token).ConfigureAwait(continueOnCapturedContext: false);
        SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, 0, UseCRLF);
    }

    public async Task SetStick(SwitchStick stick, short x, short y, int delay, CancellationToken token)
    {
        byte[] cmd = SwitchCommand.SetStick(stick, x, y, UseCRLF);
        await Connection.SendAsync(cmd, token).ConfigureAwait(continueOnCapturedContext: false);
        await Task.Delay(delay, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task DetachController(CancellationToken token)
    {
        await Connection.SendAsync(SwitchCommand.DetachController(UseCRLF), token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task SetScreen(ScreenState state, CancellationToken token)
    {
        await Connection.SendAsync(SwitchCommand.SetScreen(state, UseCRLF), token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task EchoCommands(bool value, CancellationToken token)
    {
        byte[] cmd = SwitchCommand.Configure(SwitchConfigureParameter.echoCommands, value ? 1 : 0, UseCRLF);
        await Connection.SendAsync(cmd, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<bool> ReadUntilChanged(uint offset, byte[] comparison, int waitms, int waitInterval, bool match, CancellationToken token)
    {
        return await ReadUntilChanged(offset, comparison, waitms, waitInterval, match, absolute: false, token).ConfigureAwait(continueOnCapturedContext: false);
    }

    public async Task<bool> ReadUntilChanged(ulong offset, byte[] comparison, int waitms, int waitInterval, bool match, bool absolute, CancellationToken token)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        do
        {
            Task<byte[]> task = (absolute ? SwitchConnection.ReadBytesAbsoluteAsync(offset, comparison.Length, token) : SwitchConnection.ReadBytesAsync((uint)offset, comparison.Length, token));
            if (match == Enumerable.SequenceEqual(await task.ConfigureAwait(continueOnCapturedContext: false), comparison))
            {
                return true;
            }
            await Task.Delay(waitInterval, token).ConfigureAwait(continueOnCapturedContext: false);
        }
        while (sw.ElapsedMilliseconds < waitms);
        return false;
    }

    public static async Task<string> GetVersionAsync(ISwitchConnectionAsync connection, CancellationToken token)
    {
        byte[] gvbytes = Encoding.ASCII.GetBytes("getVersion\r\n");
        byte[] socketReturn = await connection.ReadRaw(gvbytes, 9, token).ConfigureAwait(continueOnCapturedContext: false);
        return Encoding.UTF8.GetString(socketReturn).TrimEnd(default(char)).TrimEnd('\n');
    }

    public static async Task<bool> CompareScreenImageAsync(ISwitchConnectionAsync connection, IScreenshotComparer comparer, CancellationToken token)
    {
        using SwitchScreenshot screenShot = new SwitchScreenshot(await connection.PixelPeek(token).ConfigureAwait(continueOnCapturedContext: false));
        return comparer.Compare(screenShot);
    }
}
