using System;
using System.Net;
using Search_server;
namespace HttpServer.Handlers;

public interface IHandler
{
    IHandler SetNext(IHandler handler);
    Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token);
}
public abstract class Handler : IHandler
{
    public IHandler Next{get; private set;}
    public virtual async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        await Next.HandleAsync(context, server, token);
    }
    public virtual IHandler SetNext(IHandler handler)
    {
        Next = handler;
        return Next;
    }
}