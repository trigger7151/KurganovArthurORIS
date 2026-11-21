namespace CardsApp.Services.LightCones;

/// <summary>Данные формы конуса в том виде, в каком пришли от пользователя.</summary>
public record ConeInput(
    int Id, string? Name, string? Rarity, string? PathId, string? EffectName, string? ImageUrl,
    string? DescriptionHtml, string?[] Stats1, string?[] Stats80, string?[] Effects);
