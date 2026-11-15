using System;
using System.Data;
using System.Text.RegularExpressions;
using Npgsql;
using System.Reflection;
namespace MiniHttpServer.ORM;

public partial class OrmContext(string connectionString) : IOrmContext
{
    private string ConnectionString { get; set; } = connectionString;
    
    public async Task<int> DeleteAsync(string tableName, int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string tableName, CancellationToken cancellationToken = default) where T : class, new()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        if (!Regex.IsMatch(tableName, @"^[A-Za-z_][A-Za-z0-9_]*\z"))
            throw new ArgumentException($"incorrect format of the table name: '{tableName}'");
        var command = new NpgsqlCommand($"SELECT * FROM \"{tableName}\"", connection);
        await connection.OpenAsync(cancellationToken);
        NpgsqlDataReader? reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            throw new ArgumentException($"The table doesn't exist: {ex.Message}");
        }
        await using (reader)
        {
            var propertyToColumn = GetPropertyToColumnDictionary<T>(reader);
            var properties = propertyToColumn.Select(pair => pair.Key).ToArray();
            var list = new List<T>{};
            if (!reader.HasRows)
                return list;

            while (await reader.ReadAsync(cancellationToken))
            {
                var instance = Activator.CreateInstance<T>();
                SetInstanceProperties(instance, reader, properties, propertyToColumn);
                list.Add(instance);
            }
            return list;
        }
    }

    private async Task<NpgsqlCommand> GetCommandAllAsync<T>(string tableName)
    {
        var properties = typeof(T).GetProperties();
        await using var connection = new NpgsqlConnection(ConnectionString);
        var command = new NpgsqlCommand($"SELECT * FROM \"{tableName}\"", connection);
        return command;
    }

    private static void SetInstanceProperties(object? instance, NpgsqlDataReader? reader, 
    PropertyInfo[]? properties, Dictionary<PropertyInfo?, int>? propertyToColumn)
    {
        foreach (var property in properties)
        {
            if (property.IsDefined(typeof(NotMappedAttribute)))
                continue;
            if (property.GetSetMethod() == null && !property.IsDefined(typeof(NotMappedAttribute)))
                throw new InvalidOperationException("No public set for mapped property");
            var value = reader.GetValue(propertyToColumn[property]);
            try
            {
                bool acceptsNull = new NullabilityInfoContext().Create(property).WriteState
                   == NullabilityState.Nullable;
                if (reader.IsDBNull(propertyToColumn[property]))
                {
                    bool propertyAcceptsNull = new NullabilityInfoContext().Create(property).WriteState
                   == NullabilityState.Nullable;
                    if (!propertyAcceptsNull)
                        throw new InvalidOperationException($"Tried to assign null value no a non nullable property {property}");
                    property.SetValue(instance, null);
                }
                else
                {
                    property.SetValue(instance, value);
                }
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
    }

    private static Dictionary<PropertyInfo?, int> GetPropertyToColumnDictionary<T>(NpgsqlDataReader? reader)
    {
        var properties = typeof(T).GetProperties();
        var dict = new Dictionary<PropertyInfo?, int>();
        foreach (var property in properties)
        {
            int column;
            try
            {
                if (property.IsDefined(typeof(NotMappedAttribute)))
                    continue;
                column = reader.GetOrdinal(property.Name);
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new InvalidOperationException($"Column for property with public set wasn't found {ex.Message}");
            }  
            dict[property] = column;
        }

        return dict;
    }

    public async Task<int> InsertAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default) where T : class
    {
        throw new NotImplementedException();
    }

    public async Task<int> UpdateAsync<T>(string tableName, int id, T entity, CancellationToken cancellationToken = default) where T : class
    {
        throw new NotImplementedException();
    }
}
