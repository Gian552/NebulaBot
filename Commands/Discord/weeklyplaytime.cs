using Discord;
using Discord.Interactions;
using NebulaBot.API;
using NebulaBot.Database;

namespace NebulaBot.Commands
{
    public class Weeklyplaytime : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("weekly-playtime", "Zeigt deine wöchentliche Spielzeit auf dem SL Server an.")]
        public async Task Execute(string? steamID = null)
        {
            Database.API.PlayerData? _userStats;

            if (!string.IsNullOrEmpty(steamID))
            {
                if (!steamID.Contains("@steam"))
                {
                    steamID = steamID + "@steam";
                }

                _userStats = await Database.API.GetUserDataBySteamID(steamID);

                if (_userStats == null)
                {
                    await RespondAsync("Keine Daten für diesen User gefunden.", ephemeral: true);
                    return;
                }
            }
            else
            {
                _userStats = await Context.User.GetUserData();

                if (_userStats == null)
                {
                    await RespondAsync("Keine Daten für diesen User gefunden, du bist vermutlich noch nicht verifiziert, bitte führe /verify aus um diesen Command benutzen zu können.", ephemeral: true);
                    return;
                }

                steamID = _userStats.Id;
            }

            if (!_userStats.Verified)
            {
                await RespondAsync("Du bist noch nicht verifiziert, bitte führe /verify aus um diesen Command benutzen zu können.", ephemeral: true);
                return;
            }
            
            var ts = TimeSpan.FromSeconds((long?)_userStats.Playtime - _userStats.WeekStart ?? 0);
            string pt = String.Format("{0} Stunden, {1} Minuten, {2} Sekunden", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            
            SteamAPI.SteamUserData? SteamUserData = await SteamAPI.RequestSteamUser(steamID);

            if (SteamUserData == null)
            {
                await RespondAsync("Steam User nicht gefunden.", ephemeral: true);
                return;
            }

            var _footer = new EmbedFooterBuilder()
                .WithText($"Made By MisterT13 • {DateTime.Now}")
                .WithIconUrl("https://cdn.discordapp.com/avatars/931827927945994280/874fbf05b74275746245fe9c13e210b6.png");

            var _embed = new EmbedBuilder()
                .WithTitle($"🎮Spielzeit von {SteamUserData.personaname}")
                .WithColor(new Color(88, 101, 242))
                .WithAuthor($"@{SteamUserData.personaname}",$"{SteamUserData.avatarfull}")
                .WithThumbnailUrl(SteamUserData.avatarfull)
                .WithDescription($"**Wochenpielzeit**:\n{pt}\n\n[Steam Profil]({SteamUserData.profileurl})")
                .WithFooter(_footer);

            await RespondAsync(embed: _embed.Build());
        }
    
    }
}
