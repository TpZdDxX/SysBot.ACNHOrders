using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

public interface IRoutineExecutor
{
    string LastLogged { get; }

    DateTime LastTime { get; }

    void ReportStatus();

    void Log(string message);

    string GetSummary();

    Task RunAsync(CancellationToken token);

    Task MainLoop(CancellationToken token);

    Task InitialStartup(CancellationToken token);

    void SoftStop();
}
