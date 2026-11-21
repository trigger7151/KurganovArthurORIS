using System.Text.Json;
using System.IO.Compression;
using MiniHttpServer.Framework.Results;
namespace HttpServer.Controllers;

/// <summary>
/// Отправка архива с исходным кодом решения на указанный адрес электронной почты.
/// </summary>
[Route("/home-work")]
public class HomeWorkController: ControllerBase
{
    /// <summary>
    /// Читает данные для входа в почтовый ящик отправителя из JSON-файла.
    /// </summary>
    /// <param name="authInfoPath">Путь к файлу с данными аутентификации.</param>
    /// <returns>Адрес и пароль отправителя.</returns>
    private static AuthenticationInfo DeserializeAuthInfo(string authInfoPath)
    {
        var settingsJson = File.ReadAllText(authInfoPath);
        return JsonSerializer.Deserialize<AuthenticationInfo>(settingsJson);
    }


    /// <summary>
    /// Ищет каталог решения, поднимаясь от каталога приложения вверх до первого файла .sln или .slnx.
    /// </summary>
    /// <returns>Полный путь к каталогу решения.</returns>
    /// <exception cref="DirectoryNotFoundException">Файл решения не найден ни в одном родительском каталоге.</exception>
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

    /// <summary>
    /// Каталоги, которые не попадают в архив (служебные файлы IDE и результаты сборки).
    /// </summary>
    private static readonly string[] ExcludedDirs  = { ".vs", "bin", "obj" };
    /// <summary>
    /// Файлы, которые не попадают в архив, чтобы не отправлять секретные данные.
    /// </summary>
    private static readonly string[] ExcludedFiles = { "AuthInfo.json" };

    /// <summary>
    /// Упаковывает содержимое каталога в ZIP-архив в памяти, пропуская
    /// <see cref="ExcludedDirs"/> и <see cref="ExcludedFiles"/>.
    /// </summary>
    /// <param name="rootPath">Каталог, содержимое которого архивируется.</param>
    /// <returns>Поток с архивом, позиция которого установлена в начало.</returns>
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

    /// <summary>
    /// GET /home-work/send?To=адрес — архивирует решение и отправляет его письмом на адрес из параметра <c>To</c>.
    /// </summary>
    /// <returns>HTML-страница с подтверждением отправки.</returns>
    /// <exception cref="FileNotFoundException">Не найден файл AuthInfo.json с данными отправителя.</exception>
    [Method("GET")]
    [Route("/Send")]
    public async Task<IHttpResult> ProcessHomeWorkSend()
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
        return Html("<html>Request sent</html>");
    }
}

