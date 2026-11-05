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
        if (ResolvePath(dataModel, condition) is null)
        {
            return "";
        }
        else if (ResolvePath(dataModel, condition) is true || EvaluateCondition(condition, dataModel))
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

    private static bool EvaluateCondition1(string condition, object model)
    {
        var leftOperand = ConditionReader.GetOperand(condition);
        var conditionAfterOperand = String.Concat(condition.Skip(leftOperand.Length));

        var conditionOperator = ConditionReader.GetOperator(conditionAfterOperand);

        var conditionAfterOperator = String.Concat(conditionAfterOperand.Skip(conditionOperator.Value.Value.Length + conditionOperator.Value.Index));

        var rightOperand = ConditionReader.GetOperand(conditionAfterOperator);


        throw new NotImplementedException();
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
                if (c >= '0' && c <+ '9')
                    throw new TemplateRenderException("");
                else
                    return ResolvePath(model, trimmedOperand) is true;
                
        }
    }

    private static bool EvaluateFullCondition(string leftOperand, string op, string rightOperand, object model)
    {
        var leftOperandObj = (IComparable)CastOperand(leftOperand, model);
        var rightOperandObj = (IComparable)CastOperand(rightOperand, model);

        var result = leftOperandObj.CompareTo(rightOperand);
        throw new NotImplementedException();
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
                if (first == '"')
                    return operand[1..^1];
                else if (first >= '0' && first <= '9')
                    return double.Parse(operand, CultureInfo.InvariantCulture);
                else
                    return ResolvePath(model, operand);
                
        }
    }

    private static bool EvaluateCondition(string condition, object model)
    {
        var leftOperand = FindLeftOperand(condition);
        var op = FindOperator(condition, leftOperand.endIndex + 1);
        if (op is null)
            throw new TemplateRenderException("operator was not found");
        var rightOperand = FindRightOperand(condition, op.Value.op, op.Value.index);
        IComparable operand1;
        IComparable operand2;
        if (leftOperand.type == "string")
            operand1 = leftOperand.operand;
        else 
            operand1 = (IComparable)ResolvePath(model, leftOperand.operand);

        if (rightOperand.type == "string")
            operand2 = rightOperand.operand;
        else 
            operand2 = (IComparable)ResolvePath(model, rightOperand.operand);

        var result = operand1.CompareTo(operand2);
        if (op.Value.op == "<=")
        {
            if (result <= 0)
                return true;
            else
                return false;
        }
        else if (op.Value.op == ">=")
        {
            if (result >= 0)
                return true;
            else
                return false;
        }
        else if (op.Value.op == "<")
        {
            if (result < 0)
                return true;
            else
                return false;
        }
        else if (op.Value.op == ">")
        {
            if (result > 0)
                return true;
            else
                return false;
        }
        else if (op.Value.op == "==")
        {
            if (result == 0)
                return true;
            else
                return false;
        }
        else
        {
            if (result != 0)
                return true;
            else
                return false;
        }
    }

    #region Вспомогательные методы EvaluateCondition(string condition, object model)

    private static (string operand, int endIndex, string type) FindLeftOperand(string condition)
    {
        var firstCharacter = FindFirstNonSpaceCharacter(condition);
        if (firstCharacter == '"')
        {
            var operand = ReadAsString(condition);
            return (operand.operand, operand.endIndex, "string");
        }
        else if (Char.IsLetter(firstCharacter) || firstCharacter == '_')
        {
            var operand = ReadOperandAsText(condition);
            return (operand.operand, operand.endIndex, "property");
        }
        else
            throw new TemplateRenderException("left operand was not found");
    }

    private static (string operand, int endIndex, string type) FindRightOperand(string condition, string op, int index)
    {
        var firstCharacter = FindFirstNonSpaceCharacter(String.Concat(condition.Skip(index + op.Length)));
        if (firstCharacter == '"')
        {
            var operand = ReadAsString(String.Concat(condition.Skip(index + op.Length)));
            return (operand.operand, operand.endIndex, "string");
        }
        else if (Char.IsLetter(firstCharacter) || firstCharacter == '_')
        {
            var operand = ReadOperandAsText(String.Concat(condition.Skip(index + op.Length)));
            return (operand.operand, operand.endIndex, "property");
        }
        else
            throw new TemplateRenderException("right operand was not found");
        throw new NotImplementedException();
    }

    // startIndex - индекс после левого операнда
    private static (string op, int index)? FindOperator(string condition, int startIndex)
    {
        // if (startIndex == -1)
        //     throw new TemplateRenderException("");
        
        var trimmedCondition = string.Concat(condition.Skip(startIndex));
        for (int i = startIndex; i != condition.Length; i++)
        {
            switch (condition[i])
            {
                case ' ':
                    continue;
                case '<':
                    if (condition.IndexOf("<=") == i)
                        return ("<=", i);
                    else
                        return ("<", i);
                case '>':
                    if (condition.IndexOf(">=") == i)
                        return (">=", i);
                    else
                        return (">", i);
                case '=':
                    if (condition.IndexOf("==") == i)
                        return ("==", i);
                    else
                        throw new TemplateRenderException("");
                case '!':
                    if (condition.IndexOf("!=") == i)
                        return ("!=", i);
                    else
                        throw new TemplateRenderException("");
                default:
                    throw new TemplateRenderException("");
            }
        }
        return null;
    }

    private static (string operand, int endIndex) ReadAsString(string condition)
    {
        var trimmedCondition = condition.TrimStart().TrimStart('"') + " ";
        var count = condition.Length - trimmedCondition.Length;
        var foundStartingMark  = false;
        var foundEscapingMark = false;
        var builder = new StringBuilder();
        foreach (var c in trimmedCondition)
        {
            if (c == '"' && !foundEscapingMark)
                foundEscapingMark = true;
            else if (foundEscapingMark && c == '"')
                foundEscapingMark = false;
            else if (foundEscapingMark && c != '"')
                return (builder.ToString(), count + builder.Length);
            builder.Append(c);
        }
        if (builder.Length == 0)
            throw new TemplateRenderException("Missing operand");
        throw new TemplateRenderException("Missing closing quote mark");
    }


    private static (string operand, int endIndex) ReadOperandAsText(string condition)
    {
        var trimmedCondition = condition.TrimStart();
        var count = condition.Length - trimmedCondition.Length;
        var builder = new StringBuilder();
        foreach (var c in condition)
        {
            if (!(Char.IsLetter(c) || c == '_' || Char.IsNumber(c) || c == '.'))
                break;
            builder.Append(c);
        }
        if (builder.Length == 0)
            throw new TemplateRenderException("Missing operand");
        return (builder.ToString(), count + builder.Length - 1);
    }

    private static int GetSubstringOccurrences(string str, string substring, int startIndex)
    {
        var count = 0;
        var index = str.IndexOf(substring, startIndex);
        if (index == -1)
            return count;
        while(index != -1)
        {
            index = str.IndexOf(substring, index + substring.Length);
            count++;
        }
        return count;
    }

    private static char FindFirstNonSpaceCharacter(string condition)
    {
        var trimmedCondition = condition.TrimStart();
        if (trimmedCondition.Length == 0)
            throw new TemplateRenderException("empty condition");
        return condition.TrimStart().First();
    }


    #endregion

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

public class ConditionReader
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
        throw new NotImplementedException();
    }

}