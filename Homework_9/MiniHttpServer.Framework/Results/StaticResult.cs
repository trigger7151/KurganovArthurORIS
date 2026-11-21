using System;
using System.Net;
using MiniHttpServer.Framework.Content;
namespace MiniHttpServer.Framework.Results;

/// <summary>
/// Ответ с содержимым файла; Content-Type определяется по расширению.
/// </summary>
/// <param name="directoryPath">Путь к файлу.</param>
/// <param name="statusCode">HTTP-статус ответа.</param>
public class StaticResult(string directoryPath, int statusCode = 200) : IHttpResult
{
    /// <inheritdoc />
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
