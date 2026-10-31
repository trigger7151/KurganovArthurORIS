using System;
using Search_server;
using System.Net;
using System.Web;
using HttpServer.Results;
using SocialApp.Services;
using JsonClasses;
using System.Text.Json;
using System.IO.Compression;
using MimeKit;
using Microsoft.CodeAnalysis;
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

[Route("/home-work")]
public class HomeWorkController: ControllerBase
{
    private static AuthenticationInfo DeserializeAuthInfo(string authInfoPath)
    {
        var settingsJson = File.ReadAllText(authInfoPath);
        return JsonSerializer.Deserialize<AuthenticationInfo>(settingsJson);
    }


    public static string GetSolutionDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.EnumerateFiles("*.sln").Any() || dir.EnumerateFiles("*.slnx").Any())
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Файл решения (.sln/.slnx) не найден.");
    }

    private static readonly string[] ExcludedDirs  = { ".vs", "bin", "obj" };
    private static readonly string[] ExcludedFiles = { "AuthInfo.json" };

    private static MemoryStream GetZipStream(string rootPath)
    {
        var zipStream = new MemoryStream();

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(rootPath, file);
                var parts = relative.Split(Path.DirectorySeparatorChar);

                if (parts.SkipLast(1).Any(d => ExcludedDirs.Contains(d, StringComparer.OrdinalIgnoreCase)))
                    continue;

                if (ExcludedFiles.Contains(parts[^1], StringComparer.OrdinalIgnoreCase))
                    continue;

                archive.CreateEntryFromFile(file, relative.Replace('\\', '/'), CompressionLevel.Optimal);
            }
        }

        zipStream.Position = 0;
        return zipStream;
    }

    [Method("GET")]
    [Route("/Send")]
    public async Task<IResult> ProcessHomeWorkSend()
    {
        var parameters = Context.Request.QueryString;
        var toEmail = parameters.GetValues("To").First();
        var serverPath = GetSolutionDirectory();
        var authInfoPath = Path.Combine(AppContext.BaseDirectory, "AuthInfo.json");
        if (!File.Exists(authInfoPath))
        {
            throw new FileNotFoundException($"Файл данных аутентификации {authInfoPath} не найден");
        }

        var authInfo = DeserializeAuthInfo(authInfoPath);
        var emailSevice = new EmailService(authInfo.Email, authInfo.Password);
        var stream = GetZipStream(GetSolutionDirectory());
        await emailSevice.SendEmailAsync(toEmail, "zip архив сервера", "", stream);
        return ReturnStaticResult();
    }
}

public class Order
{
    public static uint MinWeight{get;set;} = 20;
    public string Name{get;set;}
    public uint Weight{get;set;}
    public Order(uint weight, string name)
    {
        if (weight > 20000 || weight < 1)
            throw new ArgumentException("Вес должен быть не больше 20000 или меньше 1");
        if(name.Length < 2 || name.Length > 60)
        {
            throw new ArgumentException("длина имени должна быть от 2 до 60 символов");
        }
        Weight = weight;
        Name = name;
    }

    public override string ToString()
    {
        return $"Вес: {Weight.ToString()}, Имя: {Name}";
    }
}

[Route("/parcels")]
public class ParcelsController: ControllerBase
{
    [Method("GET")]
    [Route("")]
    public async Task<IResult> ProcessParcels()
    {
        Console.WriteLine("/parcels запрошен");
        var db = new List<Order>{new Order(1, "aaa"), new Order(50, "bbb"), new Order(60, "ccc"), new Order(70, "ddd")};

        var orders = db.Where(order => order.Weight > Order.MinWeight);
        
        var renderer = new MiniTemplateEngine.HtmlTemplateRenderer();
        var html = "<html>";
        foreach (var i in orders)
        {
            var renderedHtml = renderer.RenderFromString("<p>${i}</p>", i);
            html += renderedHtml;
        }
        html += "</html>";

        var result = new DynamicResult() {HtmlText = html};
        return result;
    }
}
