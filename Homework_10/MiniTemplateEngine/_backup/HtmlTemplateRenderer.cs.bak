using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

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
    private const double Epsilon = 1e-9;

    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        var htmlText = ResolveIfStatement(htmlTemplate, dataModel);
        return RenderVariables(htmlText, dataModel);
    }

    #region Вспомогательные методы для RenderFromString(string htmlTemplate, object dataModel)

    private static string ResolveIfStatement(string htmlTemplate, object dataModel)
    {
        var positions = GetIfPositions(htmlTemplate);
        string body = GetBody(htmlTemplate, dataModel, positions);
        if (positions.IfIndex == -1)
            return htmlTemplate;
        return htmlTemplate[0..positions.IfIndex] + body + htmlTemplate[(positions.EndIfIndex + endIf.Length)..htmlTemplate.Length];
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

        if (EvaluateCondition(condition, dataModel))
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

    private static bool EvaluateCondition(string condition, object model)
    {
        var leftOperand = ConditionReader.GetOperand(condition);
        var trimmedLeftOperand = leftOperand.TrimStart();
        var conditionAfterOperand = String.Concat(condition.Skip(leftOperand.Length));

        var conditionOperator = ConditionReader.GetOperator(conditionAfterOperand);
        if (conditionOperator is null)
            return EvaluateOperandAsCondition(trimmedLeftOperand, model);
        var conditionAfterOperator = String.Concat(conditionAfterOperand.Skip(conditionOperator.Value.Value.Length + conditionOperator.Value.Index));

        var rightOperand = ConditionReader.GetOperand(conditionAfterOperator);
        var trimmedRightOperand = rightOperand.TrimStart();

        return EvaluateFullCondition(trimmedLeftOperand, conditionOperator.Value.Value, trimmedRightOperand, model);
    }

    private static bool EvaluateOperandAsCondition(string operand, object model)
    {
        var trimmedOperand = operand.TrimStart();
        switch (trimmedOperand)
        {
            case "true":
                return true;

            case "false" or "null":
                return false;

            default:
                var c =  trimmedOperand.First();
                if (c >= '0' && c <= '9')
                    throw new TemplateRenderException("");
                else
                {
                    var resolved = ResolvePath(model, trimmedOperand);
                    if (resolved is not true && resolved is not false && resolved is not null)
                        throw new TemplateRenderException("");
                    return resolved is true;
                }
                
        }
    }

    private static bool EvaluateFullCondition(string leftOperand, string op, string rightOperand, object model)
    {
        IComparable leftOperandObj;
        IComparable rightOperandObj;
        try
        {
            leftOperandObj = (IComparable)CastOperand(leftOperand, model);
            rightOperandObj = (IComparable)CastOperand(rightOperand, model);
        }
        catch (InvalidCastException ex)
        {
            throw new TemplateRenderException(ex.Message);
        }

        if (leftOperandObj is null || rightOperandObj is null)
        {
            return op switch
            {
                "==" => Object.Equals(leftOperandObj, rightOperandObj),
                "!=" => !Object.Equals(leftOperandObj, rightOperandObj),
                ">" or "<" or ">=" or "<=" => false,
                _ => throw new TemplateRenderException(""),
            };
        }
        if (leftOperandObj.GetType() != rightOperandObj.GetType())
            throw new TemplateRenderException("");

        if (leftOperandObj is double a && rightOperandObj is double b)
            return CompareDoubles(a, b, op);
            
        var result = leftOperandObj.CompareTo(rightOperandObj);

        return op switch
        {
            ">=" => result >= 0,
            "<=" => result <= 0,
            ">" => result > 0,
            "<" => result < 0,
            "==" => result == 0,
            "!=" => result != 0,
            _ => throw new TemplateRenderException(""),
        };
    }

    private static bool CompareDoubles(double a, double b, string op)
    {
        bool equal = Math.Abs(a - b) < Epsilon;

        return op switch
        {
            "==" => equal,
            "!=" => !equal,
            ">=" => a > b || equal,
            "<=" => a < b || equal,
            ">"  => a > b && !equal,
            "<"  => a < b && !equal,
            _ => throw new TemplateRenderException($"Unknown operator '{op}'"),
        };
    }
    private static bool IsNumber(object obj)
    { 
        return obj is
        sbyte or byte or short or ushort or int or uint or
        long or ulong or float or double or decimal;
    }

    private static object CastOperand(string operand, object model)
    {

        switch (operand)
        {
            case "null":
                return null;
            case "true":
                return true;
            case "false":
                return false;
            default:
                var first = operand.First();
                object? resolved;
                if (first == '"')
                    return operand[1..^1];
                else if (first >= '0' && first <= '9')
                    return double.Parse(operand, CultureInfo.InvariantCulture);
                else if (IsNumber(resolved = ResolvePath(model, operand)))
                    return Convert.ToDouble(resolved);
                else
                    return resolved;
                
        }
    }

    private static object ResolvePath(object? model, string path)
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

    #endregion

    public string RenderFromFile(string filePath, object dataModel)
    {
        throw new NotImplementedException();
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        throw new NotImplementedException();
    }
}

internal class ConditionReader
{
    public static string GetOperand(string str)
    {
        var reader = new OperandReader();
        // выбрасывает исключение если не нашел операнд
        return reader.ProcessWord(str);
    }

    // возвращает null если не нашел оператор
    public static (string Value, int Index)? GetOperator(string str)
    {
        for (int i = 0; i != str.Length; i++)
        {
            switch (str[i])
            {
                case ' ':
                    continue;
                case '<':
                    if (str.IndexOf("<=") == i)
                        return ("<=", i);
                    else
                        return ("<", i);
                case '>':
                    if (str.IndexOf(">=") == i)
                        return (">=", i);
                    else
                        return (">", i);
                case '=':
                    if (str.IndexOf("==") == i)
                        return ("==", i);
                    else
                        throw new TemplateRenderException("");
                case '!':
                    if (str.IndexOf("!=") == i)
                        return ("!=", i);
                    else
                        throw new TemplateRenderException("");
                default:
                    throw new TemplateRenderException("");
            }
        }
        return null;
    }

}