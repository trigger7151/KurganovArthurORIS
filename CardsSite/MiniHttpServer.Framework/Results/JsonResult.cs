using System;
using System.Net;
using System.Text.Json;
namespace MiniHttpServer.Framework.Results;

/// <summary>
/// Ответ с объектом, сериализованным в JSON (имена свойств в camelCase).
/// </summary>
/// <param name="data">Объект для сериализации.</param>
/// <param name="statusCode">HTTP-статус ответа.</param>
public class JsonResult(object? data, int statusCode = 200) : IHttpResult
{
    /// <summary>
    /// Общие настройки сериализации: веб-умолчания, то есть camelCase.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new (JsonSerializerDefaults.Web);
    /// <inheritdoc />
    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        var buffer = JsonSerializer.SerializeToUtf8Bytes(data, Options);
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, cancellationToken);
    }
}
