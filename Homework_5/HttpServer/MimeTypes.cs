using System;

namespace MimeTypesSpace;

public static class MimeTypes
{
    public static string GetMimeType(string absolutePath)
    {
        Dictionary<string, string> types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".css"]  = "text/css; charset=utf-8",
            [".js"]   = "text/javascript; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".png"]  = "image/png",
            [".jpg"]  = "image/jpeg",
            [".svg"]  = "image/svg+xml",
            [".webp"] = "image/webp"
        };
        return types[Path.GetExtension(absolutePath)];
    }
}
