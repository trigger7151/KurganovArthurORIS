namespace CardsApp.Services.Catalog;

/// <summary>Параметры фильтра каталога. Значения приходят из адресной строки как есть.</summary>
public record CatalogQuery(string? Q, string? Rarity, string? Path, string? Sort)
{
    public const int MaxQueryLength = 80;
    public static readonly string[] SortValues = ["rarity", "name", "new"];

    public string SearchText
    {
        get
        {
            var q = (Q ?? "").Trim();
            return q.Length > MaxQueryLength ? q[..MaxQueryLength] : q;
        }
    }
    public int? RarityValue => int.TryParse(Rarity, out var r) && r is >= 3 and <= 5 ? r : null;
    public string PathSlug => ValidationRules.Slug().IsMatch(Path ?? "") ? Path! : "";
    public string SortValue => SortValues.Contains(Sort) ? Sort! : "rarity";
}
