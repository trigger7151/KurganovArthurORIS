using System;
using System.Net;
using MiniHttpServer.Framework.Content;
namespace MiniHttpServer.Framework.Results;

public class StaticResult(string directoryPath, int statusCode = 200) : IHttpResult
{
    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(directoryPath))
            throw new UnauthorizedAccessException("An attempt to request folder was made");

        var buffer = await File.ReadAllBytesAsync(directoryPath, cancellationToken);
        response.StatusCode = statusCode;
        response.ContentType = $"{MimeTypes.GetMimeType(directoryPath)}";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, cancellationToken);
    }
}
