using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

public class BotSource<T> where T : class, IConsoleBotConfig
{
    public readonly RoutineExecutor<T> Bot;

    private CancellationTokenSource Source = new CancellationTokenSource();

    public bool IsRunning { get; private set; }

    public bool IsPaused { get; private set; }

    private bool IsStopping { get; set; }

    public BotSource(RoutineExecutor<T> bot)
    {
        Bot = bot;
    }

    public void Stop()
    {
        if (IsRunning && !IsStopping)
        {
            IsStopping = true;
            Source.Cancel();
            Source = new CancellationTokenSource();
            Task.Run(async () =>
            {
                await Bot.HardStop().ConfigureAwait(continueOnCapturedContext: false);
                bool flag = (IsStopping = false);
                bool isPaused = (IsRunning = flag);
                IsPaused = isPaused;
            });
        }
    }

    public void Pause()
    {
        if (IsRunning && !IsStopping)
        {
            IsPaused = true;
            Bot.SoftStop();
        }
    }

    public void Start()
    {
        if (IsPaused)
        {
            Stop();
        }
        if (IsRunning || IsStopping)
        {
            return;
        }
        Task.Run(() => Bot.RunAsync(Source.Token).ContinueWith(ReportFailure, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously).ContinueWith((Task _) => IsRunning = false));
        IsRunning = true;
    }

    private void ReportFailure(Task finishedTask)
    {
        string name = Bot.Connection.Name;
        AggregateException? exception = finishedTask.Exception;
        if (exception == null)
        {
            LogUtil.LogError("Bot has stopped without error.", name);
            return;
        }
        LogUtil.LogError("Bot has crashed!", name);
        if (!string.IsNullOrEmpty(exception.Message))
        {
            LogUtil.LogError("Aggregate message: " + exception.Message, name);
        }
        string? stackTrace = exception.StackTrace;
        if (!string.IsNullOrEmpty(stackTrace))
        {
            LogUtil.LogError("Aggregate stacktrace: " + stackTrace, name);
        }
        foreach (Exception innerException in exception.InnerExceptions)
        {
            if (!string.IsNullOrEmpty(innerException.Message))
            {
                LogUtil.LogError("Inner message: " + innerException.Message, name);
            }
            LogUtil.LogError("Inner stacktrace: " + innerException.StackTrace, name);
        }
    }

    public void Resume()
    {
        Start();
    }
}
