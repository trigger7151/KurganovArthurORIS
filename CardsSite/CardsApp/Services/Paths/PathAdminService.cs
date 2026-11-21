namespace CardsApp.Services.Paths;

public class PathAdminService(IPathRepository paths, ILightConeRepository cones) : IPathAdminService
{
    public async Task<IReadOnlyList<(CombatPathEntity Path, int ConesCount)>> ListAsync(CancellationToken ct = default)
    {
        var allCones = await cones.GetAllAsync(ct);
        return (await paths.GetAllAsync(ct)).OrderBy(p => p.Id)
            .Select(p => (p, allCones.Count(c => c.PathId == p.Id))).ToList();
    }

    public Task<IReadOnlyList<CombatPathEntity>> GetAllAsync(CancellationToken ct = default) => paths.GetAllAsync(ct);

    public Task<CombatPathEntity?> GetAsync(int id, CancellationToken ct = default) => paths.GetByIdAsync(id, ct);

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

        var path = existing ?? new CombatPathEntity();
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
