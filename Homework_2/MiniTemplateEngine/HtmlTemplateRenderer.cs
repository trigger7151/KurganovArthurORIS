using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

/// <summary>
/// Контекст рендеринга: корневая модель и переменные, объявленные циклами $foreach.
/// </summary>
internal record Context(object? Model, IReadOnlyDictionary<string, object?> Vars)
{
    public static Context Root(object? model) => new(model, new Dictionary<string, object?>());

    // Возвращает новый контекст с добавленной переменной цикла (старый не меняется).
    public Context With(string name, object? value)
    {
        var vars = new Dictionary<string, object?>(Vars) { [name] = value };
        return this with { Vars = vars };
    }
}

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    private static readonly Regex VariableRegex = new(@"\$\{([a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)\}");
    private static readonly Regex ForeachHeaderRegex =
        new(@"^\s*var\s+([a-zA-Z_]\w*)\s+in\s+([a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)\s*$");

    private const string IfStart = "$if(";
    private const string Else = "$else";
    private const string EndIf = "$endif";
    private const string ForeachStart = "$foreach(";
    private const string EndFor = "$endfor";
    private const double Epsilon = 1e-9;

    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        return Render(htmlTemplate, Context.Root(dataModel));
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        return RenderFromString(File.ReadAllText(filePath), dataModel);
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(outputFilePath);
        var result = RenderFromFile(inputFilePath, dataModel);
        File.WriteAllText(outputFilePath, result);
        return result;
    }

    #region Разбор блоков $if / $foreach

    /// <summary>
    /// Проходит шаблон слева направо: обычный текст выводится с подстановкой ${...},
    /// а каждый блок $if/$foreach обрабатывается отдельно, и его тело рендерится
    /// этим же методом рекурсивно. Так работают и несколько блоков подряд, и вложенность.
    /// </summary>
    private static string Render(string template, Context ctx)
    {
        var sb = new StringBuilder();
        int pos = 0;
        while (true)
        {
            int ifPos = template.IndexOf(IfStart, pos, StringComparison.Ordinal); 
            int forPos = template.IndexOf(ForeachStart, pos, StringComparison.Ordinal);
            int next = ifPos == -1 ? forPos
                     : forPos == -1 ? ifPos
                     : Math.Min(ifPos, forPos);

            if (next == -1)
            {
                sb.Append(RenderVariables(template[pos..], ctx));
                break;
            }

            sb.Append(RenderVariables(template[pos..next], ctx));
            pos = next == ifPos
                ? RenderIf(template, next, ctx, sb)
                : RenderForeach(template, next, ctx, sb);
        }
        return sb.ToString();
    }

    // Рендерит блок $if, начинающийся с позиции start; возвращает позицию сразу после $endif.
    private static int RenderIf(string template, int start, Context ctx, StringBuilder sb)
    {
        int closeBracket = template.IndexOf(')', start);
        if (closeBracket == -1)
            throw new TemplateRenderException("closingBracket for $if() is missing");

        int endIf = FindMatching(template, closeBracket + 1, IfStart, EndIf);
        int elsePos = FindElse(template, closeBracket + 1, endIf);

        var condition = template[(start + IfStart.Length)..closeBracket];
        string body;
        if (EvaluateCondition(condition, ctx))
            body = template[(closeBracket + 1)..(elsePos == -1 ? endIf : elsePos)];
        else
            body = elsePos == -1 ? "" : template[(elsePos + Else.Length)..endIf];

        sb.Append(Render(body, ctx));
        return endIf + EndIf.Length;
    }

    // Рендерит блок $foreach, начинающийся с позиции start; возвращает позицию сразу после $endfor.
    private static int RenderForeach(string template, int start, Context ctx, StringBuilder sb)
    {
        int closeBracket = template.IndexOf(')', start);
        if (closeBracket == -1)
            throw new TemplateRenderException("closingBracket for $foreach() is missing");

        int endFor = FindMatching(template, closeBracket + 1, ForeachStart, EndFor);

        var header = template[(start + ForeachStart.Length)..closeBracket];
        var match = ForeachHeaderRegex.Match(header);
        if (!match.Success)
            throw new TemplateRenderException($"Неверный заголовок $foreach({header})");

        var itemName = match.Groups[1].Value;
        var collectionPath = match.Groups[2].Value;
        var collection = ResolvePath(ctx, collectionPath);

        var body = template[(closeBracket + 1)..endFor];
        if (collection is null)
            return endFor + EndFor.Length;   // нет коллекции — нечего выводить
        if (collection is string || collection is not IEnumerable items)
            throw new TemplateRenderException($"'{collectionPath}' не является коллекцией");

        foreach (var item in items)
            sb.Append(Render(body, ctx.With(itemName, item)));

        return endFor + EndFor.Length;
    }

    /// <summary>
    /// Ищет закрывающий тег, парный открывающему, с учётом вложенных блоков того же вида.
    /// </summary>
    private static int FindMatching(string template, int from, string open, string close)
    {
        int depth = 0;
        for (int i = from; i < template.Length; i++)
        {
            var rest = template.AsSpan(i);
            if (rest.StartsWith(open, StringComparison.Ordinal))
            {
                depth++;
                i += open.Length - 1;
            }
            else if (rest.StartsWith(close, StringComparison.Ordinal))
            {
                if (depth == 0)
                    return i;
                depth--;
                i += close.Length - 1;
            }
        }
        throw new TemplateRenderException($"closing {close} is missing");
    }

    /// <summary>
    /// Ищет $else, относящийся к текущему $if (на глубине 0), между from и endIf.
    /// $else вложенных $if пропускаются.
    /// </summary>
    private static int FindElse(string template, int from, int endIf)
    {
        int depth = 0;
        for (int i = from; i < endIf; i++)
        {
            var rest = template.AsSpan(i);
            if (rest.StartsWith(IfStart, StringComparison.Ordinal))
            {
                depth++;
                i += IfStart.Length - 1;
            }
            else if (rest.StartsWith(EndIf, StringComparison.Ordinal))
            {
                depth--;
                i += EndIf.Length - 1;
            }
            else if (depth == 0 && rest.StartsWith(Else, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return -1;
    }

    #endregion

    #region Условия

    private static bool EvaluateCondition(string condition, Context ctx)
    {
        var leftOperand = ConditionReader.GetOperand(condition);
        var trimmedLeftOperand = leftOperand.TrimStart();
        var conditionAfterOperand = string.Concat(condition.Skip(leftOperand.Length));

        var conditionOperator = ConditionReader.GetOperator(conditionAfterOperand);
        if (conditionOperator is null)
            return EvaluateOperandAsCondition(trimmedLeftOperand, ctx);
        var conditionAfterOperator = string.Concat(conditionAfterOperand.Skip(conditionOperator.Value.Value.Length + conditionOperator.Value.Index));

        var rightOperand = ConditionReader.GetOperand(conditionAfterOperator);
        var trimmedRightOperand = rightOperand.TrimStart();

        return EvaluateFullCondition(trimmedLeftOperand, conditionOperator.Value.Value, trimmedRightOperand, ctx);
    }

    private static bool EvaluateOperandAsCondition(string operand, Context ctx)
    {
        var trimmedOperand = operand.TrimStart();
        switch (trimmedOperand)
        {
            case "true":
                return true;

            case "false" or "null":
                return false;

            default:
                var c = trimmedOperand.First();
                if (c >= '0' && c <= '9')
                    throw new TemplateRenderException("Число не может быть условием без оператора сравнения");

                var resolved = ResolvePath(ctx, trimmedOperand);
                if (resolved is not true && resolved is not false && resolved is not null)
                    throw new TemplateRenderException($"'{trimmedOperand}' не является булевым значением");
                return resolved is true;
        }
    }

    private static bool EvaluateFullCondition(string leftOperand, string op, string rightOperand, Context ctx)
    {
        IComparable? leftOperandObj;
        IComparable? rightOperandObj;
        try
        {
            leftOperandObj = (IComparable?)CastOperand(leftOperand, ctx);
            rightOperandObj = (IComparable?)CastOperand(rightOperand, ctx);
        }
        catch (InvalidCastException ex)
        {
            throw new TemplateRenderException(ex.Message);
        }

        if (leftOperandObj is null || rightOperandObj is null)
        {
            return op switch
            {
                "==" => Equals(leftOperandObj, rightOperandObj),
                "!=" => !Equals(leftOperandObj, rightOperandObj),
                ">" or "<" or ">=" or "<=" => false,
                _ => throw new TemplateRenderException($"Unknown operator '{op}'"),
            };
        }
        if (leftOperandObj.GetType() != rightOperandObj.GetType())
            throw new TemplateRenderException(
                $"Нельзя сравнить {leftOperandObj.GetType().Name} и {rightOperandObj.GetType().Name}");

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
            _ => throw new TemplateRenderException($"Unknown operator '{op}'"),
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
            ">" => a > b && !equal,
            "<" => a < b && !equal,
            _ => throw new TemplateRenderException($"Unknown operator '{op}'"),
        };
    }

    private static bool IsNumber(object? obj)
    {
        return obj is
            sbyte or byte or short or ushort or int or uint or
            long or ulong or float or double or decimal;
    }

    private static object? CastOperand(string operand, Context ctx)
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
                if (first >= '0' && first <= '9')
                    return double.Parse(operand, CultureInfo.InvariantCulture);

                var resolved = ResolvePath(ctx, operand);
                return IsNumber(resolved) ? Convert.ToDouble(resolved) : resolved;
        }
    }

    #endregion

    #region Переменные

    /// <summary>
    /// Достаёт значение по пути вида a.B.C через рефлексию.
    /// Если первая часть пути — переменная цикла, начинаем с неё, иначе с корневой модели
    /// (имя корня в шаблоне может быть любым).
    /// </summary>
    private static object? ResolvePath(Context ctx, string path)
    {
        var properties = path.Split('.');
        object? value = ctx.Vars.TryGetValue(properties[0], out var loopValue) ? loopValue : ctx.Model;

        for (int i = 1; i < properties.Length; i++)
        {
            if (value is null)
                return null;

            var type = value.GetType();
            var propertyInfo = type.GetProperty(properties[i]);
            if (propertyInfo is null)
                throw new TemplateRenderException(
                    $"Свойство '{properties[i]}' не найдено в типе {type.Name} (путь {path})");

            value = propertyInfo.GetValue(value);
        }
        return value;
    }

    private static string RenderVariables(string text, Context ctx)
    {
        return VariableRegex.Replace(text, match =>
        {
            var value = ResolvePath(ctx, match.Groups[1].Value);
            return value?.ToString() ?? string.Empty;
        });
    }

    #endregion
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
                        throw new TemplateRenderException("Ожидался оператор '=='");
                case '!':
                    if (str.IndexOf("!=") == i)
                        return ("!=", i);
                    else
                        throw new TemplateRenderException("Ожидался оператор '!='");
                default:
                    throw new TemplateRenderException($"Неожиданный символ '{str[i]}' в условии");
            }
        }
        return null;
    }
}
