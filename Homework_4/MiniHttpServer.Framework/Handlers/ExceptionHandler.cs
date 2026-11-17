using System.Net;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Handlers;

public class ExceptionHandler : Handler
{
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

    private static async Task TryWriteErrorAsync(HttpListenerResponse response, CancellationToken token)
    {
        try
        {
            await new JsonResult(new { Success = false, Message = "Inner server error" }, 500)
                .ExecuteAsync(response, token);
        }
        catch
        {
        }
    }
}
