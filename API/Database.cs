using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NebulaBot.API;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using static NebulaBot.API.Roles;

namespace NebulaBot.Database
{
    internal static class API
    {
        private static class PlayerDataCache
        {
            internal static readonly ConcurrentDictionary<string, PlayerData> Data = new();

            public static PlayerData Get(string id)
                => Data.TryGetValue(id, out var result) ? result : null;

            public static void Set(string id, PlayerData playerData)
                => Data[id] = playerData;
        }

        // Player data model
        public class PlayerData
        {
            [BsonId]
            public string Id { get; set; }
            public string DiscordId { get; set; } = null;
            public bool Verified { get; set; } = false;
            public string VerificationToken { get; set; }
            public string Nickname { get; set; }
            public string CustomNick { get; set; }
            public Roles.DiscordRoles? dcRole { get; set; } = null;
            public List<Roles.DiscordRoles> dcRoles { get; set; } = new List<Roles.DiscordRoles>();
            public string slRole { get; set; } = null;
            public bool NicknameChangable { get; set; } = true;
            public List<Warn> Warns { get; set; } = new List<Warn>();
            public List<Warn> Watchlists { get; set; } = new List<Warn>();
            public List<Ban> Bans { get; set; } = new List<Ban>();
            public double? Playtime { get; set; }
            public double? WeekStart { get; set; } = 0;
            public int XP { get; set; }
            public int RequiredXP { get; set; } = 230;
            public int Level { get; set; } = 1;
            public int Kills { get; set; }
            public int Deaths { get; set; }
        }

        //Warn data structure
        public class Warn
        {
            public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
            public DateTime CreatedAt { get; set; }
            public string Reason { get; set; }
            public string Issuer { get; set; }
        }

        //Ban data structure
        public class Ban
        {
            public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string Reason { get; set; }
            public string Issuer { get; set; }
        }

        private static IMongoCollection<PlayerData> _collection;
        internal static bool DbLoaded = false;

        /// Tokens for <see cref="VerifiedUpdate(SocketGuild)">
        private static CancellationTokenSource _vcr = new();
        private static bool VerifyUpdateRunning = false;

        /// Tokens for <see cref="UpdateRanks(SocketGuild)">
        private static CancellationTokenSource _ucr = new();
        private static bool UpdateRanksRunning = false;

        internal static void InitDB()
        {
            if (DbLoaded)
                return;

            string connectionString = $"mongodb://SL_MAIN_NEBULA:{Config.Instance.dbPW}@91.99.148.236:27017/SL_NEBULA?authSource=admin&authMechanism=SCRAM-SHA-256";
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase("SL_NEBULA");
            _collection = db.GetCollection<PlayerData>("players");
            DbLoaded = true;
            Log.Info($"MongoDB connected successfully.");
        }

        internal static void CloseDB()
        {
            _vcr.Cancel();
            _ucr.Cancel();
            _collection = null;
            DbLoaded = false;
        }

