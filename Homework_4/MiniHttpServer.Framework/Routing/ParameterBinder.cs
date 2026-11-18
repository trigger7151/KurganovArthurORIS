using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Web;

namespace MiniHttpServer.Framework.Routing;

/// <summary>
/// Ошибка клиента при привязке параметров. RouteHandler отвечает статусом из StatusCode:
/// 400 — неверные или отсутствующие значения, 413 — слишком большое тело запроса.
/// </summary>
public class ParameterBindingException : Exception
{
    public int StatusCode { get; }

    public ParameterBindingException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public ParameterBindingException(string message, Exception innerException, int statusCode = 400)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Собирает значения из HTTP-запроса и превращает их в аргументы метода контроллера.
/// </summary>
public static class ParameterBinder
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string JsonContentType = "application/json";

    /// <summary>
    /// Максимальный размер тела формы или JSON, который читается в память (1 МБ).
    /// </summary>
    public const long MaxBodySize = 15*(1024 * 1024);

    /// <summary>
    /// Точка входа: возвращает массив аргументов для <c>method.Invoke</c>.
    /// </summary>
    public static async Task<object?[]> BindAsync(MethodInfo method, HttpListenerRequest request)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
            return [];   

        NameValueCollection values = await GetRequestParametersAsync(request);
        return GetMethodParameterValues(parameters, values);
    }

    /// <summary>
    /// Собирает параметры запроса в одну коллекцию «имя → значение».
    /// Строка запроса берётся всегда; тело читается только для формы и JSON,
    /// остальные тела (например, картинки) остаются нетронутыми для контроллера.
    /// </summary>
    public static async Task<NameValueCollection> GetRequestParametersAsync(HttpListenerRequest request)
    {
        var values = new NameValueCollection(StringComparer.OrdinalIgnoreCase) { request.QueryString };

        if (!request.HasEntityBody)
            return values;

        bool isForm = HasContentType(request, FormContentType);
        bool isJson = HasContentType(request, JsonContentType);
        if (!isForm && !isJson)
            return values;

        string body = await ReadBodyAsync(request);

        if (isForm)
            values.Add(HttpUtility.ParseQueryString(body));
        else
            AddJsonValues(body, values);

        return values;
    }

    /// <summary>
    /// Читает тело запроса не больше MaxBodySize байт.
    /// Content-Length проверяется заранее, а при чтении считается каждый байт:
    /// клиент может не указать длину (chunked) или указать неправду.
    /// </summary>
    private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        if (request.ContentLength64 > MaxBodySize)
            throw BodyTooLarge();

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await request.InputStream.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > MaxBodySize)
                throw BodyTooLarge();
            buffer.Write(chunk, 0, read);
        }

        return request.ContentEncoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    private static ParameterBindingException BodyTooLarge()
        => new($"Тело запроса больше допустимого размера ({MaxBodySize / 1024} КБ).", 413);

    /// <summary>
    /// Для каждого параметра метода находит значение по имени и приводит к нужному типу.
    /// Порядок элементов массива совпадает с порядком параметров.
    /// </summary>
    public static object?[] GetMethodParameterValues(ParameterInfo[] parameters, NameValueCollection values)
    {
        var args = new object?[parameters.Length];

        foreach (ParameterInfo parameter in parameters)
        {
            string? raw = values[parameter.Name!];
            args[parameter.Position] = raw is null
                ? GetMissingValue(parameter)
                : CastStringToParameterType(raw, parameter);
        }

        return args;
    }

    /// <summary>
    /// Преобразует строку из запроса в тип параметра.
    /// </summary>
    public static object? CastStringToParameterType(string str, ParameterInfo parameter)
    {
        var converter = TypeDescriptor.GetConverter(parameter.ParameterType);
        if (!converter.CanConvertFrom(typeof(string)))
            throw new NotSupportedException(
                $"Тип {parameter.ParameterType.Name} параметра '{parameter.Name}' не поддерживается привязкой.");

        try
        {
            return converter.ConvertFromInvariantString(str);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new ParameterBindingException(
                $"Параметр '{parameter.Name}': значение '{str}' нельзя преобразовать в {parameter.ParameterType.Name}.",
                ex);
        }
    }

    /// <summary>
    /// Что передать, если параметра нет в запросе.
    /// </summary>
    private static object? GetMissingValue(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;                                   

        Type type = parameter.ParameterType;
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null)
            return null;                                                     

        throw new ParameterBindingException($"Не передан обязательный параметр '{parameter.Name}'.");
    }

    /// <summary>
    /// Добавляет поля верхнего уровня JSON-объекта как строки.
    /// Вложенные объекты, массивы и null пропускаются.
    /// </summary>
    private static void AddJsonValues(string body, NameValueCollection values)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new ParameterBindingException("Тело запроса должно быть JSON-объектом.");

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        values.Add(property.Name, property.Value.GetString());
                        break;
                    case JsonValueKind.Number:
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        values.Add(property.Name, property.Value.GetRawText());
                        break;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new ParameterBindingException("Некорректный JSON в теле запроса.", ex);
        }
    }

    private static bool HasContentType(HttpListenerRequest request, string contentType)
        => request.ContentType?.StartsWith(contentType, StringComparison.OrdinalIgnoreCase) == true;
}
