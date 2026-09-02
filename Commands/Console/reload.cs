using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Log = NebulaBot.API.Log;

namespace NebulaBot.Commands;

public static class Reload
{
    public static async Task Execute()
    {
        // Register interaction modules
        await WebSocket._interactionService.AddModulesAsync(typeof(WebSocket).Assembly, null);

        foreach (var comm in WebSocket._interactionService.Modules)
        {
            Log.Debug($"Registering Command: {comm.Name}");
        }

        await WebSocket._interactionService.RegisterCommandsGloballyAsync();

        Log.Debug("Reload complete.");
    }
}
