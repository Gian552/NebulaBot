using Discord;
using System.Text.Json;

namespace NebulaBot.API
{
    internal static class NorthwoodAPI
    {
        private static HttpClient _client = new();
        internal static CancellationTokenSource _cr = new();
        private static bool _IsListUpdateRunning = false;

        public static ServerInfo? SLServerInfo { get; private set; }
        public static string PlayerListString => string.Join("\n", SLServerInfo?.PlayersList?.Select(p => p.Nickname) ?? Enumerable.Empty<string>());

        internal static async Task UpdatePlayerList()
        {
            if (_IsListUpdateRunning)
                return;
            
            _IsListUpdateRunning = true;

            while (!_cr.IsCancellationRequested)
            {
                try
                {
                    string response = await _client.GetStringAsync($"https://api.scpslgame.com/serverinfo.php?id={Config.Instance.SLAccountID}&key={Config.Instance.SLAPIToken}&players=true&list=true&nicknames=true&online=true");

                    var responseObject = JsonSerializer.Deserialize<ApiResponse>(response);
                    SLServerInfo = responseObject?.Servers.FirstOrDefault(s => s.Port == 7777);

                    if (responseObject != null && responseObject.Success && responseObject.Servers.Length > 0)
                    {
                        var server = responseObject.Servers.FirstOrDefault(s => s.Port == 7777);

                        if (server == null)
                            throw new Exception("No Matching Server Found for !!");

                        if (!server.Online)
                        {
                            await WebSocket._client.SetActivityAsync(new Game($"Offline", ActivityType.Playing));
                            await WebSocket._client.SetStatusAsync(UserStatus.DoNotDisturb);

                            await Task.Delay(1000 * responseObject.Cooldown + 10000, _cr.Token);
                        }
                        else
                        {
                            await WebSocket._client.SetActivityAsync(new Game($"{server.Players} Online", ActivityType.Playing));

                            if (server.Players.StartsWith("0/"))
                            {
                                await WebSocket._client.SetStatusAsync(UserStatus.Idle);
                            }
                            else
                            {
                                await WebSocket._client.SetStatusAsync(UserStatus.Online);
                            }

                            await Task.Delay(1000 * responseObject.Cooldown + 10000, _cr.Token);
                        }
                    }
                    else
                    {
                        Log.Warn("No servers found or API returned an error.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Fehler beim Abrufen der Spielerliste: {ex.Message}");
                }

                await Task.Delay(30000, _cr.Token);
            }
            _IsListUpdateRunning = false;
        }

        // Classes for JSON deserialization
        private class ApiResponse
        {
            public bool Success { get; set; }
            public int Cooldown { get; set; }
            public ServerInfo[] Servers { get; set; }
        }

        public class ServerInfo
        {
            public int ID { get; set; }
            public int Port { get; set; }
            public bool Online { get; set; } // optional in JSON example
            public string Players { get; set; } // Example format: "0/20"
            public PlayerEntry[] PlayersList { get; set; } = Array.Empty<PlayerEntry>();
        }

        public class PlayerEntry
        {
            public string ID { get; set; }
            public string Nickname { get; set; }
        }
    }
}