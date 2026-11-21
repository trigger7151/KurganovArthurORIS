using System;
using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Results;

/// <summary>
/// Ответ с готовой HTML-строкой (Content-Type: text/html; charset=utf-8).
/// </summary>
/// <param name="responseString">HTML-разметка.</param>
/// <param name="statusCode">HTTP-статус ответа.</param>
public class HtmlResult(string responseString, int statusCode = 200) : IHttpResult
{
    /// <inheritdoc />
    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        response.StatusCode = statusCode;
        response.ContentType = "text/html; charset=utf-8";
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, cancellationToken);
    }
}
