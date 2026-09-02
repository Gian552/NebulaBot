using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NebulaBot.API;
using NebulaBot.Commands;
using System;
using Log = NebulaBot.API.Log;

namespace NebulaBot
{
    public static class WebSocket
    {
        internal static DiscordSocketClient _client;
        internal static InteractionService _interactionService;
        public static Discord.Rest.DiscordRestClient restClient;
        public static string OwnerAvatarUrl;

        public static DiscordConfig clientConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200,
            LogLevel = LogSeverity.Debug,
        };

        public static async Task Main()
        {
            string bottoken = Config.Instance.BotToken;
            Log.Debug("Token Loaded...");
            await Task.Delay(100);

            var _clientConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 200,
                LogLevel = Config.Instance.LogLevel,
            };

            _client = new DiscordSocketClient(_clientConfig);
            _interactionService = new InteractionService(_client);

            _client.Log += async (msg) =>
            {
                switch (msg.Severity)
                {
                    case LogSeverity.Critical:
                        Log.Fatal(msg.Message, msg.Exception?.ToString() ?? "No exception");
                        break;
                    case LogSeverity.Error:
                        Log.Error(msg.Message);
                        break;

                    case LogSeverity.Warning:
                        Log.Warn(msg.Message);
                        break;

                    case LogSeverity.Info:
                        Log.Info(msg.Message);
                        break;

                    case LogSeverity.Verbose:
                        Log.Verbose(msg.Message);
                        break;
                    case LogSeverity.Debug:
                        Log.Debug(msg.Message);
                        break;
                }
            };

            _client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, null);
            };

            Log.Debug("Logging in...");
            await _client.LoginAsync(TokenType.Bot, bottoken);
            await _client.StartAsync();

            _client.Ready += async () =>
            {
                Log.Debug("Bot is ready! Setting presence, reggistering commands and loading DB...");

                // Load DB
                Database.API.InitDB();
                //Task.Run(() => Database.API.VerifiedUpdate(_client.GetGuild(1357113312743260160)));
                Task.Run(() => Database.API.UpdateRanks(_client.GetGuild(1357113312743260160)));

                // Set presence
                Task.Run(() => NorthwoodAPI.UpdatePlayerList());

                // Reggister commands
                Reload.Execute();
            };

            restClient = new();
            restClient.LoginAsync(TokenType.Bot, bottoken).Wait();

            restClient.Log += async (msg) =>
            {
                switch (msg.Severity)
                {
                    case LogSeverity.Critical:
                        Log.Fatal(msg.Message, msg.Exception?.ToString() ?? "No exception");
                        break;
                    case LogSeverity.Error:
                        Log.Error(msg.Message);
                        break;

                    case LogSeverity.Warning:
                        Log.Warn(msg.Message);
                        break;

                    case LogSeverity.Info:
                        Log.Info(msg.Message);
                        break;

                    case LogSeverity.Verbose:
                        Log.Verbose(msg.Message);
                        break;
                    case LogSeverity.Debug:
                        Log.Debug(msg.Message);
                        break;
                }
            };

            restClient.LoggedIn += async () =>
            {
                OwnerAvatarUrl = restClient.GetUserAsync(504875989776596992).Result.GetAvatarUrl();
            };

            _ = Task.Run(() => HandleConsoleInput());

            await Task.Delay(-1);
        }

        // This method runs on a background thread and processes console commands.
        private static async Task HandleConsoleInput()
        {
            while (true)
            {
                string? input = Console.ReadLine();

                if (!string.IsNullOrEmpty(input))
                {
                    Log.Command($">>> {input}");
                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        Exit.Execute();
                    }
                    else if (input.Equals("reload", StringComparison.OrdinalIgnoreCase))
                    {
                        //Reload.Execute();
                        Log.Info("Reload no worki :3 \n sorry, pls restart");
                    }
                    else
                    {
                        Log.Command($"Command {input} does not exist!");
                    }
                }

                await Task.Delay(100);
            }
        }
    }
}