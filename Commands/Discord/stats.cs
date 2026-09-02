using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MongoDB.Driver;
using NebulaBot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace NebulaBot.Commands
{
    public class stats : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("stats", "Zeigt Statistiken vom SL Server an.")]
        public async Task Execute()
        {
            var _userStats = await Context.User.GetUserData();

            if (_userStats == null || !_userStats.Verified)
            {
                await RespondAsync("Du bist noch nicht verifiziert, bitte führe /verify aus um diesen Command benutzen zu können.", ephemeral: true);
                return;
            }
            var ts = TimeSpan.FromSeconds((long?)_userStats.Playtime ?? 0);

            string pt = String.Format("{0} Stunden, {1} Minuten, {2} Sekunden", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            float kd = (float)MathF.Round((float)_userStats.Kills / _userStats.Deaths,2);


            var _killsField = new EmbedFieldBuilder()
                .WithName("Kills")
                .WithValue($"{_userStats.Kills}");

            var _deathsField = new EmbedFieldBuilder()
                .WithName("Tode")
                .WithValue($"{_userStats.Deaths}");

            var _currentXpField = new EmbedFieldBuilder()
                .WithName("Aktuelle XP")
                .WithValue($"{_userStats.XP}");

            var _levelField = new EmbedFieldBuilder()
                .WithName("Level")
                .WithValue($"{_userStats.Level}");

            var _playtimeField = new EmbedFieldBuilder()
                .WithName("Spielzeit")
                .WithValue($"{pt}");

            var _killDeathRatio = new EmbedFieldBuilder()
                .WithName("Kill-Death ratio (KD)")
                .WithValue($"{kd}");

            var _footer = new EmbedFooterBuilder()
                .WithText($"Made By @skorp1.0 • {DateTime.Now}")
                .WithIconUrl($"{WebSocket.OwnerAvatarUrl}");

            var _embed = new EmbedBuilder()
                .WithColor(new Color(45, 45, 255))
                .WithAuthor($"@{Context.User.GlobalName}")
                .WithImageUrl(Context.User.GetDisplayAvatarUrl())
                .WithFields(_killsField, _deathsField, _killDeathRatio, _currentXpField, _levelField, _playtimeField)
                .WithFooter(_footer);

            await RespondAsync(embed: _embed.Build());
        }
    }
}