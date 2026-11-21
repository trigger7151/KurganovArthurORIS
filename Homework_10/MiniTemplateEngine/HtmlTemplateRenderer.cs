using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

/// <summary>
/// Контекст рендеринга: корневая модель и переменные, объявленные циклами $foreach.
/// </summary>
internal record Context(object? Model, IReadOnlyDictionary<string, object?> Vars)
{
    /// <summary>
    /// Создаёт корневой контекст без переменных цикла.
    /// </summary>
    /// <param name="model">Модель данных.</param>
    /// <returns>Новый контекст.</returns>
    public static Context Root(object? model) => new(model, new Dictionary<string, object?>());

    /// <summary>
    /// Возвращает новый контекст с добавленной переменной цикла (старый не меняется).
    /// </summary>
    /// <param name="name">Имя переменной цикла.</param>
    /// <param name="value">Значение текущего элемента.</param>
    /// <returns>Контекст с той же моделью и расширенным набором переменных.</returns>
    public Context With(string name, object? value)
    {
        var vars = new Dictionary<string, object?>(Vars) { [name] = value };
        return this with { Vars = vars };
    }
}

/// <inheritdoc />
public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    /// <summary>
    /// Подстановка <c>${путь}</c>, где путь — имена через точку.
    /// </summary>
    private static readonly Regex VariableRegex = new(@"\$\{([a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)\}");
    /// <summary>
    /// Заголовок цикла <c>var имя in путь</c>.
    /// </summary>
    private static readonly Regex ForeachHeaderRegex =
        new(@"^\s*var\s+([a-zA-Z_]\w*)\s+in\s+([a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)*)\s*$");

    /// <summary>Начало условия.</summary>
    private const string IfStart = "$if(";
    /// <summary>Ветка «иначе».</summary>
    private const string Else = "$else";
    /// <summary>Конец условия.</summary>
    private const string EndIf = "$endif";
    /// <summary>Начало цикла.</summary>
    private const string ForeachStart = "$foreach(";
    /// <summary>Конец цикла.</summary>
    private const string EndFor = "$endfor";
    /// <summary>
    /// Погрешность, в пределах которой числа считаются равными при сравнении.
    /// </summary>
    private const double Epsilon = 1e-9;

    /// <inheritdoc />
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        return Render(htmlTemplate, Context.Root(dataModel));
    }

    /// <inheritdoc />
    public string RenderFromFile(string filePath, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        return RenderFromString(File.ReadAllText(filePath), dataModel);
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Рендерит блок $if, начинающийся с позиции <paramref name="start"/>.
    /// </summary>
    /// <param name="template">Шаблон целиком.</param>
    /// <param name="start">Позиция начала <c>$if(</c>.</param>
    /// <param name="ctx">Контекст рендеринга.</param>
    /// <param name="sb">Буфер, в который дописывается результат.</param>
    /// <returns>Позиция сразу после <c>$endif</c>.</returns>
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

    /// <summary>
    /// Рендерит блок $foreach, начинающийся с позиции <paramref name="start"/>:
    /// тело выводится для каждого элемента коллекции с переменной цикла в контексте.
    /// </summary>
    /// <param name="template">Шаблон целиком.</param>
    /// <param name="start">Позиция начала <c>$foreach(</c>.</param>
    /// <param name="ctx">Контекст рендеринга.</param>
    /// <param name="sb">Буфер, в который дописывается результат.</param>
    /// <returns>Позиция сразу после <c>$endfor</c>.</returns>
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

    /// <summary>
    /// Вычисляет условие из <c>$if(...)</c>: один операнд или «операнд оператор операнд».
    /// </summary>
    /// <param name="condition">Текст условия.</param>
    /// <param name="ctx">Контекст рендеринга.</param>
    /// <returns>Значение условия.</returns>
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

    /// <summary>
    /// Вычисляет условие из одного операнда: <c>true</c>/<c>false</c>/<c>null</c> или путь к булеву значению.
    /// </summary>
    /// <param name="operand">Операнд.</param>
    /// <param name="ctx">Контекст рендеринга.</param>
    /// <returns>Значение условия.</returns>
    /// <exception cref="TemplateRenderException">Операнд — число или не булево значение.</exception>
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

    /// <summary>
    /// Сравнивает два операнда. Сравниваемые значения должны быть одного типа и реализовывать <see cref="IComparable"/>;
    /// с null допустимы только <c>==</c> и <c>!=</c>.
    /// </summary>
    /// <param name="leftOperand">Левый операнд.</param>
    /// <param name="op">Оператор сравнения.</param>
    /// <param name="rightOperand">Правый операнд.</param>
    /// <param name="ctx">Контекст рендеринга.</param>
    /// <returns>Результат сравнения.</returns>
    /// <exception cref="TemplateRenderException">Значения несравнимы или оператор неизвестен.</exception>
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

    /// <summary>
    /// Сравнивает числа с учётом погрешности <see cref="Epsilon"/>.
    /// </summary>
    /// <param name="a">Левое число.</param>
    /// <param name="b">Правое число.</param>
    /// <param name="op">Оператор сравнения.</param>
    /// <returns>Результат сравнения.</returns>
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

    /// <summary>
    /// Проверяет, является ли значение числом встроенного типа.
    /// </summary>
    /// <param name="obj">Значение.</param>
    /// <returns>true для числовых типов.</returns>
    private static bool IsNumber(object? obj)
    {
        return obj is
            sbyte or byte or short or ushort or int or uint or
            long or ulong or float or double or decimal;
    }

    /// <summary>
    /// Превращает операнд в значение: литерал (<c>null</c>, <c>true</c>, <c>false</c>, строка в кавычках, число)
    /// или значение по пути из модели. Числа приводятся к double, чтобы сравнивать разные числовые типы.
    /// </summary>
    /// <param name="operand">Операнд.</param>
    /// <param name="ctx">Контекст рендеринга.</param>
    /// <returns>Значение операнда.</returns>
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

    /// <summary>
    /// Заменяет все подстановки <c>${...}</c> в тексте HTML-кодированными значениями.
    /// </summary>
    /// <param name="text">Текст без блоков $if/$foreach.</param>
    /// <param name="ctx">Контекст рендеринга.</param>
    /// <returns>Текст с подставленными значениями.</returns>
    private static string RenderVariables(string text, Context ctx)
    {
        return VariableRegex.Replace(text, match =>
        {
            var value = ResolvePath(ctx, match.Groups[1].Value);
            // HTML-кодирование: <, >, &, " и ' становятся текстом, а не разметкой,
            // поэтому данные из модели не могут внедрить на страницу теги или скрипты
            return WebUtility.HtmlEncode(value?.ToString() ?? string.Empty);
        });
    }

    #endregion
}

/// <summary>
/// Разбор условия <c>$if(...)</c> на операнды и оператор.
/// </summary>
internal class ConditionReader
{
    /// <summary>
    /// Читает операнд с начала строки конечным автоматом <see cref="OperandReader"/>.
    /// </summary>
    /// <param name="str">Строка, начинающаяся с операнда (возможно, с пробелами).</param>
    /// <returns>Прочитанный операнд вместе с ведущими пробелами.</returns>
    /// <exception cref="TemplateRenderException">Операнд не найден.</exception>
    public static string GetOperand(string str)
    {
        var reader = new OperandReader();
        // выбрасывает исключение если не нашел операнд
        return reader.ProcessWord(str);
    }

    /// <summary>
    /// Ищет оператор сравнения (<c>==</c>, <c>!=</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c>),
    /// пропуская пробелы в начале строки.
    /// </summary>
    /// <param name="str">Часть условия после левого операнда.</param>
    /// <returns>Оператор и его позиция или null, если строка закончилась без оператора.</returns>
    /// <exception cref="TemplateRenderException">Вместо оператора встретился другой символ.</exception>
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
