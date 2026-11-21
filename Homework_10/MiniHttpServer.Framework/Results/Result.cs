using System;
using System.Net;
using MiniHttpServer.Framework.Content;
namespace MiniHttpServer.Framework.Results;
//Возвращает файлы только по пути, указанному в самом http запросе
// public class StaticResult : IHttpResult
// {
//     public async Task ExecuteAsync(HttpListenerContext context)
//     {
//         var response = context.Response;
//         var request = context.Request;
//         byte[] buffer;
//         var localUrl = request.Url.LocalPath;
//         Console.WriteLine($"Относительный url {localUrl}");
//         // path.combine сам добавляет между элементами /
//         var url = Path.Combine(AppContext.BaseDirectory + "static", localUrl.TrimStart('/'));
//         buffer = await File.ReadAllBytesAsync($"{url}");
//         // media type тоже должно быть динамичным. Пока будет работать только с некоторыми запросами
//         response.ContentType = $"{MimeTypes.GetMimeType(url)}; charset=utf-8";
//         using Stream output = response.OutputStream;
//         context.Response.StatusCode = (int)HttpStatusCode.OK;
//         await output.WriteAsync(buffer);
//     }
// }
// // возвращает файлы по указанному абсолютному пути
// public class FileResult : IHttpResult
// {
//     public string Path{get;private set;}
//     public FileResult(string path)
//     {
//         Path = path;
//     }
//     public async Task ExecuteAsync(HttpListenerContext context)
//     {
//         var buffer = await File.ReadAllBytesAsync(Path);
//         context.Response.ContentType = $"{MimeTypes.GetMimeType(Path)}; charset=utf-8";
//         context.Response.ContentLength64 = buffer.Length;
//         using Stream output = context.Response.OutputStream;
//         context.Response.StatusCode = (int)HttpStatusCode.OK;
//         await output.WriteAsync(buffer);
//     }
// }
