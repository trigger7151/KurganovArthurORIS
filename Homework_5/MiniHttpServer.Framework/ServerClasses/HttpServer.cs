using System;
using System.Net;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Handlers;
namespace MiniHttpServer.Framework.ServerClasses;

/// <summary>
/// HTTP-сервер на основе <see cref="HttpListener"/>: принимает запросы и передаёт каждый
/// в цепочку обработчиков. Запросы обрабатываются параллельно.
/// </summary>
public class Server
{
    /// <summary>
    /// Текущий прослушиватель; null, если сервер остановлен.
    /// </summary>
    private HttpListener? Listener { get; set; }
    /// <summary>
    /// Источник токена отмены для текущего запуска.
    /// </summary>
    private CancellationTokenSource? TokenSource { get; set; }
    /// <summary>
    /// Задача цикла приёма запросов.
    /// </summary>
    private Task? ListenTask { get; set; }
    /// <summary>
    /// Прослушиваемые адреса.
    /// </summary>
    private string[] Prefixes { get; }
    /// <summary>
    /// Путь к странице, которая отдаётся по адресу <c>/</c>.
    /// </summary>
    public string ResponsePath { get; }
    /// <summary>
    /// Первый обработчик цепочки.
    /// </summary>
    private IHandler Pipeline{get;}

    /// <summary>
    /// Запущен ли сервер.
    /// </summary>
    public bool IsRunning => ListenTask is { IsCompleted: false };
    /// <summary>
    /// Создаёт сервер.
    /// </summary>
    /// <param name="settings">Настройки: адреса и путь к главной странице.</param>
    /// <param name="pipeline">Первый обработчик цепочки.</param>
    public Server(ServerSettings settings, IHandler pipeline)
    {
        Prefixes = settings.Prefixes;
        ResponsePath = settings.ResponsePath;
        Pipeline = pipeline;
    }
    /// <summary>
    /// Начинает прослушивание адресов и приём запросов. Повторный вызов у запущенного сервера ничего не делает.
    /// </summary>
    /// <exception cref="Exception">Не удалось начать прослушивание (например, адрес занят).</exception>
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

    /// <summary>
    /// Останавливает приём запросов и освобождает ресурсы.
    /// </summary>
    /// <returns>Задача, завершающаяся после остановки.</returns>
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

    /// <summary>
    /// Цикл приёма запросов до отмены токена; каждый запрос обрабатывается без ожидания предыдущих.
    /// </summary>
    /// <param name="listener">Прослушиватель.</param>
    /// <param name="token">Токен отмены.</param>
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
    /// <summary>
    /// Передаёт запрос в цепочку обработчиков и в любом случае закрывает ответ.
    /// Это единственное место, где закрывается ответ.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <param name="server">Сервер.</param>
    /// <param name="token">Токен отмены.</param>
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
