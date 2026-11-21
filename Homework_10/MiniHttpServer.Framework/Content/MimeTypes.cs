using System;

namespace MiniHttpServer.Framework.Content;

/// <summary>
/// Определяет MIME-тип (заголовок Content-Type) файла по его расширению.
/// </summary>
public static class MimeTypes
{
    /// <summary>
    /// Тип для неизвестных расширений: «просто байты», браузер предложит скачать файл.
    /// </summary>
    private const string DefaultType = "application/octet-stream";

    /// <summary>
    /// Соответствие расширений и MIME-типов; регистр расширения не учитывается.
    /// </summary>
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"]  = "text/html; charset=utf-8",
        [".htm"]   = "text/html; charset=utf-8",
        [".css"]   = "text/css; charset=utf-8",
        [".js"]    = "text/javascript; charset=utf-8",
        [".json"]  = "application/json; charset=utf-8",
        [".txt"]   = "text/plain; charset=utf-8",
        [".png"]   = "image/png",
        [".jpg"]   = "image/jpeg",
        [".jpeg"]  = "image/jpeg",
        [".gif"]   = "image/gif",
        [".svg"]   = "image/svg+xml",
        [".webp"]  = "image/webp",
        [".ico"]   = "image/x-icon",
        [".pdf"]   = "application/pdf",
        [".zip"]   = "application/zip",
    };

    /// <summary>
    /// Возвращает MIME-тип по расширению файла.
    /// </summary>
    /// <param name="path">Путь или имя файла.</param>
    /// <returns>MIME-тип или <c>application/octet-stream</c>, если расширение неизвестно.</returns>
    public static string GetMimeType(string path)
        => Types.TryGetValue(Path.GetExtension(path), out var type) ? type : DefaultType;
}
