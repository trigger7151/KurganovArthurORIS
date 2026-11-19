namespace HttpServer;
class Program
{
    static async Task Main(string[] args)
    {
        var sessionService = new UserSessionService();
        await ServerService.StartServer(true, sessionService);
    }
}
