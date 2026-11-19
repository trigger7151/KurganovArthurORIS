using System.Globalization;
using System.Web;

namespace CardsApp.Services;

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

public interface ICatalogService
{
    Task<CatalogPageVm> GetCatalogAsync(CatalogQuery query, CancellationToken ct = default);
    Task<ConeDetailsVm?> GetDetailsAsync(int id, CancellationToken ct = default);
}

public class CatalogService(
    ILightConeRepository cones,
    IPathRepository paths,
    IStatRepository stats,
    ISuperimpositionRepository superimpositions) : ICatalogService
{
    private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), true);
    private static readonly NumberFormatInfo StatFormat = new() { NumberGroupSeparator = " ", NumberGroupSizes = [3] };

    public async Task<CatalogPageVm> GetCatalogAsync(CatalogQuery query, CancellationToken ct = default)
    {
        var allPaths = await paths.GetAllAsync(ct);
        var pathById = allPaths.ToDictionary(p => p.Id);
        var activePath = allPaths.FirstOrDefault(p => p.Slug == query.PathSlug);

        IEnumerable<LightCone> items = await cones.GetAllAsync(ct);
        if (query.SearchText.Length > 0)
            items = items.Where(c => c.Name.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase));
        if (query.RarityValue is int rarity)
            items = items.Where(c => c.Rarity == rarity);
        if (activePath is not null)
            items = items.Where(c => c.PathId == activePath.Id);

        items = query.SortValue switch
        {
            "name" => items.OrderBy(c => c.Name, NameComparer),
            "new" => items.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id),
            _ => items.OrderByDescending(c => c.Rarity).ThenBy(c => c.Id),
        };

        var cards = items.Select(c => ToCard(c, pathById)).ToList();
        var rarityValue = query.RarityValue?.ToString() ?? "";
        var pathValue = activePath?.Slug ?? "";

        return new CatalogPageVm
        {
            Query = query.SearchText,
            Rarity = rarityValue,
            Path = pathValue,
            Sort = query.SortValue,
            Items = cards,
            HasItems = cards.Count > 0,
            CountText = CountText(cards.Count),
            CountShort = CountShort(cards.Count),
            IsFiltered = query.SearchText.Length > 0 || rarityValue != "" || pathValue != "",
            RarityChips = new[] { ("", "Все"), ("5", "5★"), ("4", "4★"), ("3", "3★") }
                .Select(r => new ChipVm
                {
                    Value = r.Item1, Label = r.Item2, IsActive = r.Item1 == rarityValue,
                    Url = BuildUrl(query.SearchText, r.Item1, pathValue, query.SortValue),
                }).ToList(),
            PathChips = new[] { new ChipVm { Value = "", Label = "Все пути", IsActive = pathValue == "" } }
                .Concat(allPaths.OrderBy(p => p.Id).Select(p => new ChipVm
                {
                    Value = p.Slug, Label = p.Name, IsActive = p.Slug == pathValue,
                })).Select(chip =>
                {
                    chip.Url = BuildUrl(query.SearchText, rarityValue, chip.Value, query.SortValue);
                    return chip;
                }).ToList(),
            SortOptions = SortOptions(query.SortValue),
            SortLabel = SortOptions(query.SortValue).First(o => o.IsSelected).Label,
            SortShortLabel = SortOptions(query.SortValue).First(o => o.IsSelected).ShortLabel,
        };
    }

    public async Task<ConeDetailsVm?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        var cone = await cones.GetByIdAsync(id, ct);
        if (cone is null)
            return null;

        var path = await paths.GetByIdAsync(cone.PathId, ct);
        var levels = (await stats.FindAsync(s => s.LightConeId == id, ct)).OrderBy(s => s.Level).ToList();
        var effects = (await superimpositions.FindAsync(s => s.LightConeId == id, ct)).OrderBy(s => s.Level).ToList();

        var levelVms = levels.Select((s, i) => new StatLevelVm
        {
            Label = $"Ур. {s.Level}",
            Hp = s.Hp.ToString("#,0", StatFormat),
            Atk = s.Atk.ToString("#,0", StatFormat),
            Def = s.Def.ToString("#,0", StatFormat),
            IsActive = i == levels.Count - 1, // по умолчанию показываем максимальный уровень
        }).ToList();

        var effectVms = effects.Select((s, i) => new SuperimpositionVm
        {
            Label = $"S{s.Level}", Text = s.Text, IsActive = i == 0,
        }).ToList();

        var description = HtmlSanitizer.Sanitize(cone.DescriptionHtml);
        return new ConeDetailsVm
        {
            Id = cone.Id,
            Name = cone.Name,
            Rarity = cone.Rarity,
            Stars = Stars(cone.Rarity),
            PathName = path?.Name ?? "—",
            EffectName = cone.EffectName,
            ImageUrl = cone.ImageUrl ?? "",
            HasImage = !string.IsNullOrEmpty(cone.ImageUrl),
            Levels = levelVms,
            HasLevels = levelVms.Count > 0,
            CurrentStats = levelVms.LastOrDefault() ?? new StatLevelVm { Hp = "—", Atk = "—", Def = "—" },
            Superimpositions = effectVms,
            HasEffects = effectVms.Count > 0,
            CurrentEffectText = effectVms.FirstOrDefault()?.Text ?? "",
            DescriptionHtml = description,
            HasDescription = HtmlSanitizer.HasText(description),
        };
    }

    private static List<OptionVm> SortOptions(string selected) =>
    [
        new() { Value = "rarity", Label = "Сортировка: по редкости", ShortLabel = "По редкости", IsSelected = selected == "rarity" },
        new() { Value = "name", Label = "Сортировка: по названию", ShortLabel = "По названию", IsSelected = selected == "name" },
        new() { Value = "new", Label = "Сортировка: сначала новые", ShortLabel = "Сначала новые", IsSelected = selected == "new" },
    ];

    public static string Stars(int rarity) => new('★', Math.Clamp(rarity, 0, 5));

    /// <summary>«Показано 1 световой конус», «2 световых конуса», «5 световых конусов».</summary>
    public static string CountText(int count)
        => $"Показано {count} {Plural(count, "световой конус", "световых конуса", "световых конусов")}";

    /// <summary>«1 конус», «3 конуса», «18 конусов».</summary>
    public static string CountShort(int count) => $"{count} {Plural(count, "конус", "конуса", "конусов")}";

    private static string Plural(int count, string one, string few, string many)
    {
        var n = Math.Abs(count) % 100;
        if (n is >= 11 and <= 14) return many;
        return (n % 10) switch { 1 => one, 2 or 3 or 4 => few, _ => many };
    }

    private static ConeCardVm ToCard(LightCone cone, IReadOnlyDictionary<int, CombatPath> pathById) => new()
    {
        Id = cone.Id,
        Name = cone.Name,
        Rarity = cone.Rarity,
        Stars = Stars(cone.Rarity),
        PathName = pathById.TryGetValue(cone.PathId, out var p) ? p.Name : "",
        ImageUrl = cone.ImageUrl ?? "",
        HasImage = !string.IsNullOrEmpty(cone.ImageUrl),
        Url = $"/cone?id={cone.Id}",
    };

    private static string BuildUrl(string q, string rarity, string path, string sort)
    {
        var parts = new List<string>();
        if (q != "") parts.Add("q=" + HttpUtility.UrlEncode(q));
        if (rarity != "") parts.Add("rarity=" + rarity);
        if (path != "") parts.Add("path=" + path);
        if (sort != "rarity") parts.Add("sort=" + sort);
        return parts.Count == 0 ? "/" : "/?" + string.Join("&", parts);
    }
}
