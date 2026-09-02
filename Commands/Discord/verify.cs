using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace NebulaBot.Commands
{
    public class verification : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("verify", "Verbinde deinen Steam und discord account für den Server.")]
        public async Task Execute(string steamID)
        {
            if (string.IsNullOrEmpty(steamID))
                await RespondAsync("Du musst deine richtige SteamID64 Angeben. Website dafür -> https://steamid.xyz", ephemeral: true);

            if (steamID == null)
            {
                await RespondAsync("Da lief etwas schief, bitte gebt dem Developmemnt Team oder Skorp bescheid!", ephemeral: true);
                return;
            }
            else if (!steamID.Contains("@steam"))
            {
                steamID = steamID + "@steam";
            }

            var Link = Database.API.InitLink((SocketGuildUser)Context.User, steamID).Result;

            if (Link.status == 0)
            {
                await Context.User.SendMessageAsync($"Dein verifikations Token Lautet: {Link.message}\nGib diesen Token mit dem command `.verify <token>` in der Ö-Konsole auf dem Nebula Server ein.");
                await RespondAsync("Dir wurde dein Token via DM geschickt, gib diesen nicht weiter!", ephemeral: true);
                return;
            }
            else if (Link.status == 4)
            {
                await Context.User.SendMessageAsync($"Dein verifikations Token Lautet: {Link.message}\nGib diesen Token mit dem command `.verify <token>` in der Ö-Konsole auf dem Nebula Server ein.\nSollte es einen Fehler geben, prüfe bitte ob du den Token richtig eingegeben hast, wenn es immernoch Problemegibt, höffne bitte ein Support Ticket.");
                await RespondAsync("Dir wurde dein Token via DM geschickt, gib diesen nicht weiter!", ephemeral: true);
                return;
            }
            else if (Link.status == 1)
            {
                await RespondAsync("Du hast eine falsche SteamID angegeben. Bitte überprüfe nochmal deine SteamID, oder Frag das Development Team oder Skorp.", ephemeral: true);
                return;
            }
            else if (Link.status == 2)
            {
                await RespondAsync("Du bist bereits auf dem SL Server verifiziert. Sollte das flasch sein, wende dich bitte an das Development Team oder Skorp!", ephemeral: true);
                return;
            }
            else if (Link.status == 3)
            {
                await RespondAsync("Du hast bereits angefangen dich zu verifizieren, bitte befolge die anweisungen in deinen Direkt Nachrichten.", ephemeral: true);
                return;
            }
            else if (Link.status == -1)
            {
                await RespondAsync("Es gabe einen Fehler beim Ausführen, bitte meldet das dem Development Team oder Skorp.", ephemeral: true);
            }
        }
    }
}