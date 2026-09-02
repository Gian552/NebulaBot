using System.Text.Json;

namespace NebulaBot.API
{
    internal class SteamAPI
    {
        private static HttpClient httpClient = new HttpClient();

        public async static Task<SteamUserData?> RequestSteamUser(string steamID)
        {
            if (steamID.Contains("@steam"))
            {
                steamID = steamID.Replace("@steam", "");
            }
            SteamUserData? _SuserData = await RequestSteamAPI(steamID);
            return _SuserData;
        }

        private async static Task<SteamUserData?> RequestSteamAPI(string steamID)
        {
            string? response = await httpClient.GetStringAsync($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={Config.Instance.SteamAPIKey}&steamids={steamID}");
            ApiResponse? responseObject = JsonSerializer.Deserialize<ApiResponse?>(response);
            return responseObject.response.players[0];
        }

        public class ApiResponse
        {
            public Response response { get; set; }
        }

        public class Response
        {
            public SteamUserData[] players { get; set; }
        }

        public class SteamUserData
        {
            public string steamid { get; set; }
            public int communityvisibilitystate { get; set; }
            public int profilestate { get; set; }
            public string personaname { get; set; }
            public int commentpermission { get; set; }
            public string profileurl { get; set; }
            public string avatar { get; set; }
            public string avatarmedium { get; set; }
            public string avatarfull { get; set; }
            public string avatarhash { get; set; }
            public ulong? lastlogoff { get; set; }
            public int personastate { get; set; }
            public string realname { get; set; }
            public string primaryclanid { get; set; }
            public ulong? timecreated { get; set; }
            public int personastateflags { get; set; }
        }
    }
}
