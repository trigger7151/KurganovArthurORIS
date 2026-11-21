using System.Net;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Handlers;

/// <summary>
/// Первый обработчик цепочки: перехватывает необработанные исключения остальных обработчиков,
/// пишет их в консоль и отвечает клиенту 500 без подробностей ошибки.
/// </summary>
public class ExceptionHandler : Handler
{
    /// <inheritdoc />
    public override async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        try
        {
            await Next.HandleAsync(context, server, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{context.Request.HttpMethod} {context.Request.Url?.LocalPath}:\n{ex}");
            await TryWriteErrorAsync(context.Response, token);
        }
    }

    /// <summary>
    /// Пытается записать ответ 500. Если ответ уже начал отправляться
    /// или клиент отключился, ошибка записи игнорируется.
    /// </summary>
    /// <param name="response">HTTP-ответ.</param>
    /// <param name="token">Токен отмены.</param>
    private static async Task TryWriteErrorAsync(HttpListenerResponse response, CancellationToken token)
    {
        try
        {
            await new JsonResult(new { Success = false, Message = "Internal server error" }, 500)
                .ExecuteAsync(response, token);
        }
        catch
        {
        }
    }
}
