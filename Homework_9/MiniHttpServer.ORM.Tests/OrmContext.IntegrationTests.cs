using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;
using MiniHttpServer.ORM.Tests.Entities;
using Npgsql;
namespace MiniHttpServer.ORM.Tests;

[TestClass]
[DoNotParallelize]
public class OrmContextIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=OrisTestdb;Username=OrisTestUser;Password=bwydf175";
    private const string fewerPropertiesTable = "fewerproperties";
    private const string hasColumnForNotMapped = "hascolumnfornotmapped";
    private const string incompatiblePropertyAndColumnType = "incompatiblepropertyandcolumntype";
    private const string columnForPropertyNoPublicSet = "columnforpropertynopublicset";
    private const string missingColumnTable = "missingcolumn";
    private const string caseTable = "differentcase";
    private const string nullTable = "nullvalues";
    private const string productsTable = "products";
    private OrmContext Context {get; set;}

    [TestInitialize]
    public void SetUp()
    {
        Context = new OrmContext(ConnectionString);
    }

    [ClassInitialize]
    public static async Task DbInitialize(TestContext context)
    {
        await ExecuteNonQueryAsync($"""
        drop table if exists "{new string('a', 63)}";
        create table "{new string('a', 63)}" 
        (
            Id serial primary key
        );
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {fewerPropertiesTable};
        create table {fewerPropertiesTable}
        (
            Id serial primary key,
            Column2 integer,
            Column1 integer
        );
        insert into {fewerPropertiesTable}
        (Id, Column2, Column1)
        values (1, 1, 2);
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {hasColumnForNotMapped};
        create table {hasColumnForNotMapped}
        (
            Id serial primary key,
            NotMapped int
        );
        insert into {hasColumnForNotMapped} (NotMapped)
        values (67);
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {incompatiblePropertyAndColumnType};
        create table {incompatiblePropertyAndColumnType}
        (
            Id serial primary key,
            Incompatible varchar(20)
        );
        insert into {incompatiblePropertyAndColumnType} (Incompatible)
        values ('abc');
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {columnForPropertyNoPublicSet};
        create table {columnForPropertyNoPublicSet}
        (
            Id serial primary key,
            NoPublicSet int,
            NoSetter int
        );
        insert into {columnForPropertyNoPublicSet} (NoPublicSet, NoSetter)
        values (52, 13);
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {missingColumnTable};
        create table {missingColumnTable}
        (
            Id serial primary key
        );
        insert into {missingColumnTable} default values;
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {caseTable};
        create table {caseTable}
        (
            Id serial primary key,
            "NAME" varchar(100)
        );
        insert into {caseTable} ("NAME") values ('Ivan');
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {nullTable};
        create table {nullTable}
        (
            Id serial primary key,
            Age int,
            Email varchar(100)
        );
        insert into {nullTable} (Age, Email) values (null, null);
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {productsTable};
        create table {productsTable}
        (
            Id serial primary key,
            Name varchar(100) not null,
            Price numeric(10, 2) not null,
            InStock boolean not null
        );
        insert into {productsTable} (Name, Price, InStock)
        values ('Keyboard', 2500.50, true),
               ('Mouse', 900.00, false);
        """);
    }

    [ClassCleanup]
    public static async Task DbCleanup()
    {
        await ExecuteNonQueryAsync($"""
        drop table if exists "{new string('a', 63)}";
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {fewerPropertiesTable};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {hasColumnForNotMapped};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {incompatiblePropertyAndColumnType};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {columnForPropertyNoPublicSet};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {missingColumnTable};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {caseTable};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {nullTable};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {productsTable};
        """);
    }

    private static async Task<int> ExecuteNonQueryAsync(string query)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await using var command = new NpgsqlCommand(query, connection);
        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }



    [TestMethod] 
    public async Task GetAllAsync_TableName63Symbols_CorrectResult()
    {
        var result = await Context.GetAllAsync<LongNameEntity>(new string('a', 63));
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnCountGreaterThanPropertyCount_CorrectResult()
    {
        var result = await Context.GetAllAsync<FewerProperties>(fewerPropertiesTable);

        Assert.HasCount(1, result);            
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(2, result[0].Column1); 
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnForNotMappedPropertyFound_IgnoresColumn()
    {
        var result = await Context.GetAllAsync<HasColumnForNotMapped>(hasColumnForNotMapped);

        Assert.HasCount(1, result);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(-1, result[0].NotMapped);
    }

    [TestMethod]
    public async Task GetAllAsync_CantCastColumnValueToPropertyType_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Context
        .GetAllAsync<IncompatiblePropertyAndColumnType>(incompatiblePropertyAndColumnType));
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnFoundForPrivateSetter_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.GetAllAsync<PrivateSetterEntity>(columnForPropertyNoPublicSet));
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnFoundForGetOnlyProperty_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.GetAllAsync<GetOnlyEntity>(columnForPropertyNoPublicSet));
    }

    [TestMethod]
    public async Task GetAllAsync_TableDoesntExist_ThrowsArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.GetAllAsync<LongNameEntity>("fgwvrqdnqmsvk"));
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnForPropertyMissing_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.GetAllAsync<MissingColumnEntity>(missingColumnTable));
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnForNotMappedPropertyMissing_CorrectResult()
    {
        var result = await Context.GetAllAsync<NotMappedWithoutColumnEntity>(missingColumnTable);

        Assert.HasCount(1, result);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual("default", result[0].Missing);
    }

    [TestMethod]
    public async Task GetAllAsync_PropertyAndColumnDifferentCase_MatchesIgnoringCase()
    {
        var result = await Context.GetAllAsync<CaseEntity>(caseTable);

        Assert.HasCount(1, result);
        Assert.AreEqual("Ivan", result[0].Name);
    }

    [TestMethod]
    public async Task GetAllAsync_NullValueToNullableProperty_SetsNull()
    {
        var result = await Context.GetAllAsync<NullableEntity>(nullTable);

        Assert.HasCount(1, result);
        Assert.IsNull(result[0].Age);
        Assert.IsNull(result[0].Email);
    }

    [TestMethod]
    public async Task GetAllAsync_NullValueToNonNullableProperty_ThrowsInvalidOperationEx()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => Context.GetAllAsync<NonNullableEntity>(nullTable));
    }

    [TestMethod]
    public async Task GetAllAsync_CanceledToken_ThrowsOperationCanceledEx()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Context.GetAllAsync<FewerProperties>(fewerPropertiesTable, cts.Token));
    }

    [TestMethod]
    public async Task GetAllAsync_SeveralRowsAndTypes_ReturnsAllRowsWithCorrectValues()
    {
        var result = await Context.GetAllAsync<Product>(productsTable);

        Assert.HasCount(2, result);

        var keyboard = result.Single(p => p.Name == "Keyboard");
        Assert.AreEqual(2500.50m, keyboard.Price);
        Assert.IsTrue(keyboard.InStock);

        var mouse = result.Single(p => p.Name == "Mouse");
        Assert.AreEqual(900.00m, mouse.Price);
        Assert.IsFalse(mouse.InStock);

        Assert.AreNotEqual(keyboard.Id, mouse.Id);
    }
}