        /// <summary>
        /// Asynchronously retrieves the player data associated with the specified Discord user.
        /// </summary>
        /// <remarks>If the user does not have associated player data, the method returns <see
        /// langword="null"/>. If an error occurs during retrieval, the method logs the error and returns <see
        /// langword="null"/>.</remarks>
        /// <param name="user">The Discord user for whom to retrieve player data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the player's data if found;
        /// otherwise, <see langword="null"/>.</returns>
        public static async Task<PlayerData?> GetUserData(this SocketUser user)
        {
            try
            {
                return await _collection.Find(u => u.DiscordId == user.Id.ToString()).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching user data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Asynchronously retrieves player data for the specified Steam ID.
        /// </summary>
        /// <param name="steamID">The unique Steam ID of the user whose player data is to be retrieved. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the player's data if found;
        /// otherwise, null.</returns>
        public static async Task<PlayerData?> GetUserDataBySteamID(string steamID)
        {
            try
            {
                return await _collection.Find(u => u.Id == steamID).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching user data by SteamID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads all MongoDB documents into <see cref="PlayerDataCache.Data">.
        /// </summary>
        private static void LoadAllDataFromDatabase()
        {
            Task.Run(async () =>
            {
                var allPlayers = await _collection.Find(_ => true).ToListAsync();
                foreach (var player in allPlayers)
                {
                    try
                    {
                        PlayerDataCache.Data[player.Id] = player;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Player with wrong atributes, skipping...");
                        continue;
                    }
                }

                Log.Debug($"Loaded {PlayerDataCache.Data.Count} player data entries into memory.");
            }).GetAwaiter().GetResult(); // Block here if you must to ensure data is ready.
        }

        /// <summary>
        /// Generate the Random Token for Verification.
        /// </summary>
        /// <param name="length">The length for the Token.</param>
        /// <returns>The Token.</returns>
        internal static string GenerateVerificationToken(int length = 25)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var data = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            var result = new StringBuilder(length);
            foreach (byte b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }


        /// <summary>
        /// Seting the verified Role for every verified user.
        /// </summary>
        /// <param name="guild">The <see cref="SocketGuild"/> to go through.</param>
        /// <returns>The Task for this operation</returns>
        internal static async Task VerifiedUpdate(SocketGuild guild)
        {
            if (VerifyUpdateRunning)
                return;

            VerifyUpdateRunning = true;

            await Task.Delay(10000, _vcr.Token);

            while (!_vcr.IsCancellationRequested)
            {
                LoadAllDataFromDatabase();
                var datacache = PlayerDataCache.Data;

                foreach (var user in datacache.Values)
                {
                    if (!user.Verified)
                        continue;

                    if (string.IsNullOrWhiteSpace(user.DiscordId))
                        continue;

                    string idDigits = new string(user.DiscordId.Where(char.IsDigit).ToArray());

                    if (!ulong.TryParse(idDigits, out ulong dcid))
                    {
                        Log.Info("Skipping user (couldn't parse Discord ID)");
                        continue;
                    }

                    var dcuser = await WebSocket._client.Rest.GetGuildUserAsync(guild.Id, dcid);

                    var veriRole = guild.Roles.FirstOrDefault(r => r.Id == (ulong)Roles.DiscordRoles.Verified);
                    if (veriRole == null)
                    {
                        Log.Warn("Verification role not found.");
                        break; // Stop loop; no point continuing if role doesn't exist
                    }

                    if (dcuser.RoleIds.Contains(veriRole.Id))
                        continue;

                    Log.Info($"Adding Role to {dcuser.Username}");
                    await dcuser.AddRoleAsync(veriRole);
                }
                datacache = null;

                Log.Debug("Verification update cycle completed. Waiting 60 seconds before next cycle.");

                await Task.Delay(17000, _vcr.Token);
            }
            VerifyUpdateRunning = false;
        }

        /// <summary>
        /// Update Roles for SCP:SL server.
        /// </summary>
        /// <param name="guild">The <see cref="SocketGuild"/> to go through.</param>
        /// <returns>The Task for this operation</returns>
        internal static async Task UpdateRanks(SocketGuild guild)
        {
            if (UpdateRanksRunning)
                return;
            UpdateRanksRunning = true;

            await Task.Delay(10000, _ucr.Token);

            while (!_ucr.IsCancellationRequested)
            {
                LoadAllDataFromDatabase();
                var datacache = PlayerDataCache.Data;

                foreach (PlayerData user in datacache.Values)
                {
                    if (!user.Verified)
                        continue;

                    Log.Debug($"Verified player found: {user.Nickname}");

                    string idDigits = new string(user.DiscordId.Where(char.IsDigit).ToArray());

                    if (!ulong.TryParse(idDigits, out ulong dcid))
                    {
                        Log.Debug("Skipping user (couldn't parse Discord ID)");
                        continue;
                    }

                    try
                    {
                        var dcuser = guild.GetUser(dcid);

                        if (dcuser == null)
                        {
                            Log.Debug($"Skipping user (couldn't find Discord user with ID {dcid})");
                            continue;
                        }

                        if (dcuser.Roles.Select(r => r.Id).Any(id => Enum.IsDefined(typeof(DiscordRoles), id)))
                        {
                            var matchedRoles = dcuser.Roles.Select(r => r.Id)
                                .Where(id => Enum.IsDefined(typeof(DiscordRoles), id))
                                .Select(id => (DiscordRoles)id)
                                .ToList();

                            Log.Debug($"Matched Roles for {dcuser.Username}: {string.Join(", ", matchedRoles)}");
                            Log.Debug($"All roles of the User: {dcuser.Roles.Select(r => r.Id)}");

                            if (matchedRoles.Contains(DiscordRoles.Verified))
                                matchedRoles.Remove(DiscordRoles.Verified);

                            user.dcRoles = matchedRoles;

                            var filter = Builders<PlayerData>.Filter.Eq(p => p.Id, user.Id);

                            var update = Builders<PlayerData>.Update
                                .Set(p => p.dcRoles, user.dcRoles);

                            await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                datacache = null;

                Log.Debug("Ranks update cycle completed. Waiting 60 seconds before next cycle.");

                await Task.Delay(180000, _ucr.Token);
            }
            UpdateRanksRunning = false;
        }

        internal static async Task RolePickUpdaterAsync(PlayerData user)
        {
            var filter = Builders<PlayerData>.Filter.Eq(p => p.Id, user.Id);

            var update = Builders<PlayerData>.Update
                .Set(p => p.dcRole, user.dcRole);

            await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }

        /// <summary>
        /// Linking a Discord Account to a SteamID64
        /// </summary>
        /// <param name="user">The <see cref="SocketGuildUser"/>, which executed and the account will be linked to.</param>
        /// <param name="steamId">The SteamID the user provided.</param>
        /// <returns>An <see cref="int"> "status" and a nullable <see cref="string"/> "message", which indicate the status of the operation and "message" returns the actuall verification Token on success.</returns>
        internal static async Task<(int status, string? message)> InitLink(SocketGuildUser user, string steamId)
        {
            var player = await _collection.Find(u => u.Id == steamId).FirstOrDefaultAsync();
            Log.Warn($"given steam: {steamId}\ngiven discord{user.Id}");

            if (player == null)
                return (1, null); // Error 1: "Not in DB" (prob. wrong SteamID)

            if (!player.Verified)
            {
                if (!string.IsNullOrEmpty(player.VerificationToken))
                    return (3, null); // Error 3: User already started verification

                if (!string.IsNullOrEmpty(player.DiscordId))
                {
                    player.VerificationToken = GenerateVerificationToken();
                    var _filter = Builders<PlayerData>.Filter.Eq(p => p.Id, player.Id);
                    await _collection.ReplaceOneAsync(_filter, player);

                    return (4, player.VerificationToken); // Error 4: User migrated from Old DB with not verified link
                }

                player.DiscordId = user.Id.ToString();
                player.VerificationToken = GenerateVerificationToken();

                var filter = Builders<PlayerData>.Filter.Eq(p => p.Id, player.Id);
                await _collection.ReplaceOneAsync(filter, player);
                return (0, player.VerificationToken); // Status 0: Success
            }
            else if (player.Verified)
                return (2, null); // Error 2: Player already verified

            return (-1, null); // Error -1: General Error
        }
    }
}