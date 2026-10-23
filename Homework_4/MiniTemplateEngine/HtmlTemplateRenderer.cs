using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    private static readonly Regex VariableRegex = new(@"\$\{([a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)\}");
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        var result = VariableRegex.Replace(htmlTemplate, (match) => 
        {
            var variable = match.Groups[1].Value;
            var properties = variable.Split('.');
            object? value = dataModel;
            for (int i = 1; i < properties.Length; i++)
            {
                if (value is null)
                {
                    return string.Empty;
                }
                var propertyType = value.GetType();
                var propertyInfo = propertyType.GetProperty(properties[i]);
                if (propertyInfo is null)
                {
                    throw new TemplateRenderException(
                        $"Свойство '{properties[i]}' не найдено в типе {propertyType.Name} (переменная ${{{variable}}})");
                }
                value = propertyInfo.GetValue(value);
            }
            return value?.ToString() ?? string.Empty;
        });
        return result;
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        var a = 3;
        object t = new object();
        object aobj = a;
        var b = aobj > t;
        throw new NotImplementedException();
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        throw new NotImplementedException();
    }
}
