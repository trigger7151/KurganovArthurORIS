namespace Search_server;
using System.Net;
using System.Text.Json;
using JsonClasses;
using HttpServer.Settings;
using MimeTypesSpace;
using HttpServer.Handlers;

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
public class Server
{
    private HttpListener? Listener { get; set; }
    private CancellationTokenSource? TokenSource { get; set; }
    private Task? ListenTask { get; set; }
    private string[] Prefixes { get; }
    public string ResponsePath { get; }
    public RouteHandler RouteHandler{get;}
    private StaticHandler StaticHandler{get;set;}

    public bool IsRunning => ListenTask is { IsCompleted: false };

    public Server(ServerSettings settings)
    {
        Prefixes = settings.Prefixes;
        ResponsePath = settings.ResponsePath;
        StaticHandler = new StaticHandler();
        RouteHandler = new RouteHandler();
        StaticHandler.SetNext(RouteHandler);
        RouteHandler.MapRoutes();
    }
    public void Start()
    {
        if (IsRunning)
        {
            Console.WriteLine("Сервер уже запущен.");
            return;
        }

        
        var listener = new HttpListener();
        foreach (var prefix in Prefixes)
        listener.Prefixes.Add(prefix);
        try
        {
            listener.Start(); 
        }
        catch (Exception ex)
        {
            listener.Close();
            throw new Exception($"Не удалось запустить сервер: {ex.Message}");
            //Console.WriteLine($"Не удалось запустить сервер: {ex.Message}");
            //return;
        }
        // для каждого объекта listener нужен свой токен отмены
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        Listener = listener;
        TokenSource = cts;
        ListenTask = ListenLoopAsync(listener, token);
        Console.WriteLine($"Сервер запущен: {string.Join(", ", Prefixes)}");
    }

    public async Task StopAsync()
    {
        var listener = Listener;
        var task = ListenTask;
        var cts = TokenSource;

        if (listener is null || task is null || cts is null)
        {
            Console.WriteLine("Сервер не запущен.");
            return;
        }

        cts.Cancel();   

        try { await task; }
        catch (Exception ex) { Console.WriteLine($"Остановка: {ex.Message}"); }

        if (listener.IsListening) listener.Stop();
        listener.Close();

        cts.Dispose();

        Listener = null;
        TokenSource = null;
        ListenTask = null;

        Console.WriteLine("Сервер остановлен.");
    }

    private async Task ListenLoopAsync(HttpListener listener, CancellationToken token)
    {
        using var registration = token.Register(() =>
        {
            if (listener.IsListening) listener.Stop();
        });
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                break;   
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка приёма запроса: {ex.Message}");
                continue;
            }

            _ = CallHandlerChain(context, this, token);   
        }
    }
    private async Task CallHandlerChain(HttpListenerContext context, Server server, CancellationToken token)
    {
        try
        {
            await StaticHandler.HandleAsync(context, this, token);
        }
        finally
        {
            context.Response.Close();   
        
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var server = new Server(ServerSettings.Instance);
        var consoleListener = new ConsoleListener(server);
        server.Start();
        await consoleListener.ListenConsoleLoopAsync();
    }
}
