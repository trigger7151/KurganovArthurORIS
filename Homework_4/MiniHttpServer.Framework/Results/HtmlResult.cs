using System;
using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Results;

public class HtmlResult(string responseString, int statusCode = 200) : IHttpResult
{
    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        response.StatusCode = statusCode;
        response.ContentType = "text/html; charset=utf-8";
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, cancellationToken);
    }
}
