namespace CardsApp.Services.LightCones;

public class LightConeAdminService(
    ILightConeRepository cones,
    IPathRepository paths,
    IStatRepository stats,
    ISuperimpositionRepository superimpositions,
    IImageStorage images) : ILightConeAdminService
{
    public const int EffectCount = 5;
    public const int DescriptionMaxLength = 20_000;
    public const int EffectTextMaxLength = 600;
    public static readonly string[] StatNames = ["hp", "atk", "def"];

    public async Task<IReadOnlyList<(LightConeEntity Cone, string PathName)>> ListAsync(CancellationToken ct = default)
    {
        var pathById = (await paths.GetAllAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        return (await cones.GetAllAsync(ct))
            .OrderByDescending(c => c.Rarity).ThenBy(c => c.Name)
            .Select(c => (c, pathById.GetValueOrDefault(c.PathId, "—")))
            .ToList();
    }

    public async Task<ConeInput?> GetInputAsync(int id, CancellationToken ct = default)
    {
        var cone = await cones.GetByIdAsync(id, ct);
        if (cone is null)
            return null;

        var coneStats = await stats.FindAsync(s => s.LightConeId == id, ct);
        var coneEffects = await superimpositions.FindAsync(s => s.LightConeId == id, ct);
        string?[] StatsOf(int level)
        {
            var s = coneStats.FirstOrDefault(x => x.Level == level);
            return s is null ? ["", "", ""] : [s.Hp.ToString(), s.Atk.ToString(), s.Def.ToString()];
        }

        return new ConeInput(cone.Id, cone.Name, cone.Rarity.ToString(), cone.PathId.ToString(), cone.EffectName,
            cone.ImageUrl, cone.DescriptionHtml, StatsOf(StatLevels.Min), StatsOf(StatLevels.Max),
            Enumerable.Range(1, EffectCount).Select(l => coneEffects.FirstOrDefault(e => e.Level == l)?.Text).ToArray());
    }

    public async Task<bool> IsNameAvailableAsync(string name, int exceptId, CancellationToken ct = default)
    {
        name = name.Trim();
        return !(await cones.GetAllAsync(ct)).Any(c =>
            c.Id != exceptId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<SaveResult> SaveAsync(ConeInput input, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        LightConeEntity? existing = null;
        if (input.Id != 0)
        {
            existing = await cones.GetByIdAsync(input.Id, ct);
            if (existing is null)
            {
                errors.Add("form", "Конус не найден: возможно, его уже удалили.");
                return new SaveResult(errors, 0);
            }
        }

        var name = errors.Text("name", input.Name, "Название", 80, ValidationRules.Title(), minLength: 2);
        if (errors.Get("name") == "" && !await IsNameAvailableAsync(name, input.Id, ct))
            errors.Add("name", "Конус с таким названием уже есть.");

        var rarity = int.TryParse(input.Rarity, out var r) && r is >= 3 and <= 5 ? r : 0;
        if (rarity == 0) errors.Add("rarity", "Выберите редкость: 3★, 4★ или 5★.");

        var pathId = int.TryParse(input.PathId, out var p) ? p : 0;
        if (await paths.GetByIdAsync(pathId, ct) is null) errors.Add("pathId", "Выберите путь из списка.");

        var effectName = errors.Text("effectName", input.EffectName, "Название эффекта", 60, ValidationRules.Title(), minLength: 2);
        var imageUrl = errors.Text("imageUrl", input.ImageUrl, "Арт", 300, ValidationRules.ImageUrl(), required: false,
            ruleMessage: "Укажите адрес вида https://... или загрузите файл.");

        var description = HtmlSanitizer.Sanitize(input.DescriptionHtml);
        if (!HtmlSanitizer.HasText(description))
            description = "";
        if (description.Length > DescriptionMaxLength)
            errors.Add("description", $"Описание слишком длинное (больше {DescriptionMaxLength} символов разметки).");

        var stats1 = ReadStats(errors, input.Stats1, StatLevels.Min);
        var stats80 = ReadStats(errors, input.Stats80, StatLevels.Max);
        for (var i = 0; i < StatNames.Length; i++)
        {
            if (errors.Get($"{StatNames[i]}{StatLevels.Max}") == "" && stats80[i] < stats1[i])
                errors.Add($"{StatNames[i]}{StatLevels.Max}", "На Ур. 80 значение не может быть меньше, чем на Ур. 1.");
        }

        var effectTexts = Enumerable.Range(1, EffectCount)
            .Select(level => errors.Text($"s{level}", input.Effects.ElementAtOrDefault(level - 1),
                $"Эффект S{level}", EffectTextMaxLength, minLength: 10))
            .ToArray();

        if (!errors.IsValid)
            return new SaveResult(errors, input.Id);

        var cone = existing ?? new LightConeEntity { CreatedAt = DateTime.UtcNow };
        var oldImage = existing?.ImageUrl;
        cone.Name = name;
        cone.Rarity = rarity;
        cone.PathId = pathId;
        cone.EffectName = effectName;
        cone.ImageUrl = imageUrl.Length > 0 ? imageUrl : null;
        cone.DescriptionHtml = description;

        if (existing is null)
        {
            await cones.AddAsync(cone, ct);
            // ORM возвращает только число вставленных строк, поэтому Id новой записи
            // приходится искать по уникальному названию.
            cone = (await cones.FindAsync(c => c.Name == name, ct)).Single();
        }
        else
        {
            await cones.UpdateAsync(cone, ct);
        }

        await SaveStatsAsync(cone.Id, StatLevels.Min, stats1, ct);
        await SaveStatsAsync(cone.Id, StatLevels.Max, stats80, ct);
        await SaveEffectsAsync(cone.Id, effectTexts, ct);

        if (oldImage is not null && oldImage != cone.ImageUrl)
            await DeleteImageIfUnusedAsync(oldImage, ct);

        return new SaveResult(errors, cone.Id);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var cone = await cones.GetByIdAsync(id, ct);
        if (cone is null)
            return false;
        // Характеристики и эффекты удаляются каскадно (ON DELETE CASCADE).
        var deleted = await cones.DeleteAsync(id, ct);
        if (deleted && cone.ImageUrl is not null)
            await DeleteImageIfUnusedAsync(cone.ImageUrl, ct);
        return deleted;
    }

    private static int[] ReadStats(ValidationErrors errors, string?[] values, int level)
        => StatNames.Select((stat, i) => errors.Number($"{stat}{level}", values.ElementAtOrDefault(i),
            $"{stat.ToUpperInvariant()} на Ур. {level}", 0, 99_999)).ToArray();

    private async Task SaveStatsAsync(int coneId, int level, int[] values, CancellationToken ct)
    {
        var row = (await stats.FindAsync(s => s.LightConeId == coneId && s.Level == level, ct)).FirstOrDefault()
                  ?? new LightConeStatEntity { LightConeId = coneId, Level = level };
        (row.Hp, row.Atk, row.Def) = (values[0], values[1], values[2]);
        if (row.Id == 0) await stats.AddAsync(row, ct);
        else await stats.UpdateAsync(row, ct);
    }

    private async Task SaveEffectsAsync(int coneId, string[] texts, CancellationToken ct)
    {
        var existing = await superimpositions.FindAsync(s => s.LightConeId == coneId, ct);
        for (var level = 1; level <= texts.Length; level++)
        {
            var row = existing.FirstOrDefault(s => s.Level == level)
                      ?? new SuperimpositionEntity { LightConeId = coneId, Level = level };
            row.Text = texts[level - 1];
            if (row.Id == 0) await superimpositions.AddAsync(row, ct);
            else await superimpositions.UpdateAsync(row, ct);
        }
    }

    private async Task DeleteImageIfUnusedAsync(string url, CancellationToken ct)
    {
        if (!(await cones.GetAllAsync(ct)).Any(c => c.ImageUrl == url))
            images.DeleteIfLocal(url);
    }
}
