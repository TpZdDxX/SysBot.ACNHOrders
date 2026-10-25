using NHSE.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace SysBot.ACNHOrders.Twitch
{
    public class TwitchCrossBot
    {
        internal static CrossBot Bot = default!;
        internal static string BotName = default!;
        internal static readonly List<TwitchQueue> QueuePool = new();
        private readonly TwitchClient client;
        private readonly string Channel;
        private readonly TwitchConfig Settings;

        public TwitchCrossBot(TwitchConfig settings, CrossBot bot)
        {
            Settings = settings;
            Bot = bot;
            BotName = settings.Username;

            var credentials = new ConnectionCredentials(settings.Username.ToLower(), settings.Token);

            // TwitchLib throttles over a fixed 30 second window, so scale the configured rate to it
            var messagesPerWindow = settings.ThrottleSeconds > 0 ? settings.ThrottleMessages * 30 / settings.ThrottleSeconds : settings.ThrottleMessages;
            var sendOptions = new SendOptions(sendsAllowedInPeriod: (uint)Math.Clamp(messagesPerWindow, 1, int.MaxValue));

            var lowerKeyDic = new Dictionary<string, string>();
            foreach (var kvp in settings.UserDefinitedCommands)
                lowerKeyDic.Add(kvp.Key.ToLower(), kvp.Value);
            foreach (var kvp in settings.UserDefinedSubOnlyCommands)
                lowerKeyDic.Add(kvp.Key.ToLower(), kvp.Value);
            settings.UserDefinitedCommands = lowerKeyDic;

            Channel = settings.Channel;
            client = new TwitchClient(sendOptions: sendOptions);

            // Must be set before Initialize, which otherwise adds '!' as the prefix
            var commandPrefix = settings.CommandPrefix.ToString();
            client.ChatCommandIdentifiers.Add(commandPrefix);
            client.WhisperCommandIdentifiers.Add(commandPrefix);
            client.Initialize(credentials, Channel);

            client.OnJoinedChannel += (_, e) => Safe(() => Client_OnJoinedChannel(e));
            client.OnMessageReceived += (_, e) => Safe(() => Client_OnMessageReceived(e));
            client.OnWhisperReceived += (_, e) => Safe(() => Client_OnWhisperReceived(e));
            client.OnChatCommandReceived += (_, e) => Safe(() => Client_OnChatCommandReceived(e));
            client.OnWhisperCommandReceived += (_, e) => Safe(() => Client_OnWhisperCommandReceived(e));
            client.OnLeftChannel += (_, e) => Safe(() => Client_OnLeftChannel(e));

            client.OnSendReceiveData += (_, e)
                => Safe(() => LogUtil.LogText($"[{client.TwitchUsername}] - {e.Direction}: {e.Data}"));
            client.OnConnected += (_, e)
                => Safe(() => LogUtil.LogText($"[{client.TwitchUsername}] - Connected as {e.BotUsername}"));
            // TwitchLib reconnects and rejoins channels by itself
            client.OnDisconnected += (_, _)
                => Safe(() => LogUtil.LogText($"[{client.TwitchUsername}] - Disconnected."));
            client.OnReconnected += (_, _)
                => Safe(() => LogUtil.LogText($"[{client.TwitchUsername}] - Reconnected."));
            client.OnMessageSent += (_, e)
                => Safe(() => LogUtil.LogText($"[{client.TwitchUsername}] - Message Sent in {e.SentMessage.Channel}: {e.SentMessage.Message}"));

            client.OnMessageThrottled += (_, e)
                => Safe(() => LogUtil.LogError($"Message Throttled: {e.Reason} {e.ItemNotSent.Message}", "TwitchBot"));

            client.OnError += (_, e)
                => Safe(() => LogUtil.LogError(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace, "TwitchBot"));
            client.OnConnectionError += (_, e)
                => Safe(() => LogUtil.LogError(e.BotUsername + Environment.NewLine + e.Error.Message, "TwitchBot"));

            Observe(client.ConnectAsync(), "Connecting to Twitch");

            EchoUtil.Forwarders.Add(msg => SendMessage(client, Channel, msg));

            // Turn on if verified
            // Hub.Queues.Forwarders.Add((bot, detail) => SendMessage(client, Channel, $"{bot.Connection.Name} is now trading (ID {detail.ID}) {detail.Trainer.TrainerName}"));
        }

        /// <summary>Sends a chat message without throwing; TwitchLib throws if no channel is joined yet.</summary>
        internal static void SendMessage(TwitchClient client, string channel, string message)
        {
            // Twitch's limit, TwitchLib drops longer messages without telling us
            if (message.Length > 500)
            {
                LogUtil.LogError($"Message to {channel} is over 500 characters and was not sent: {message}", "TwitchBot");
                return;
            }

            try
            {
                Observe(client.SendMessageAsync(channel, message), $"Sending to {channel}");
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Unable to send message to {channel}: {ex.Message}", "TwitchBot");
            }
        }

        internal static void LogWhisperUnsupported(string user)
            => LogUtil.LogError($"Twitch no longer delivers whispers sent by bots, so the message to @{user} was not sent. Set this message destination to Channel in the Twitch config.", "TwitchBot");

        // TwitchLib awaits handlers inside its receive loop, so an exception there would stop the bot hearing chat
        private static Task Safe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogUtil.LogError(ex.Message + Environment.NewLine + ex.StackTrace, "TwitchBot");
            }
            return Task.CompletedTask;
        }

        private static void Observe(Task task, string operation)
            => task.ContinueWith(failedTask => LogUtil.LogError($"{operation} failed: {failedTask.Exception?.GetBaseException().Message}", "TwitchBot"), TaskContinuationOptions.OnlyOnFaulted);

        private void JoinChannel(string channel)
        {
            try
            {
                Observe(client.JoinChannelAsync(channel), $"Joining {channel}");
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Unable to join {channel}: {ex.Message}", "TwitchBot");
            }
        }

        private void Client_OnJoinedChannel(OnJoinedChannelArgs e)
        {
            LogUtil.LogInfo($"Joined {e.Channel}", e.BotUsername);
            SendMessage(client, e.Channel, "Connected!");
        }

        private void Client_OnMessageReceived(OnMessageReceivedArgs e)
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - Received message: @{e.ChatMessage.Username}: {e.ChatMessage.Message}");
            if (client.JoinedChannels.Count == 0)
                JoinChannel(e.ChatMessage.Channel);
        }

        private void Client_OnLeftChannel(OnLeftChannelArgs e)
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - Left channel {e.Channel}");
            JoinChannel(e.Channel);
        }

        private void Client_OnChatCommandReceived(OnChatCommandReceivedArgs e)
        {
            if (!Settings.AllowCommandsViaChannel || Settings.UserBlacklist.Contains(e.ChatMessage.Username))
                return;

            var msg = e.ChatMessage;
            var c = e.Command.Name.ToLower();
            var args = e.Command.ArgumentsAsString;
            var response = HandleCommand(msg, c, args, false, out var dest);
            if (response.Length == 0)
                return;

            if (dest == TwitchMessageDestination.Whisper)
                LogWhisperUnsupported(msg.Username);
            else
                SendMessage(client, msg.Channel, response);
        }

        private void Client_OnWhisperCommandReceived(OnWhisperCommandReceivedArgs e)
        {
            if (!Settings.AllowCommandsViaWhisper || Settings.UserBlacklist.Contains(e.WhisperMessage.Username))
                return;

            var msg = e.WhisperMessage;
            var c = e.Command.Name.ToLower();
            var args = e.Command.ArgumentsAsString;
            var response = HandleCommand(msg, c, args, true, out _);
            if (response.Length == 0)
                return;

            LogWhisperUnsupported(msg.Username);
        }

        private string HandleCommand(TwitchLibMessage m, string c, string args, bool whisper, out TwitchMessageDestination dest)
        {
            bool sudo() => m is ChatMessage ch && (ch.IsBroadcaster || Settings.IsSudo(m.Username));
            bool subscriber() => m is ChatMessage { UserDetail.IsSubscriber: true };
            LogUtil.LogInfo($"[Command] {m.Username}: {c} {args}", nameof(TwitchCrossBot));

            dest = TwitchMessageDestination.Disabled; // disable overwrite so responds in same area.

            // user-defined
            if (Settings.UserDefinedSubOnlyCommands.ContainsKey(c.ToLower()))
            {
                if (subscriber())
                {
                    dest = Settings.UserDefinedSubOnlyCommandsDestination;
                    return ReplacePredefined(Settings.UserDefinedSubOnlyCommands[c], m.Username);
                }
                else
                    return $"@{m.Username} - You must be a subscriber to use this command.";
            }
            if (Settings.UserDefinitedCommands.ContainsKey(c.ToLower()))
            {
                dest = Settings.UserDefinedCommandsDestination;
                return ReplacePredefined(Settings.UserDefinitedCommands[c], m.Username);
            }

            switch (c)
            {
                // User Usable Commands
                case "order":
                    var _ = TwitchHelper.AddToWaitingList(args, m.DisplayName, m.Username, ulong.Parse(m.UserId), subscriber(), false, out string msg);
                    return msg;
                case "ordercat":
                    var _1 = TwitchHelper.AddToWaitingList(args, m.DisplayName, m.Username, ulong.Parse(m.UserId), subscriber(), true, out string msg1);
                    return msg1;
                case "preset":
                    var _2 = TwitchHelper.AddToWaitingListPreset(args, m.DisplayName, m.Username, ulong.Parse(m.UserId), subscriber(), out string msg2);
                    return msg2;
                case "presets":
                    return TwitchHelper.GetPresets(Settings.CommandPrefix);
                case "drop":
                    return $"@{m.Username}: {TwitchHelper.Drop(args, ulong.Parse(m.UserId), m.Username, Settings)}";
                case "clean":
                    return $"@{m.Username}: {TwitchHelper.Clean(ulong.Parse(m.UserId), m.Username, Settings)}";
                case "ts":
                case "pos":
                case "position":
                case "time":
                case "eta":
                    return $"@{m.Username}: {TwitchHelper.GetPosition(ulong.Parse(m.UserId))}";
                case "tc":
                case "remove":
                case "delete":
                case "qc":
                    return $"@{m.Username}: {TwitchHelper.ClearTrade(ulong.Parse(m.UserId))}";
                case "ping":
                    return $"@{m.Username}: pong!";

                // Sudo Only Commands
                case "toggledrop" when !sudo():
                case "tcu" when !sudo():
                    return "This command is locked for sudo users only!";

                case "tcu":
                    return TwitchHelper.ClearTrade(args);
                case "toggledrop":
                    Settings.AllowDropViaTwitchChat = !Settings.AllowDropViaTwitchChat;
                    return Settings.AllowDropViaTwitchChat ? "I am now accepting drop commands!" : "I am no longer accepting drop commands!";

                default: return string.Empty;
            }
        }

        private bool AddToTradeQueue(TwitchQueue queueItem, string pass, out string msg)
        {
            if (int.TryParse(pass, out var ps))
            {
                var twitchRequest = new TwitchOrderRequest<Item>(queueItem.ItemReq.ToArray(), queueItem.ID, QueueExtensions.GetNextID(), queueItem.DisplayName, queueItem.DisplayName, client, Channel, Settings, ps, queueItem.VillagerReq);
                var result = QueueExtensions.AddToQueueSync(twitchRequest, queueItem.DisplayName, queueItem.DisplayName, out var msge);
                msg = TwitchOrderRequest<Item>.SanitizeForTwitch(msge);
                return result;
            }

            msg = $"@{queueItem.DisplayName} - Your 3-digit number was invalid. Order has been removed, please start over.";
            return false;
        }

        private void Client_OnWhisperReceived(OnWhisperReceivedArgs e)
        {
            LogUtil.LogInfo($"[{client.TwitchUsername}] - @{e.WhisperMessage.Username}: {e.WhisperMessage.Message}", nameof(TwitchCrossBot));
            if (QueuePool.Count > 100)
            {
                var removed = QueuePool[0];
                QueuePool.RemoveAt(0); // First in, first out
                SendMessage(client, Channel, $"Removed @{removed.DisplayName} from the waiting list: stale request.");
            }

            var queueItem = QueuePool.FindLast(q => q.ID == ulong.Parse(e.WhisperMessage.UserId));
            if (queueItem == null)
            {
                LogUtil.LogInfo($"No queue item found, returning...", nameof(TwitchCrossBot));
                return;
            }
            QueuePool.Remove(queueItem);
            var msg = e.WhisperMessage.Message;
            try
            {
                var _ = AddToTradeQueue(queueItem, msg, out string message);
                SendMessage(client, Channel, message);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogUtil.LogError($"{ex.Message}", nameof(TwitchCrossBot));
            }
        }

        private static string ReplacePredefined(string message, string caller)
        {
            return message.Replace("{islandname}", Bot.TownName)
                .Replace("{dodo}", Bot.DodoCode)
                .Replace("{vcount}", (Bot.VisitorList.VisitorCount > 0 ? Bot.VisitorList.VisitorCount - 1 : 0).ToString()) // -1 for the host
                .Replace("{visitorlist}", Bot.VisitorList.VisitorFormattedString)
                .Replace("{villagerlist}", Bot.Villagers.LastVillagers)
                .Replace("{user}", caller);
        }
    }
}
