using System;
using System.Collections.Generic;
using System.Threading;

namespace SysBot.Base;

public class BotSynchronizer
{
    private readonly ISynchronizationSetting Config;

    public readonly Barrier Barrier;

    public readonly List<Action> BarrierReleasingActions = new List<Action>();

    public BotSynchronizer(ISynchronizationSetting hub)
    {
        Config = hub;
        Barrier = new Barrier(0, ReleaseBarrier);
    }

    private void ReleaseBarrier(Barrier b)
    {
        foreach (Action barrierReleasingAction in BarrierReleasingActions)
        {
            barrierReleasingAction();
        }
        int synchronizeDelayBarrier = Config.SynchronizeDelayBarrier;
        if (synchronizeDelayBarrier != 0)
        {
            Thread.Sleep(synchronizeDelayBarrier);
        }
    }
}
