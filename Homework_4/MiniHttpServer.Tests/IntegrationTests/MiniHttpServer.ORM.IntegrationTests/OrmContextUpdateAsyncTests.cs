using MiniHttpServer.ORM.IntegrationTests.Entities;
using Npgsql;

namespace MiniHttpServer.ORM.IntegrationTests;

[TestClass]
public sealed class OrmContextUpdateAsyncTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=OrisTestdb;Username=OrisTestUser;Password=password";

    private const string ProductsTable = "update_products";
    private const string UsersTable = "update_users";
    private const string TypedTable = "update_typed";
    private const string SecretTable = "update_secret";
    private const string CaseTable = "update_case";
    private const string NoIdTable = "update_noid";
    private const string NonExistentTable = "update_nonexistent";

    private OrmContext Context { get; set; } = null!;

    [ClassInitialize]
    public static async Task DbInitialize(TestContext _)
    {
        await ExecuteNonQueryAsync($"""
            drop table if exists {ProductsTable}, {UsersTable}, {TypedTable}, {SecretTable}, {CaseTable}, {NoIdTable}, {NonExistentTable};

            create table {ProductsTable}
            (
                Id serial primary key,
                Name varchar(100) not null,
                Price numeric(10, 2) not null,
                InStock boolean not null
            );

            --столбец, для которого в моделях нет свойства
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

            create table {NoIdTable}
            (
                Idd serial primary key,
                Name varchar(100) not null,
                Email varchar(100)
            );
            """);
    }

    [ClassCleanup]
    public static async Task DbCleanup()
    {
        await ExecuteNonQueryAsync($"""
            drop table if exists {ProductsTable}, {UsersTable}, {TypedTable}, {SecretTable}, {CaseTable}, {NoIdTable};
            """);
    }

    [TestInitialize]
    public async Task SetUp()
    {
        await ExecuteNonQueryAsync($"""
            truncate {ProductsTable}, {UsersTable}, {TypedTable}, {SecretTable}, {CaseTable}, {NoIdTable} restart identity;

            insert into {ProductsTable} (Name, Price, InStock) values ('Keyboard', 2500.50, true), ('Mouse', 900, false);
            insert into {UsersTable} (Name, Email, Extra) values ('Ivan', 'ivan@example.com', 'old'), ('Petr', 'petr@example.com', 'old');
            insert into {TypedTable} (Age) values (20);
            insert into {SecretTable} (Name, Secret) values ('Ivan', 'old');
            insert into {CaseTable} ("NAME") values ('Ivan');
            insert into {NoIdTable} (Name) values ('Ivan');
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

    private static Task<object?> UserNameAsync(int id) =>
        ScalarAsync($"select name from {UsersTable} where id = {id}");


    [TestMethod]
    public async Task UpdateAsync_IdExists_ReturnsOneAndUpdatesValues()
    {
        var product = new InsertProduct { Name = "Monitor", Price = 15000.99m, InStock = false };

        var updated = await Context.UpdateAsync(ProductsTable, 1, product);

        Assert.AreEqual(1, updated);
        Assert.AreEqual("Monitor", await ScalarAsync($"select name from {ProductsTable} where id = 1"));
        Assert.AreEqual(15000.99m, await ScalarAsync($"select price from {ProductsTable} where id = 1"));
        Assert.AreEqual(false, await ScalarAsync($"select instock from {ProductsTable} where id = 1"));
    }

    [TestMethod]
    public async Task UpdateAsync_IdExists_OtherRowsNotChanged()
    {
        await Context.UpdateAsync(UsersTable, 1, new InsertUser { Name = "Sergey" });

        Assert.AreEqual("Petr", await UserNameAsync(2));
        Assert.AreEqual("petr@example.com", await ScalarAsync($"select email from {UsersTable} where id = 2"));
    }

    [TestMethod]
    public async Task UpdateAsync_IdDoesntExist_ReturnsZeroAndChangesNothing()
    {
        var updated = await Context.UpdateAsync(UsersTable, 3, new InsertUser { Name = "Sergey" });

        Assert.AreEqual(0, updated);
        Assert.AreEqual(0L, (long)(await ScalarAsync($"select count(*) from {UsersTable} where name = 'Sergey'"))!);
    }

    [TestMethod]
    public async Task UpdateAsync_EntityIdDiffersFromIdParameter_UsesIdParameterAndKeepsId()
    {
        var user = new InsertUser { Id = 2, Name = "Sergey" };

        await Context.UpdateAsync(UsersTable, 1, user);

        Assert.AreEqual("Sergey", await UserNameAsync(1));
        Assert.AreEqual("Petr", await UserNameAsync(2));
    }

    [TestMethod]
    public async Task UpdateAsync_NullToNullableColumn_SetsNull()
    {
        await Context.UpdateAsync(UsersTable, 1, new InsertUser { Name = "Ivan", Email = null });

        Assert.IsNull(await ScalarAsync($"select email from {UsersTable} where id = 1"));
    }

    [TestMethod]
    public async Task UpdateAsync_ColumnCountGreaterThanPropertyCount_ExtraColumnKeepsValue()
    {
        var updated = await Context.UpdateAsync(UsersTable, 1, new InsertUser { Name = "Sergey" });

        Assert.AreEqual(1, updated);
        Assert.AreEqual("old", await ScalarAsync($"select extra from {UsersTable} where id = 1"));
    }

    [TestMethod]
    public async Task UpdateAsync_PropertyAndColumnDifferentCase_MatchesIgnoringCase()
    {
        var updated = await Context.UpdateAsync(CaseTable, 1, new InsertCaseEntity { Name = "Sergey" });

        Assert.AreEqual(1, updated);
        Assert.AreEqual("Sergey", await ScalarAsync($"select \"NAME\" from {CaseTable} where id = 1"));
    }


    [TestMethod]
    public async Task UpdateAsync_NotMappedPropertyWithoutColumn_IsIgnored()
    {
        var user = new InsertUserWithNotMapped { Name = "Sergey", Nickname = "serg" };

        var updated = await Context.UpdateAsync(UsersTable, 1, user);

        Assert.AreEqual(1, updated);
        Assert.AreEqual("Sergey", await UserNameAsync(1));
    }

    [TestMethod]
    public async Task UpdateAsync_NotMappedPropertyWithColumn_ColumnKeepsValue()
    {
        var user = new InsertUserWithNotMappedEmail { Name = "Sergey", Email = "new@example.com" };

        await Context.UpdateAsync(UsersTable, 1, user);

        Assert.AreEqual("ivan@example.com", await ScalarAsync($"select email from {UsersTable} where id = 1"));
    }


    [TestMethod]
    public async Task UpdateAsync_ColumnForPropertyMissing_ThrowsInvalidOperationEx()
    {
        var entity = new InsertMissingColumnEntity { Name = "Sergey", Missing = "x" };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.UpdateAsync(UsersTable, 1, entity));
        Assert.AreEqual("Ivan", await UserNameAsync(1));
    }

    [TestMethod]
    public async Task UpdateAsync_ColumnFoundForPropertyWithoutPublicGetter_ThrowsInvalidOperationEx()
    {
        var entity = new InsertSecretEntity { Name = "Sergey", Secret = "new" };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.UpdateAsync(SecretTable, 1, entity));
        Assert.AreEqual("old", await ScalarAsync($"select secret from {SecretTable} where id = 1"));
    }

    [TestMethod]
    public async Task UpdateAsync_CantConvertPropertyValueToColumnType_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.UpdateAsync(TypedTable, 1, new InsertTypedEntity { Age = "abc" }));
        Assert.AreEqual(20, await ScalarAsync($"select age from {TypedTable} where id = 1"));
    }

    [TestMethod]
    public async Task UpdateAsync_NullToNotNullColumn_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.UpdateAsync(UsersTable, 1, new InsertUser { Name = null! }));
        Assert.AreEqual("Ivan", await UserNameAsync(1));
    }

    [TestMethod]
    public async Task UpdateAsync_ColumnIdDoesntExist_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.UpdateAsync(NoIdTable, 1, new InsertUser { Name = "Sergey" }));
        Assert.AreEqual("Ivan", await ScalarAsync($"select name from {NoIdTable} where idd = 1"));
    }


    [TestMethod]
    public async Task UpdateAsync_TableDoesntExist_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.UpdateAsync(NonExistentTable, 1, new InsertUser { Name = "Sergey" }));
    }

    [TestMethod]
    public async Task UpdateAsync_CanceledToken_ThrowsAndDoesNotUpdate()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Context.UpdateAsync(UsersTable, 1, new InsertUser { Name = "Sergey" }, cts.Token));
        Assert.AreEqual("Ivan", await UserNameAsync(1));
    }
}
