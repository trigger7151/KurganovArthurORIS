using System;
using System.Data;
using System.Text.RegularExpressions;
using Npgsql;
using System.Reflection;
using System.Text;
namespace MiniHttpServer.ORM;

/// <summary>
/// Реализация <see cref="IOrmContext"/> для PostgreSQL (Npgsql). Свойства модели сопоставляются
/// со столбцами по имени без учёта регистра; свойства с <see cref="NotMappedAttribute"/> пропускаются.
/// </summary>
public partial class OrmContext : IOrmContext
{
    /// <summary>
    /// Строка подключения к базе данных.
    /// </summary>
    private string ConnectionString { get; set; }

    /// <summary>
    /// Создаёт контекст для указанной базы данных.
    /// </summary>
    /// <param name="connectionString">Строка подключения PostgreSQL.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> равна null.</exception>
    public OrmContext(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ConnectionString = connectionString;
    }
    
    /// <inheritdoc />
    public async Task<int> DeleteAsync(string tableName, int id, CancellationToken cancellationToken = default)
    {
        await HandleTableNameExceptionsAsync(tableName);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await using var command = new NpgsqlCommand($"delete FROM \"{tableName}\" where Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        await connection.OpenAsync(cancellationToken);
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            throw new ArgumentException($"Table '{tableName}' doesn't exist.", nameof(tableName), ex);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            throw new InvalidOperationException($"No column id in '{tableName}' table.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string tableName, CancellationToken cancellationToken = default) where T : class, new()
    {
        await HandleTableNameExceptionsAsync(tableName);
        await using var connection = new NpgsqlConnection(ConnectionString);
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

    #region вспомогательные методы GetAllAsync()
    /// <summary>
    /// Заполняет свойства объекта значениями текущей строки результата.
    /// </summary>
    /// <param name="instance">Заполняемый объект.</param>
    /// <param name="reader">Читатель, стоящий на текущей строке.</param>
    /// <param name="properties">Сопоставленные свойства.</param>
    /// <param name="propertyToColumn">Номер столбца для каждого свойства.</param>
    /// <exception cref="InvalidOperationException">У свойства нет публичного set, null записывается в не-nullable свойство или тип значения не подходит.</exception>
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

    /// <summary>
    /// Сопоставляет свойства модели с номерами столбцов результата запроса.
    /// </summary>
    /// <typeparam name="T">Тип модели.</typeparam>
    /// <param name="reader">Читатель результата запроса.</param>
    /// <returns>Номер столбца для каждого сопоставленного свойства.</returns>
    /// <exception cref="InvalidOperationException">Для свойства не найден столбец.</exception>
    private static Dictionary<PropertyInfo?, int> GetPropertyToColumnDictionary<T>(NpgsqlDataReader? reader)
    {
        var properties = typeof(T).GetProperties()
        .Where(property => !property.IsDefined(typeof(NotMappedAttribute)));
        var dict = new Dictionary<PropertyInfo?, int>();
        foreach (var property in properties)
        {
            int column;
            try
            {
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

    #endregion
    /// <summary>
    /// Проверяет имя таблицы перед подстановкой в SQL: только латиница, цифры и подчёркивание,
    /// не длиннее 63 символов. Защищает от SQL-инъекции через имя таблицы.
    /// </summary>
    /// <param name="tableName">Имя таблицы.</param>
    /// <exception cref="ArgumentNullException">Имя таблицы равно null.</exception>
    /// <exception cref="ArgumentException">Имя таблицы имеет недопустимый формат или длину.</exception>
    private static async Task HandleTableNameExceptionsAsync(string tableName)
    {
        ArgumentNullException.ThrowIfNull(tableName);
        if (!Regex.IsMatch(tableName, @"^[A-Za-z_][A-Za-z0-9_]*\z"))
            throw new ArgumentException($"incorrect format of the table name: '{tableName}'");
        if (tableName.Length > 63)
            throw new ArgumentException($"table name {tableName} exceeds maximum length");
    }

    /// <inheritdoc />
    public async Task<int> InsertAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default) where T : class
    {
        await HandleTableNameExceptionsAsync(tableName);
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var tableColumns = await GetTableColumnsAsync(connection, tableName, cancellationToken);
        var propertyToColumn = GetPropertyToColumnPairs<T>(tableColumns, tableName);

        await using var command = new NpgsqlCommand { Connection = connection };
        var columns = new StringBuilder();
        var values = new StringBuilder();
        foreach (var (property, column) in propertyToColumn)
        {
            command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(entity) ?? DBNull.Value);
            columns.Append($"\"{column}\", ");
            values.Append($"@{property.Name}, ");
        }

        if (columns.Length == 0)
        {
            command.CommandText = $"insert into \"{tableName}\" default values";
        }
        else
        {
            columns.Length -= 2;
            values.Length -= 2;
            command.CommandText = $"insert into \"{tableName}\" ({columns}) values ({values})";
        }

        return await ExecuteWriteCommandAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> UpdateAsync<T>(string tableName, int id, T entity, CancellationToken cancellationToken = default) where T : class
    {
        await HandleTableNameExceptionsAsync(tableName);
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var tableColumns = await GetTableColumnsAsync(connection, tableName, cancellationToken);
        if (!tableColumns.TryGetValue("Id", out var idColumn))
            throw new InvalidOperationException($"No column id in '{tableName}' table.");
        var propertyToColumn = GetPropertyToColumnPairs<T>(tableColumns, tableName);

        await using var command = new NpgsqlCommand { Connection = connection };
        var set = new StringBuilder();
        foreach (var (property, column) in propertyToColumn)
        {
            command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(entity) ?? DBNull.Value);
            set.Append($"\"{column}\" = @{property.Name}, ");
        }

        if (set.Length == 0)
            set.Append($"\"{idColumn}\" = \"{idColumn}\"");
        else
            set.Length -= 2;

        command.Parameters.AddWithValue("@id", id);
        command.CommandText = $"update \"{tableName}\" set {set} where \"{idColumn}\" = @id";

        return await ExecuteWriteCommandAsync(command, cancellationToken);
    }

    #region вспомогательные методы InsertAsync() и UpdateAsync()
    /// <summary>
    /// Получает имена столбцов таблицы запросом без строк.
    /// </summary>
    /// <param name="connection">Открытое подключение.</param>
    /// <param name="tableName">Имя таблицы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Имена столбцов (ключ без учёта регистра → точное имя).</returns>
    /// <exception cref="ArgumentException">Таблица не существует.</exception>
    private static async Task<Dictionary<string, string>> GetTableColumnsAsync(
        NpgsqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"select * from \"{tableName}\" where false", connection);
        NpgsqlDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            throw new ArgumentException($"Table '{tableName}' doesn't exist.", nameof(tableName), ex);
        }

        await using (reader)
        {
            var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                columns[reader.GetName(i)] = reader.GetName(i);
            return columns;
        }
    }

    /// <summary>
    /// Сопоставляет записываемые свойства модели (кроме Id и <see cref="NotMappedAttribute"/>) со столбцами таблицы.
    /// </summary>
    /// <typeparam name="T">Тип модели.</typeparam>
    /// <param name="tableColumns">Столбцы таблицы.</param>
    /// <param name="tableName">Имя таблицы (для сообщений об ошибках).</param>
    /// <returns>Пары «свойство — столбец».</returns>
    /// <exception cref="InvalidOperationException">Для свойства нет столбца или у свойства нет публичного get.</exception>
    private static List<(PropertyInfo Property, string Column)> GetPropertyToColumnPairs<T>(
        Dictionary<string, string> tableColumns, string tableName)
    {
        var properties = typeof(T).GetProperties()
            .Where(property => !property.IsDefined(typeof(NotMappedAttribute)))
            .Where(property => !property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        var pairs = new List<(PropertyInfo Property, string Column)>();
        foreach (var property in properties)
        {
            if (!tableColumns.TryGetValue(property.Name, out var column))
                throw new InvalidOperationException($"Column for mapped property '{property.Name}' wasn't found in table '{tableName}'");
            if (property.GetGetMethod() == null)
                throw new InvalidOperationException($"No public get for mapped property '{property.Name}'");
            pairs.Add((property, column));
        }

        return pairs;
    }

    /// <summary>
    /// Выполняет команду записи и переводит ошибки PostgreSQL в понятные исключения.
    /// </summary>
    /// <param name="command">Команда INSERT или UPDATE.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Количество затронутых строк.</returns>
    /// <exception cref="InvalidOperationException">Запись null в NOT NULL-столбец или несовместимый тип значения.</exception>
    private static async Task<int> ExecuteWriteCommandAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.NotNullViolation)
        {
            throw new InvalidOperationException($"Tried to write null into a not null column '{ex.ColumnName}'", ex);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DatatypeMismatch)
        {
            throw new InvalidOperationException($"Property value can't be converted to the column type: {ex.MessageText}", ex);
        }
    }
    #endregion
}
