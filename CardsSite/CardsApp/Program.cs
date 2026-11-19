using MiniHttpServer.Framework.Configuration;

namespace CardsApp;

class Program
{
    static async Task Main(string[] args)
    {
        var sessionService = new UserSessionService();

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") is { Length: > 0 } fromEnv
            ? fromEnv
            : ServerSettings.Instance.ConnectionString;
        AppServices.Initialize(connectionString, sessionService);
        
        await ServerService.StartServer(true, sessionService);
    }
}
