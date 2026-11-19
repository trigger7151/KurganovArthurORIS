namespace CardsApp.Web.Infrastructure;

/// <summary>
/// Корень композиции: здесь один раз собираются все зависимости приложения.
/// Фреймворк создаёт контроллеры через конструктор без параметров,
/// поэтому контроллеры получают сервисы отсюда, а не через конструктор.
/// </summary>
public sealed class AppServices
{
    private static AppServices? _current;
    public static AppServices Current => _current ?? throw new InvalidOperationException("AppServices не инициализирован.");

    public IUserSessionService Sessions { get; }
    public ICatalogService Catalog { get; }
    public IAuthService Auth { get; }
    public ILightConeAdminService ConesAdmin { get; }
    public IPathAdminService PathsAdmin { get; }
    public IImageStorage Images { get; }

    private AppServices(string connectionString, IUserSessionService sessions)
    {
        IOrmContext orm = new OrmContext(connectionString);
        var paths = new PathRepository(orm);
        var cones = new LightConeRepository(orm);
        var stats = new StatRepository(orm);
        var superimpositions = new SuperimpositionRepository(orm);
        var users = new UserRepository(orm);

        Sessions = sessions;
        Images = new LocalImageStorage();
        Catalog = new CatalogService(cones, paths, stats, superimpositions);
        Auth = new AuthService(users, new Pbkdf2PasswordHasher());
        ConesAdmin = new LightConeAdminService(cones, paths, stats, superimpositions, Images);
        PathsAdmin = new PathAdminService(paths, cones);
    }

    public static void Initialize(string connectionString, IUserSessionService sessions)
        => _current = new AppServices(connectionString, sessions);
}
