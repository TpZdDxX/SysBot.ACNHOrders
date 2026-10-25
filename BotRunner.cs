using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using SysBot.ACNHOrders.Twitch;
using SysBot.ACNHOrders.Signalr;

namespace SysBot.ACNHOrders
{
    public static class BotRunner
    {
        public static async Task RunFrom(CrossBotConfig config, CancellationToken cancel, TwitchConfig? tConfig = null)
        {
            // Set up logging for Console Window
            LogUtil.Forwarders.Add(Logger);
            static void Logger(string msg, string identity) => Console.WriteLine(GetMessage(msg, identity));
            static string GetMessage(string msg, string identity) => $"> [{DateTime.Now:hh:mm:ss}] - {identity}: {msg}";

            var bot = new CrossBot(config);

            var sys = new SysCord(bot);

            Globals.Self = sys;
            Globals.Bot = bot;
            Globals.Hub = QueueHub.CurrentInstance;
            GlobalBan.UpdateConfiguration(config);

            bot.Log("Starting Discord.");
#pragma warning disable 4014
            Task.Run(() => sys.MainAsync(config.Token, cancel), cancel);
#pragma warning restore 4014


            if (tConfig != null && !string.IsNullOrWhiteSpace(tConfig.Token))
            {
                bot.Log("Starting Twitch.");
                var _ = new TwitchCrossBot(tConfig, bot);
            }

            if (!string.IsNullOrWhiteSpace(config.SignalrConfig.URIEndpoint))
            {
                bot.Log("Starting Web.");
                var _ = new SignalrCrossBot(config.SignalrConfig, bot);
            }

            if (config.SkipConsoleBotCreation)
            {
                await Task.Delay(-1, cancel).ConfigureAwait(false);
                return;
            }

            while (!cancel.IsCancellationRequested)
            {
                bot.Log("Starting bot loop.");

                try
                {
                    await bot.RunAsync(cancel).ConfigureAwait(false);
                    bot.Log("Bot has terminated.");
                }
                catch (Exception ex) when (!cancel.IsCancellationRequested)
                {
                    // Retry in-process instead of crashing; a crash means a fresh Discord login every cycle.
                    bot.Log("Bot has terminated due to an error:");
                    bot.Log(ex.Message);
                    if (ex.StackTrace != null)
                        bot.Log(ex.StackTrace);
                    try { bot.Connection.Disconnect(); } catch { } // RunAsync skips this on error; not gated on Connected since USB never sets it
                }

                bot.Log("Please wait... Attempting to reconnect in 10 seconds.");
                await Task.Delay(10_000, cancel).ConfigureAwait(false);
                bot.Log("Bot is attempting a restart...");
                var notifiers = bot.DodoNotifiers;
                bot = new CrossBot(config);
                foreach (var n in notifiers)
                    bot.DodoNotifiers.Add(n);
                Globals.Bot = bot;
                TwitchCrossBot.Bot = bot;
                // Reuse the existing Discord session; re-logging in on every bot restart burns Discord's 1000 identify/day limit.
                sys.Bot = bot;
            }
        }
    }
}
