namespace CardsApp.Services.Images;

public interface IImageStorage
{
    /// <summary>Сохраняет картинку и возвращает её адрес (/uploads/...), либо текст ошибки.</summary>
    Task<(string? Url, string? Error)> SaveAsync(Stream body, string? contentType, long contentLength, CancellationToken ct = default);
    void DeleteIfLocal(string url);
}
