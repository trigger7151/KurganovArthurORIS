using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

internal record IfBlockPositions
{
    public int IfIndex{get;init;}
    public int ClosingBracketIndex{get;init;}
    public int EndIfIndex{get;init;}
    public int ElseIndex{get;init;}

    public IfBlockPositions(int ifIndex, int closingBracketsIndex, int endIfIndex, int elseIndex)
    {
        IfIndex = ifIndex;
        ClosingBracketIndex = closingBracketsIndex;
        EndIfIndex = endIfIndex;
        ElseIndex = elseIndex;
    }
}

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    private static readonly Regex VariableRegex = new(@"\$\{([a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)\}");
    private const string ifStart = "$if(";
    private const string endIf = "$endif";
    private const string Else = "$else";

    private static object? ResolvePath(object? model, string path)
    {
        var properties = path.Split('.');
        object? value = model;
        for (int i = 1; i < properties.Length; i++)
        {
            if (value is null)
            {
                return null;
            }
            var propertyType = value.GetType();
            var propertyInfo = propertyType.GetProperty(properties[i]);
            if (propertyInfo is null)
            {
                throw new TemplateRenderException(
                    $"Свойство '{properties[i]}' не найдено в типе {propertyType.Name} (путь {path})");
            }
            value = propertyInfo.GetValue(value);
        }
        return value;
    }

    private static string RenderVariables(string htmlTemplate, object? dataModel)
    {
        var result = VariableRegex.Replace(htmlTemplate, (match) => 
        {
            var variable = match.Groups[1].Value;
            var value = ResolvePath(dataModel, variable);
            return value?.ToString() ?? string.Empty;
        });
        return result;
    }

    private static IfBlockPositions GetIfPositions(string htmlTemplate)
    {
        var ifIndex = htmlTemplate.IndexOf(ifStart);
        if (ifIndex == -1)
            return new (-1, -1, -1, -1);
        var closingBracketIndex = htmlTemplate.IndexOf(")", ifIndex);
        if (closingBracketIndex == -1)
            throw new TemplateRenderException("closingBracket for $if() is missing");
        var endIfIndex = htmlTemplate.IndexOf(endIf, closingBracketIndex);
        if (endIfIndex == -1)
            throw new TemplateRenderException("closing endIf is missing");
        var elseIndex = htmlTemplate.IndexOf(Else, closingBracketIndex + 1, endIfIndex - closingBracketIndex - 1);
        return new (ifIndex, closingBracketIndex, endIfIndex, elseIndex);
    }

    private static string GetBody(string htmlTemplate, object dataModel, IfBlockPositions positions)
    {
        if (positions.IfIndex == -1)
            return "";
        var condition = htmlTemplate[(positions.IfIndex + ifStart.Length)..positions.ClosingBracketIndex];
        string body;
        if (ResolvePath(dataModel, condition) is true)
        {
            var bodyEndIndex = (new int[]{positions.ElseIndex, positions.EndIfIndex}).Where(num => num > -1).Min();
            body = htmlTemplate[(positions.ClosingBracketIndex + ")".Length)..bodyEndIndex];
        }
        else
        {
            if (positions.ElseIndex == -1)
                body = "";
            else
                body = htmlTemplate[(positions.ElseIndex + Else.Length)..positions.EndIfIndex];
        }
        return body;
    }

    private static string ResolveIfStatement(string htmlTemplate, object dataModel)
    {
        var positions = GetIfPositions(htmlTemplate);
        string body = GetBody(htmlTemplate, dataModel, positions);
        if (positions.IfIndex == -1)
            return htmlTemplate;
        return htmlTemplate[0..positions.IfIndex] + body + htmlTemplate[(positions.EndIfIndex + endIf.Length)..htmlTemplate.Length];
    }

    private static bool EvaluateCondition(string condition, object model)
    {
        
        throw new NotImplementedException();
    }

    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        var htmlText = ResolveIfStatement(htmlTemplate, dataModel);
        return RenderVariables(htmlText, dataModel);
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        throw new NotImplementedException();
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        throw new NotImplementedException();
    }
}
