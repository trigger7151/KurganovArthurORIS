namespace HttpServer;
/// <summary>
/// Точка входа приложения.
/// </summary>
class Program
{
    /// <summary>
    /// Создаёт общий сервис сеансов и запускает HTTP-сервер вместе с консольным управлением.
    /// </summary>
    /// <param name="args">Аргументы командной строки (не используются).</param>
    static async Task Main(string[] args)
    {
        var sessionService = new UserSessionService();
        await ServerService.StartServer(true, sessionService);
    }
}
