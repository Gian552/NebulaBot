using Discord;
using Discord.Interactions;
using NebulaBot.Database;

namespace NebulaBot.Commands
{
    public class rolePick : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("sl_rollen", "Suche dir die Rolle aus, die du in SCP:SL haben möchtest.")]
        public async Task Execute()
        {
            var userStats = await Context.User.GetUserData();

            if (userStats == null || !userStats.Verified)
            {
                await RespondAsync("Du bist noch nicht verifiziert, bitte führe /verify aus um diesen Command benutzen zu können.");
                return;
            }
            if (userStats.dcRoles == null || userStats.dcRoles.Count == 0)
            {
                await RespondAsync("Du hast keine Rollen zum Auswählen. Bitte Warte noch ein Stück.", ephemeral: true);
                return;
            }

            var menu = new SelectMenuBuilder()
                .WithCustomId("rolePickMenu")
                .WithPlaceholder("Wähle die Rolle …")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var role in userStats.dcRoles)
                menu.AddOption(role.ToString(), Convert.ToInt64(role).ToString());

            var component = new ComponentBuilder()
                .WithSelectMenu(menu)
                .Build();

            await RespondAsync("Wähle eine Rolle:", components: component, ephemeral: true);
        }
    }

    public class MenuHandlers : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("rolePickMenu")]
        public async Task HandleRolePickMenu(string[] selectedValues)
        {
            string role = selectedValues[0];

            var userStats = await Context.User.GetUserData();
            userStats.dcRole = (API.Roles.DiscordRoles)ulong.Parse(role);

            await Database.API.RolePickUpdaterAsync(userStats);

            await RespondAsync($"Du hast folgende Rolle ausgewählt und sie wird dir beim nächstenmal joinen auf dem Server zugeteilt:\n**{((API.Roles.DiscordRoles)ulong.Parse(role)).ToString()}**", ephemeral: true);
        }
    }
}