using System;
using MiniHttpServer.Framework.Content;
using System.Net;
using System.Web;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Content;
using System.Text.Json;
using System.IO.Compression;
using MimeKit;
using Microsoft.CodeAnalysis;
using MiniHttpServer.Framework.Results;
namespace HttpServer.Controllers;

/// <summary>
/// Страница входа сайта Rockstar: показывает форму и принимает её отправку.
/// </summary>
[Route("/login")]
public class LoginController : ControllerBase
{
    /// <summary>
    /// POST /login — принимает отправленную форму, выводит её поля в консоль и снова показывает страницу входа.
    /// </summary>
    /// <returns>HTML страницы входа.</returns>
    [Method("POST")]
    [Route("")]
    public async Task<IHttpResult> ProcessLoginPostAuth()
    {
        var request = base.Context.Request;
        var response = base.Context.Response;
        // byte[] buffer = new byte[0];
        
        string body;
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            body = reader.ReadToEnd();

        var form = HttpUtility.ParseQueryString(body);

        string? login    = form["username"];
        string? password = form["password"];
        bool remember    = form["remember"] != null;
        string[]? langs  = form.GetValues("lang[]");    

        Console.WriteLine($"login={login}, password={password}, remember={remember}");

        var path = Path.Combine(AppContext.BaseDirectory + "static", "login/index.html");
        var loginHtml = await File.ReadAllTextAsync(path);
        var result = Html(loginHtml);
        return result;
        
    }

    /// <summary>
    /// GET /login — возвращает страницу входа.
    /// </summary>
    /// <returns>HTML страницы входа.</returns>
    [Method("GET")]
    [Route("")]
    public async Task<IHttpResult> ProcessLoginGet()
    {
        var path = Path.Combine(AppContext.BaseDirectory + "static", "login/index.html");
        var loginHtml = await File.ReadAllTextAsync(path);
        var result = Html(loginHtml);
        return result;
    }
}
