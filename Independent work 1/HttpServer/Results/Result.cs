using System;
using System.Net;
using MimeTypesSpace;
using System.Text;
namespace HttpServer.Results;
// Для каждого нового класса интерфейса IResult необходимо добавлять 
// новый метод, возвращающий экземпляр этого класса в ControllerBase. 
// Пространство имен HttpServer.Controllers;
public interface IResult
{
    public Task Execute(HttpListenerContext context);
}
//Возвращает файлы только по пути, указанному в самом http запросе
public class StaticResult : IResult
{
    public async Task Execute(HttpListenerContext context)
    {
        var response = context.Response;
        var request = context.Request;
        byte[] buffer;
        var localUrl = request.Url.LocalPath;
        Console.WriteLine($"Относительный url {localUrl}");
        // path.combine сам добавляет между элементами /
        var url = Path.Combine(AppContext.BaseDirectory + "static", localUrl.TrimStart('/'));
        buffer = await File.ReadAllBytesAsync($"{url}");
        // media type тоже должно быть динамичным. Пока будет работать только с некоторыми запросами
        response.ContentType = $"{MimeTypes.GetMimeType(url)}; charset=utf-8";
        using Stream output = response.OutputStream;
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await output.WriteAsync(buffer);
    }
}

// возвращает файлы по указанному абсолютному пути
public class FileResult : IResult
{
    public string Path{get;private set;}
    public FileResult(string path)
    {
        Path = path;
    }
    public async Task Execute(HttpListenerContext context)
    {
        var buffer = await File.ReadAllBytesAsync(Path);
        context.Response.ContentType = $"{MimeTypes.GetMimeType(Path)}; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;
        using Stream output = context.Response.OutputStream;
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await output.WriteAsync(buffer);
    }
}

public class DynamicResult : IResult
{
    public string HtmlText{get;set;}
    public async Task Execute(HttpListenerContext context)
    {
        var response = context.Response;
        byte[] buffer;
        buffer = Encoding.UTF8.GetBytes(HtmlText);
        response.ContentType = $"text/html; charset=utf-8";
        using Stream output = response.OutputStream;
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await output.WriteAsync(buffer);
    }
}