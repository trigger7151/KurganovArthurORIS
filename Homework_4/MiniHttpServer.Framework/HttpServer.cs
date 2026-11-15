using System;
using System.Net;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Handlers;
namespace MiniHttpServer.Framework;

public class Server
{
    private HttpListener? Listener { get; set; }
    private CancellationTokenSource? TokenSource { get; set; }
    private Task? ListenTask { get; set; }
    private string[] Prefixes { get; }
    public string ResponsePath { get; }
    private IHandler Pipeline{get;}

    public bool IsRunning => ListenTask is { IsCompleted: false };
    public Server(ServerSettings settings, IHandler pipeline)
    {
        Prefixes = settings.Prefixes;
        ResponsePath = settings.ResponsePath;
        Pipeline = pipeline;
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
            await Pipeline.HandleAsync(context, this, token);
        }
        finally
        {
            context.Response.Close();   
        
        }
    }
}
