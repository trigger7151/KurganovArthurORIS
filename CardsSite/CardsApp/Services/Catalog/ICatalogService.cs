namespace CardsApp.Services.Catalog;

public interface ICatalogService
{
    Task<CatalogPageVm> GetCatalogAsync(CatalogQuery query, CancellationToken ct = default);
    Task<ConeDetailsVm?> GetDetailsAsync(int id, CancellationToken ct = default);
}
