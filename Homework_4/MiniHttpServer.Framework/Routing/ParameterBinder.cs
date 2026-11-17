using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Web;

namespace MiniHttpServer.Framework.Routing;


public class ParameterBindingException : Exception
{
    public ParameterBindingException(string message)
        : base(message) { }

    public ParameterBindingException(string message, Exception innerException)
        : base(message, innerException) { }
}


public static class ParameterBinder
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string JsonContentType = "application/json";


    public static async Task<object?[]> BindAsync(MethodInfo method, HttpListenerRequest request)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
            return [];   

        NameValueCollection values = await GetRequestParametersAsync(request);
        return GetMethodParameterValues(parameters, values);
    }


    private static async Task<NameValueCollection> GetRequestParametersAsync(HttpListenerRequest request)
    {
        var values = new NameValueCollection(StringComparer.OrdinalIgnoreCase) { request.QueryString };

        if (!request.HasEntityBody)
            return values;

        bool isForm = HasContentType(request, FormContentType);
        bool isJson = HasContentType(request, JsonContentType);
        if (!isForm && !isJson)
            return values;

        string body;
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            body = await reader.ReadToEndAsync();

        if (isForm)
            values.Add(HttpUtility.ParseQueryString(body));
        else
            AddJsonValues(body, values);

        return values;
    }


    private static object?[] GetMethodParameterValues(ParameterInfo[] parameters, NameValueCollection values)
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


    private static object? CastStringToParameterType(string str, ParameterInfo parameter)
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

    private static object? GetMissingValue(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;                          

        Type type = parameter.ParameterType;
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null)
            return null;                                                     

        throw new ParameterBindingException($"Не передан обязательный параметр '{parameter.Name}'.");
    }

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
