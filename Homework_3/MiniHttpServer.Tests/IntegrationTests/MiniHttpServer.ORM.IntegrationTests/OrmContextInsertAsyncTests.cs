using MiniHttpServer.ORM.IntegrationTests.Entities;
using Npgsql;

namespace MiniHttpServer.ORM.IntegrationTests;

[TestClass]
public sealed class OrmContextInsertAsyncTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=OrisTestdb;Username=OrisTestUser;Password=password";

    private const string ProductsTable = "insert_products";
    private const string UsersTable = "insert_users";
    private const string TypedTable = "insert_typed";
    private const string SecretTable = "insert_secret";
    private const string CaseTable = "insert_case";
    private const string NonExistentTable = "insert_nonexistent";

    private OrmContext Context { get; set; } = null!;

    [ClassInitialize]
    public static async Task DbInitialize(TestContext _)
    {
        await ExecuteNonQueryAsync($"""
            drop table if exists {ProductsTable}, {UsersTable}, {TypedTable}, {SecretTable}, {CaseTable}, {NonExistentTable};

            create table {ProductsTable}
            (
                Id serial primary key,
                Name varchar(100) not null,
                Price numeric(10, 2) not null,
                InStock boolean not null
            );

            -- Extra: столбец, для которого в моделях нет свойства
            create table {UsersTable}
            (
                Id serial primary key,
                Name varchar(100) not null,
                Email varchar(100),
                Extra varchar(50)
            );

            create table {TypedTable}
            (
                Id serial primary key,
                Age int
            );

            create table {SecretTable}
            (
                Id serial primary key,
                Name varchar(100),
                Secret varchar(100)
            );

            create table {CaseTable}
            (
                Id serial primary key,
                "NAME" varchar(100)
            );
            """);
    }

    [ClassCleanup]
    public static async Task DbCleanup()
    {
        await ExecuteNonQueryAsync($"""
            drop table if exists {ProductsTable}, {UsersTable}, {TypedTable}, {SecretTable}, {CaseTable};
            """);
    }

    [TestInitialize]
    public async Task SetUp()
    {
        await ExecuteNonQueryAsync($"""
            truncate {ProductsTable}, {UsersTable}, {TypedTable}, {SecretTable}, {CaseTable} restart identity;
            """);
        Context = new OrmContext(ConnectionString);
    }

    private static async Task<int> ExecuteNonQueryAsync(string query)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(query, connection);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(string query)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(query, connection);
        var result = await command.ExecuteScalarAsync();
        return result is DBNull ? null : result;
    }

    private static async Task<long> CountAsync(string table) =>
        (long)(await ScalarAsync($"select count(*) from {table}"))!;


    [TestMethod]
    public async Task InsertAsync_ValidEntity_ReturnsOneAndSavesValues()
    {
        var product = new InsertProduct { Name = "Keyboard", Price = 2500.50m, InStock = true };

        var inserted = await Context.InsertAsync(ProductsTable, product);

        Assert.AreEqual(1, inserted);
        Assert.AreEqual(1L, await CountAsync(ProductsTable));
        Assert.AreEqual("Keyboard", await ScalarAsync($"select name from {ProductsTable} where id = 1"));
        Assert.AreEqual(2500.50m, await ScalarAsync($"select price from {ProductsTable} where id = 1"));
        Assert.AreEqual(true, await ScalarAsync($"select instock from {ProductsTable} where id = 1"));
    }

    [TestMethod]
    public async Task InsertAsync_EntityWithId_IdIsGeneratedByDatabase()
    {
        var user = new InsertUser { Id = 999, Name = "Ivan" };

        await Context.InsertAsync(UsersTable, user);

        Assert.AreEqual(1, await ScalarAsync($"select id from {UsersTable} where name = 'Ivan'"));
        Assert.AreEqual(0L, (long)(await ScalarAsync($"select count(*) from {UsersTable} where id = 999"))!);
    }

    [TestMethod]
    public async Task InsertAsync_NullToNullableColumn_SavesNull()
    {
        var user = new InsertUser { Name = "Ivan", Email = null };

        await Context.InsertAsync(UsersTable, user);

        Assert.AreEqual(1L, await CountAsync(UsersTable));
        Assert.IsNull(await ScalarAsync($"select email from {UsersTable} where id = 1"));
    }

    [TestMethod]
    public async Task InsertAsync_ColumnCountGreaterThanPropertyCount_InsertsAndLeavesExtraColumnNull()
    {
        var user = new InsertUser { Name = "Ivan", Email = "ivan@example.com" };

        var inserted = await Context.InsertAsync(UsersTable, user);

        Assert.AreEqual(1, inserted);
        Assert.IsNull(await ScalarAsync($"select extra from {UsersTable} where id = 1"));
    }

    [TestMethod]
    public async Task InsertAsync_PropertyAndColumnDifferentCase_MatchesIgnoringCase()
    {
        var entity = new InsertCaseEntity { Name = "Ivan" };

        var inserted = await Context.InsertAsync(CaseTable, entity);

        Assert.AreEqual(1, inserted);
        Assert.AreEqual("Ivan", await ScalarAsync($"select \"NAME\" from {CaseTable} where id = 1"));
    }


    [TestMethod]
    public async Task InsertAsync_NotMappedPropertyWithoutColumn_IsIgnored()
    {
        var user = new InsertUserWithNotMapped { Name = "Ivan", Nickname = "vanya" };

        var inserted = await Context.InsertAsync(UsersTable, user);

        Assert.AreEqual(1, inserted);
        Assert.AreEqual(1L, await CountAsync(UsersTable));
    }

    [TestMethod]
    public async Task InsertAsync_NotMappedPropertyWithColumn_ColumnStaysNull()
    {
        var user = new InsertUserWithNotMappedEmail { Name = "Ivan", Email = "ivan@example.com" };

        await Context.InsertAsync(UsersTable, user);

        Assert.IsNull(await ScalarAsync($"select email from {UsersTable} where id = 1"));
    }


    [TestMethod]
    public async Task InsertAsync_ColumnForPropertyMissing_ThrowsInvalidOperationEx()
    {
        var entity = new InsertMissingColumnEntity { Name = "Ivan", Missing = "x" };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.InsertAsync(UsersTable, entity));
        Assert.AreEqual(0L, await CountAsync(UsersTable));
    }

    [TestMethod]
    public async Task InsertAsync_ColumnFoundForPropertyWithoutPublicGetter_ThrowsInvalidOperationEx()
    {
        var entity = new InsertSecretEntity { Name = "Ivan", Secret = "123" };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.InsertAsync(SecretTable, entity));
        Assert.AreEqual(0L, await CountAsync(SecretTable));
    }

    [TestMethod]
    public async Task InsertAsync_CantConvertPropertyValueToColumnType_ThrowsInvalidOperationEx()
    {
        var entity = new InsertTypedEntity { Age = "abc" };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.InsertAsync(TypedTable, entity));
        Assert.AreEqual(0L, await CountAsync(TypedTable));
    }

    [TestMethod]
    public async Task InsertAsync_NullToNotNullColumn_ThrowsInvalidOperationEx()
    {
        var user = new InsertUser { Name = null! };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.InsertAsync(UsersTable, user));
        Assert.AreEqual(0L, await CountAsync(UsersTable));
    }


    [TestMethod]
    public async Task InsertAsync_TableDoesntExist_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.InsertAsync(NonExistentTable, new InsertUser { Name = "Ivan" }));
    }

    [TestMethod]
    public async Task InsertAsync_CanceledToken_ThrowsAndDoesNotInsert()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Context.InsertAsync(UsersTable, new InsertUser { Name = "Ivan" }, cts.Token));
        Assert.AreEqual(0L, await CountAsync(UsersTable));
    }
}
