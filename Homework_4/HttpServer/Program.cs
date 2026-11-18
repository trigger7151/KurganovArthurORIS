namespace HttpServer;
using System.Net;
using System.Text.Json;
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Routing;


public static class ServerService
{
    private static Server? Server{get;set;}
    private static bool IsStarted{get;set;}
    private static bool ListenConsole{get;set;}
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
    private static void Build(bool listenConsole, IUserSessionService sessionService)
    {
        ListenConsole = true;
        var pipeline = GetPipeline(sessionService);
        RouteTable.MapRoutes();
        Server = new Server(ServerSettings.Instance, pipeline);
    }
    private static IHandler GetPipeline(IUserSessionService sessionService)
    {
        var pipeline = new ExceptionHandler();
        pipeline.SetNext(new StaticHandler())       
        .SetNext(new RouteHandler(sessionService))        
        .SetNext(new NotFoundHandler());    
        return pipeline;
    }
}
public class ConsoleListener
{
    private Server? Server{get;}
    public ConsoleListener(Server server)
    {
        Server = server;
    }
    public async Task ListenConsoleLoopAsync()
    {
        Console.WriteLine("Команды: start | stop | restart | exit");
        while (true)
        {
            var userInput = Console.ReadLine();
            if (userInput is null)
            {
                break;
            }
            // Переводим в нижний регистр, удаляем пробелы с начала и конца. Ждем пока 
            // выполнится команда и не принимаем текст с консоли
            bool keepGoing = await ExecuteCommandAsync(userInput.Trim().ToLowerInvariant());
            if (!keepGoing) break;
        }

        await Server.StopAsync();
    }
    private async Task<bool> ExecuteCommandAsync(string command)
    {
        switch (command)
        {
            case "start":
                Server.Start();
                return true;

            case "stop":
                await Server.StopAsync();
                return true;

            case "restart":
                await Server.StopAsync();
                Server.Start();
                return true;

            case "exit":
            case "quit":
                return false;   

            case "":
                return true;

            default:
                Console.WriteLine("Неизвестная команда. Доступно: start | stop | restart | exit");
                return true;
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var sessionService = new UserSessionService();
        await ServerService.StartServer(true, sessionService);
    }
}
