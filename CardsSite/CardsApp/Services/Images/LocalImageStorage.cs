namespace CardsApp.Services.Images;

public class LocalImageStorage : IImageStorage
{
    public const long MaxSize = 5 * 1024 * 1024;
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, "static", "uploads");

    private static readonly Dictionary<string, (string Extension, byte[] Signature)> Formats = new()
    {
        ["image/png"] = (".png", [0x89, 0x50, 0x4E, 0x47]),
        ["image/jpeg"] = (".jpg", [0xFF, 0xD8, 0xFF]),
        ["image/webp"] = (".webp", [0x52, 0x49, 0x46, 0x46]),
    };

    public async Task<(string? Url, string? Error)> SaveAsync(Stream body, string? contentType, long contentLength, CancellationToken ct = default)
    {
        var type = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        if (!Formats.TryGetValue(type, out var format))
            return (null, "Поддерживаются только PNG, JPEG и WebP.");
        if (contentLength > MaxSize)
            return (null, "Файл больше 5 МБ.");

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxSize)
                return (null, "Файл больше 5 МБ.");
            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.ToArray();
        if (bytes.Length == 0 || !bytes.AsSpan().StartsWith(format.Signature))
            return (null, "Файл повреждён или не является картинкой.");

        Directory.CreateDirectory(_root);
        var fileName = Guid.NewGuid().ToString("N") + format.Extension;
        await File.WriteAllBytesAsync(Path.Combine(_root, fileName), bytes, ct);
        return ("/uploads/" + fileName, null);
    }

    public void DeleteIfLocal(string url)
    {
        if (!url.StartsWith("/uploads/", StringComparison.Ordinal))
            return;
        var fileName = Path.GetFileName(url);
        var fullPath = Path.Combine(_root, fileName);
        if (ValidationRules.ImageUrl().IsMatch("/uploads/" + fileName) && File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
