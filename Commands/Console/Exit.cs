using Log = NebulaBot.API.Log;
using NebulaBot.Database;
using NebulaBot.API;

namespace NebulaBot.Commands;

public static class Exit
{
    public static void Execute()
    {
        Log.Debug("Shutting down...");
        // Closing DB
        Database.API.CloseDB();

        NorthwoodAPI._cr.Cancel();

        // Shut down the Discord client gracefully.
        WebSocket._client?.LogoutAsync().GetAwaiter().GetResult();
        WebSocket._client?.StopAsync().GetAwaiter().GetResult();
        WebSocket.restClient.LogoutAsync().GetAwaiter().GetResult();

        // Exit the application.
        Environment.Exit(0);
    }
}