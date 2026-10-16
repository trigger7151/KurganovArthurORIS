using System;
using Search_server;
using System.Net;
using System.Web;
using HttpServer.Results;
namespace HttpServer.Controllers;

[Route("/login")]
public class LoginController : ControllerBase
{
    [Method("POST")]
    [Route("")]
    public async Task<IResult> ProcessLoginPostAuth()
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
        var result = ReturnFileResult(path);
        return result;
        
    }
    [Method("GET")]
    [Route("")]
    public async Task<IResult> ProcessLoginGet()
    {
        var path = Path.Combine(AppContext.BaseDirectory + "static", "login/index.html");
        var result = ReturnFileResult(path);
        return result;
    }
}

