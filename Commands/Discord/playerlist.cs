using Discord;
using Discord.Interactions;
using NebulaBot.API;

namespace NebulaBot.Commands
{
    public class Playerlist : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("playerlist", "Zeigt die aktuellen Spieler auf dem SL Server an.")]
        public async Task Execute()
        {
            var _Server1Players = new EmbedFieldBuilder()
                .WithName("Server 1:")
                .WithValue(string.IsNullOrWhiteSpace(NorthwoodAPI.PlayerListString) ? "Es sind aktuell keine Spieler auf dem Server." : NorthwoodAPI.PlayerListString);

            var _footer = new EmbedFooterBuilder()
                .WithText($"Made By MisterT13 • {DateTime.Now}")
                .WithIconUrl("https://cdn.discordapp.com/avatars/931827927945994280/33de396776ce7247e528db6f473b19fa.png");

            var _embed = new EmbedBuilder()
                .WithColor(new Color(45, 45, 255))
                .WithAuthor("Spielerliste:")
                .WithFields(_Server1Players)
                .WithFooter(_footer);

            await RespondAsync(embed: _embed.Build());
        }
    }
}