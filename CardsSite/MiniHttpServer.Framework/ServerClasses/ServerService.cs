using System;

namespace MiniHttpServer.Framework.ServerClasses;

public static class ServerService
{
    private static Server? Server{get;set;}
    private static bool IsStarted{get;set;}
    private static bool ListenConsole{get;set;}
    public static async Task StartServer(bool listenConsole, IUserSessionService sessionService)
    {
        if (IsStarted)
        {
            throw new Exception("Server already started");
        }
        Build(listenConsole, sessionService);
        Server.Start();
        IsStarted = true;
        if (ListenConsole)
        {
            var listener = new ConsoleListener(Server);
            await listener.ListenConsoleLoopAsync();
        }
    }
    private static void Build(bool listenConsole, IUserSessionService sessionService)
    {
        ListenConsole = true;
        var pipeline = GetPipeline(sessionService);
        RouteTable.MapRoutes();
        Server = new Server(ServerSettings.Instance, pipeline);
    }
    private static IHandler GetPipeline(IUserSessionService sessionService)
    {
        var pipeline = new ExceptionHandler();
        pipeline.SetNext(new StaticHandler())       
        .SetNext(new RouteHandler(sessionService))        
        .SetNext(new NotFoundHandler());    
        return pipeline;
    }
}
