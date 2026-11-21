namespace CardsApp.Data.Repositories;

/// <summary>
/// Общий интерфейс репозитория (паттерн Repository).
/// Контроллеры и сервисы работают с данными только через него и не знают про SQL и ORM.
/// </summary>
public interface IRepository<T> where T : class, IEntity, new()
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Func<T, bool> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Реализация репозитория поверх IOrmContext.
/// ORM умеет выбирать только все строки таблицы, поэтому поиск по Id и фильтрация
/// выполняются в памяти — для каталога из десятков карточек этого достаточно.
/// </summary>
public class OrmRepository<T> : IRepository<T> where T : class, IEntity, new()
{
    private readonly IOrmContext _orm;
    protected string TableName { get; }

    public OrmRepository(IOrmContext orm, string tableName)
    {
        _orm = orm;
        TableName = tableName;
    }

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => _orm.GetAllAsync<T>(TableName, ct);

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => (await GetAllAsync(ct)).FirstOrDefault(e => e.Id == id);

    public async Task<IReadOnlyList<T>> FindAsync(Func<T, bool> predicate, CancellationToken ct = default)
        => (await GetAllAsync(ct)).Where(predicate).ToList();

    public Task AddAsync(T entity, CancellationToken ct = default)
        => _orm.InsertAsync(TableName, entity, ct);

    public async Task<bool> UpdateAsync(T entity, CancellationToken ct = default)
        => await _orm.UpdateAsync(TableName, entity.Id, entity, ct) > 0;

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        => await _orm.DeleteAsync(TableName, id, ct) > 0;
}

public interface IPathRepository : IRepository<CombatPathEntity> { }
public interface ILightConeRepository : IRepository<LightConeEntity> { }
public interface IStatRepository : IRepository<LightConeStatEntity> { }
public interface ISuperimpositionRepository : IRepository<SuperimpositionEntity> { }
public interface IUserRepository : IRepository<UserEntity>
{
    Task<UserEntity?> GetByLoginAsync(string login, CancellationToken ct = default);
}

public class PathRepository(IOrmContext orm) : OrmRepository<CombatPathEntity>(orm, "paths"), IPathRepository;
public class LightConeRepository(IOrmContext orm) : OrmRepository<LightConeEntity>(orm, "lightcones"), ILightConeRepository;
public class StatRepository(IOrmContext orm) : OrmRepository<LightConeStatEntity>(orm, "lightconestats"), IStatRepository;
public class SuperimpositionRepository(IOrmContext orm) : OrmRepository<SuperimpositionEntity>(orm, "superimpositions"), ISuperimpositionRepository;

public class UserRepository(IOrmContext orm) : OrmRepository<UserEntity>(orm, "users"), IUserRepository
{
    public async Task<UserEntity?> GetByLoginAsync(string login, CancellationToken ct = default)
        => (await GetAllAsync(ct)).FirstOrDefault(u =>
            string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));
}
