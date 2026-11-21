namespace CardsApp.Services.Paths;

public interface IPathAdminService
{
    Task<IReadOnlyList<(CombatPathEntity Path, int ConesCount)>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CombatPathEntity>> GetAllAsync(CancellationToken ct = default);
    Task<CombatPathEntity?> GetAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SaveAsync(PathInput input, CancellationToken ct = default);
    /// <summary>Удаляет путь. Возвращает текст ошибки или null при успехе.</summary>
    Task<string?> DeleteAsync(int id, CancellationToken ct = default);
}
