namespace CardsApp.Services.LightCones;

public interface ILightConeAdminService
{
    Task<IReadOnlyList<(LightConeEntity Cone, string PathName)>> ListAsync(CancellationToken ct = default);
    Task<ConeInput?> GetInputAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SaveAsync(ConeInput input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> IsNameAvailableAsync(string name, int exceptId, CancellationToken ct = default);
}
