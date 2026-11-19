using System;

namespace MiniHttpServer.Framework.Content;

public static class MimeTypes
{
    private const string DefaultType = "application/octet-stream";

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

    public static string GetMimeType(string path)
        => Types.TryGetValue(Path.GetExtension(path), out var type) ? type : DefaultType;
}
