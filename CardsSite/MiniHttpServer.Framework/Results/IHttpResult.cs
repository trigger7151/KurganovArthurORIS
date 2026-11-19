using System.Net;
namespace MiniHttpServer.Framework.Results;

// Для каждого нового класса интерфейса IResult необходимо добавлять 
// новый метод, возвращающий экземпляр этого класса в ControllerBase. 
// Пространство имен HttpServer.Controllers;
public interface IHttpResult
{
    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default);
}
