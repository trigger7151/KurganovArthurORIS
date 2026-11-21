using System;

namespace MiniHttpServer.Framework.ServerClasses;

/// <summary>
/// Собирает и запускает сервер: таблицу маршрутов, цепочку обработчиков и консольное управление.
/// </summary>
public static class ServerService
{
    /// <summary>
    /// Запущенный сервер.
    /// </summary>
    private static Server? Server{get;set;}
    /// <summary>
    /// Был ли сервер уже запущен.
    /// </summary>
    private static bool IsStarted{get;set;}
    /// <summary>
    /// Нужно ли принимать команды из консоли.
    /// </summary>
    private static bool ListenConsole{get;set;}
    /// <summary>
    /// Собирает сервер, запускает его и, если нужно, переходит к приёму команд из консоли.
    /// </summary>
    /// <param name="listenConsole">Принимать ли команды из консоли.</param>
    /// <param name="sessionService">Общий сервис сеансов для проверки доступа.</param>
    /// <returns>Задача, завершающаяся после выхода из консольного управления.</returns>
    /// <exception cref="Exception">Сервер уже запущен.</exception>
    public static async Task StartServer(bool listenConsole, IUserSessionService sessionService)
    {
        if (IsStarted)
        {
            throw new Exception("Server already started");
        }
        Build(listenConsole, sessionService);
        Server.Start();
        IsStarted = true;
        if (ListenConsole)
        {
            var listener = new ConsoleListener(Server);
            await listener.ListenConsoleLoopAsync();
        }
    }
    /// <summary>
    /// Регистрирует маршруты и создаёт сервер с цепочкой обработчиков.
    /// </summary>
    /// <param name="listenConsole">Принимать ли команды из консоли.</param>
    /// <param name="sessionService">Общий сервис сеансов.</param>
    private static void Build(bool listenConsole, IUserSessionService sessionService)
    {
        ListenConsole = true;
        var pipeline = GetPipeline(sessionService);
        RouteTable.MapRoutes();
        Server = new Server(ServerSettings.Instance, pipeline);
    }
    /// <summary>
    /// Собирает цепочку: исключения → статические файлы → маршруты контроллеров → 404.
    /// </summary>
    /// <param name="sessionService">Общий сервис сеансов.</param>
    /// <returns>Первый обработчик цепочки.</returns>
    private static IHandler GetPipeline(IUserSessionService sessionService)
    {
        var pipeline = new ExceptionHandler();
        pipeline.SetNext(new StaticHandler())       
        .SetNext(new RouteHandler(sessionService))        
        .SetNext(new NotFoundHandler());    
        return pipeline;
    }
}
