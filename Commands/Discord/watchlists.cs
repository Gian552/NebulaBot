using Discord;
using Discord.Interactions;
using NebulaBot.API;

namespace NebulaBot.Commands
{
    public class Watchlist : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("watchlist", "Zeigt Alle Watchlisteinträge eins Spielers an.")]
        public async Task Execute(string steamID)
        {
            var _userStats = await Database.API.GetUserDataBySteamID(steamID);

            if (!WebSocket.restClient.GetGuildUserAsync(Context.Guild.Id, Context.Interaction.User.Id).Result.RoleIds.IsTeamler())
            {
                await RespondAsync("Du hast keine Berechtigung, diesen Befehl zu verwenden.", ephemeral: true);
                return;
            }

            if (_userStats == null)
            {
                await RespondAsync("Dieser Spieler ist nicht in der Datenbank vorhanden.");
                return;
            }

            if (_userStats.Watchlists.Count == 0)
            {
                await RespondAsync("Dieser Spieler hat keine Watchlisteinträge.");
                return;
            }

            var _footer = new EmbedFooterBuilder()
                .WithText($"Made By @skorp1.0 • {DateTime.Now}")
                .WithIconUrl($"{WebSocket.OwnerAvatarUrl}");

            var _embed = new EmbedBuilder()
                .WithColor(new Color(45, 45, 255))
                .WithAuthor($"@{Context.User.GlobalName}")
                .WithFooter(_footer);

            foreach (var warn in _userStats.Watchlists)
            {
                _embed.AddField($"Warn ID: {warn.Id}", $"**Grund:** {warn.Reason}\n**Ausgestellt von:** <@{warn.Issuer}>\n**Datum:** {warn.CreatedAt.ToString("dd.MM.yyyy HH:mm")}");
            }

            await RespondAsync(embed: _embed.Build());
        }
    }
}