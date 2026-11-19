using System;
using System.Net;
using System.Text.Json;
namespace MiniHttpServer.Framework.Results;

public class JsonResult(object? data, int statusCode = 200) : IHttpResult
{
    private static readonly JsonSerializerOptions Options = new (JsonSerializerDefaults.Web);
    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        var buffer = JsonSerializer.SerializeToUtf8Bytes(data, Options);
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, cancellationToken);
    }
}
