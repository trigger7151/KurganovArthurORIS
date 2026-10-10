using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace HandlerAbstraction
{
    public interface IHandler
    {
        Task HandleAsync(HttpListenerContext context, CancellationToken token);
        IHandler SetNext(IHandler next);
    }
    public abstract class Handler : IHandler
    {
        private IHandler? next;
        public virtual async Task HandleAsync(HttpListenerContext context, CancellationToken token)
        {
            if (next != null)
                await next.HandleAsync(context, token);
        }

        public IHandler SetNext(IHandler next)
        {
            this.next = next;
            return next;
        }
    }
}
