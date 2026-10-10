namespace Search_server;
using System.Net;
using System.Text.Json;
using JsonClasses1;

public class ServerSettings
{

    private static ServerSettings? PrivateInstance{get;set;}
    private static readonly object Lock = new object();
    public string ResponsePath{get;}
    public string[] Prefixes{get;}
    private ServerSettings()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException($"Файл настроек {settingsPath} не найден");
        }
        var settingsObj = DeserializeSettings(settingsPath);
        string responsePath;
        if (settingsObj.responsePathType == "relative")
        {
            responsePath = Path.Combine(AppContext.BaseDirectory, "static/index.html");
        }
        else if (settingsObj.responsePathType == "absolute")
        {
            responsePath = settingsObj.responsePath;
        }
        else
        {
            throw new JsonException("Введены неверные данные в поле responsePathType" +
            " в файле настроек. Возможные варианты: relative, absolute");
        }
        ResponsePath = responsePath;
        Prefixes = settingsObj.prefixes;
    }
    private static Settings DeserializeSettings(string settingsPath)
    {
        var settingsJson = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<Settings>(settingsJson);
    }
    public static ServerSettings Instance 
    {
        get 
        {
            if (PrivateInstance == null) 
            {
                lock (Lock) 
                {
                    if (PrivateInstance == null) 
                    {
                        PrivateInstance = new ServerSettings(); // Здесь вызовется чтение файла
                    }
                }
            }
            return PrivateInstance;
        }
    }
}
public class HttpServer
{
    private HttpListener? Listener { get; set; }
    private CancellationTokenSource? TokenSource { get; set; }
    private Task? ListenTask { get; set; }
    private string[] Prefixes { get; }
    private string ResponsePath { get; }

    public bool IsRunning => ListenTask is { IsCompleted: false };

    public HttpServer(ServerSettings settings)
    {
        Prefixes = settings.Prefixes;
        ResponsePath = settings.ResponsePath;
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
            Console.WriteLine($"Не удалось запустить сервер: {ex.Message}");
            return;
        }

        var cts = new CancellationTokenSource();
        Listener = listener;
        TokenSource = cts;
        ListenTask = ListenLoopAsync(listener, cts.Token);

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

            _ = SendResponseAsync(context, token);   
        }
    }

    private async Task SendResponseAsync(HttpListenerContext context, CancellationToken token)
    {
        var response = context.Response;
        var request = context.Request;
        try
        {
            byte[] buffer = new byte[0];
            var localUrl = request.Url.LocalPath;
            var url = Path.Combine(AppContext.BaseDirectory + "static", localUrl.TrimStart('/'));
            try
            {
                //Console.WriteLine("путь запроса " + url);
                if(localUrl == "/")
                {
                    buffer = await File.ReadAllBytesAsync(ResponsePath, token);
                    response.ContentType = "text/html; charset=utf-8";
                }
                else
                {
                    buffer = await File.ReadAllBytesAsync(url, token);
                    response.ContentType = MimeTypes.GetMimeType(url);
                }
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            //Console.WriteLine(context.Request.Url.LocalPath);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
        }
        finally
        {
            response.Close();   
        }
    }

    public async Task ListenConsoleLoopAsync()
    {
        Console.WriteLine("Команды: start | stop | restart | exit");

        while (true)
        {
            var str = Console.ReadLine();
            if (str is null)
            {
                break;
            }
            bool keepGoing = await ExecuteCommandAsync(str.Trim().ToLowerInvariant());
            if (!keepGoing) break;
        }

        await StopAsync();
    }

    private async Task<bool> ExecuteCommandAsync(string command)
    {
        switch (command)
        {
            case "start":
                Start();
                return true;

            case "stop":
                await StopAsync();
                return true;

            case "restart":
                await StopAsync();
                Start();
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
public static class MimeTypes
{
    public static string GetMimeType(string absolutePath)
    {
        Dictionary<string, string> types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".css"]  = "text/css; charset=utf-8",
            [".js"]   = "text/javascript; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".png"]  = "image/png",
            [".jpg"]  = "image/jpeg",
            [".svg"]  = "image/svg+xml",
            [".webp"] = "image/webp"
        };
        return types[Path.GetExtension(absolutePath)];
    }
}
class Program
{
    static async Task Main(string[] args)
    {
        var server = new HttpServer(ServerSettings.Instance);
        server.Start();
        await server.ListenConsoleLoopAsync();
    }
}
