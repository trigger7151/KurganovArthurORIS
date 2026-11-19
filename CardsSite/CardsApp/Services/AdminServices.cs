namespace CardsApp.Services;

// ---------- Вход в админку ----------

public interface IAuthService
{
    Task<User?> GetUserAsync(int? userId, CancellationToken ct = default);
    /// <summary>Возвращает пользователя или null, если логин или пароль неверны.</summary>
    Task<User?> SignInAsync(string? login, string? password, CancellationToken ct = default);
}

public class AuthService(IUserRepository users, IPasswordHasher hasher) : IAuthService
{
    public async Task<User?> GetUserAsync(int? userId, CancellationToken ct = default)
        => userId is int id ? await users.GetByIdAsync(id, ct) : null;

    public async Task<User?> SignInAsync(string? login, string? password, CancellationToken ct = default)
    {
        login = (login ?? "").Trim();
        password ??= "";
        if (!ValidationRules.Login().IsMatch(login) || password.Length is 0 or > 64)
            return null;

        var user = await users.GetByLoginAsync(login, ct);
        return user is not null && hasher.Verify(password, user.PasswordHash) ? user : null;
    }
}

// ---------- Световые конусы ----------

/// <summary>Данные формы конуса в том виде, в каком пришли от пользователя.</summary>
public record ConeInput(
    int Id, string? Name, string? Rarity, string? PathId, string? EffectName, string? ImageUrl,
    string? DescriptionHtml, string?[] Stats1, string?[] Stats80, string?[] Effects);

public record SaveResult(ValidationErrors Errors, int Id)
{
    public bool Success => Errors.IsValid;
}

public interface ILightConeAdminService
{
    Task<IReadOnlyList<(LightCone Cone, string PathName)>> ListAsync(CancellationToken ct = default);
    Task<ConeInput?> GetInputAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SaveAsync(ConeInput input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> IsNameAvailableAsync(string name, int exceptId, CancellationToken ct = default);
}

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

    public async Task<IReadOnlyList<(LightCone Cone, string PathName)>> ListAsync(CancellationToken ct = default)
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
        LightCone? existing = null;
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

        var cone = existing ?? new LightCone { CreatedAt = DateTime.UtcNow };
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
                  ?? new LightConeStat { LightConeId = coneId, Level = level };
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
                      ?? new Superimposition { LightConeId = coneId, Level = level };
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

// ---------- Пути ----------

public record PathInput(int Id, string? Name, string? Slug, string? Description);

public interface IPathAdminService
{
    Task<IReadOnlyList<(CombatPath Path, int ConesCount)>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CombatPath>> GetAllAsync(CancellationToken ct = default);
    Task<CombatPath?> GetAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SaveAsync(PathInput input, CancellationToken ct = default);
    /// <summary>Удаляет путь. Возвращает текст ошибки или null при успехе.</summary>
    Task<string?> DeleteAsync(int id, CancellationToken ct = default);
}

public class PathAdminService(IPathRepository paths, ILightConeRepository cones) : IPathAdminService
{
    public async Task<IReadOnlyList<(CombatPath Path, int ConesCount)>> ListAsync(CancellationToken ct = default)
    {
        var allCones = await cones.GetAllAsync(ct);
        return (await paths.GetAllAsync(ct)).OrderBy(p => p.Id)
            .Select(p => (p, allCones.Count(c => c.PathId == p.Id))).ToList();
    }

    public Task<IReadOnlyList<CombatPath>> GetAllAsync(CancellationToken ct = default) => paths.GetAllAsync(ct);

    public Task<CombatPath?> GetAsync(int id, CancellationToken ct = default) => paths.GetByIdAsync(id, ct);

    public async Task<SaveResult> SaveAsync(PathInput input, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        var all = await paths.GetAllAsync(ct);
        var existing = input.Id == 0 ? null : all.FirstOrDefault(p => p.Id == input.Id);
        if (input.Id != 0 && existing is null)
        {
            errors.Add("form", "Путь не найден: возможно, его уже удалили.");
            return new SaveResult(errors, 0);
        }

        var name = errors.Text("name", input.Name, "Название", 40, ValidationRules.PathName(), minLength: 2,
            ruleMessage: "Название пути: только буквы, пробел и дефис.");
        var slug = errors.Text("slug", input.Slug, "Ключ", 20, ValidationRules.Slug(), minLength: 3,
            ruleMessage: "Ключ: только строчные латинские буквы, например destruction.");
        var description = errors.Text("description", input.Description, "Описание", 500, required: false);

        if (errors.Get("name") == "" && all.Any(p => p.Id != input.Id && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            errors.Add("name", "Путь с таким названием уже есть.");
        if (errors.Get("slug") == "" && all.Any(p => p.Id != input.Id && p.Slug == slug))
            errors.Add("slug", "Такой ключ уже занят.");
        if (!errors.IsValid)
            return new SaveResult(errors, input.Id);

        var path = existing ?? new CombatPath();
        (path.Name, path.Slug, path.Description) = (name, slug, description);
        if (existing is null)
        {
            await paths.AddAsync(path, ct);
            path = (await paths.FindAsync(p => p.Slug == slug, ct)).Single();
        }
        else
        {
            await paths.UpdateAsync(path, ct);
        }
        return new SaveResult(errors, path.Id);
    }

    public async Task<string?> DeleteAsync(int id, CancellationToken ct = default)
    {
        var used = (await cones.GetAllAsync(ct)).Count(c => c.PathId == id);
        if (used > 0)
            return $"Нельзя удалить путь: к нему относятся конусы ({used}). Сначала перенесите их на другой путь.";
        return await paths.DeleteAsync(id, ct) ? null : "Путь не найден.";
    }
}

// ---------- Хранилище артов ----------

public interface IImageStorage
{
    /// <summary>Сохраняет картинку и возвращает её адрес (/uploads/...), либо текст ошибки.</summary>
    Task<(string? Url, string? Error)> SaveAsync(Stream body, string? contentType, long contentLength, CancellationToken ct = default);
    void DeleteIfLocal(string url);
}

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
