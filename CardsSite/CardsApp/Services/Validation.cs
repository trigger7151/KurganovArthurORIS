using System.Text.RegularExpressions;

namespace CardsApp.Services;

/// <summary>
/// Правила валидации. Те же регулярные выражения продублированы в static/js/validation.js,
/// чтобы клиент и сервер проверяли данные одинаково. Последнее слово всегда за сервером.
/// </summary>
public static partial class ValidationRules
{
    // Логин: латиница, цифры и подчёркивание, 3–20 символов.
    [GeneratedRegex(@"^[A-Za-z0-9_]{3,20}$")]
    public static partial Regex Login();

    // Пароль: 8–64 символа, хотя бы одна буква и одна цифра.
    [GeneratedRegex(@"^(?=.*[A-Za-zА-Яа-яЁё])(?=.*\d).{8,64}$")]
    public static partial Regex Password();

    // Название конуса или эффекта: начинается с буквы/цифры; буквы, цифры, пробел и знаки препинания.
    [GeneratedRegex(@"^[A-Za-zА-Яа-яЁё0-9][A-Za-zА-Яа-яЁё0-9 ,.!?'«»""\-—:]*$")]
    public static partial Regex Title();

    // Адрес арта: загруженный файл /uploads/... или внешний http(s)-адрес.
    [GeneratedRegex(@"^(/uploads/[A-Za-z0-9_\-]+\.(png|jpe?g|webp)|https?://[^\s""'<>]+)$", RegexOptions.IgnoreCase)]
    public static partial Regex ImageUrl();

    // Ключ пути: строчная латиница, 3–20 символов.
    [GeneratedRegex(@"^[a-z]{3,20}$")]
    public static partial Regex Slug();

    // Название пути: буквы, пробел и дефис, 2–40 символов.
    [GeneratedRegex(@"^[A-Za-zА-Яа-яЁё][A-Za-zА-Яа-яЁё \-]{1,39}$")]
    public static partial Regex PathName();

    // Характеристика: целое число до 5 цифр.
    [GeneratedRegex(@"^\d{1,5}$")]
    public static partial Regex StatValue();
}

/// <summary>Ошибки валидации формы: имя поля → сообщение.</summary>
public class ValidationErrors
{
    private readonly Dictionary<string, string> _errors = new(StringComparer.OrdinalIgnoreCase);

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyDictionary<string, string> All => _errors;

    public void Add(string field, string message) => _errors.TryAdd(field, message);
    public string Get(string field) => _errors.TryGetValue(field, out var m) ? m : "";

    /// <summary>Проверяет обязательность, длину и регулярное выражение.</summary>
    public string Text(string field, string? value, string label, int maxLength, Regex? rule = null,
        bool required = true, int minLength = 1, string? ruleMessage = null)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0)
        {
            if (required) Add(field, $"Заполните поле «{label}».");
            return text;
        }
        if (text.Length < minLength || text.Length > maxLength)
            Add(field, $"«{label}»: от {minLength} до {maxLength} символов.");
        else if (rule is not null && !rule.IsMatch(text))
            Add(field, ruleMessage ?? $"«{label}» содержит недопустимые символы.");
        return text;
    }

    public int Number(string field, string? value, string label, int min, int max)
    {
        var text = (value ?? "").Trim();
        if (!ValidationRules.StatValue().IsMatch(text) || !int.TryParse(text, out var number))
        {
            Add(field, $"«{label}»: введите целое число.");
            return 0;
        }
        if (number < min || number > max)
            Add(field, $"«{label}»: от {min} до {max}.");
        return number;
    }
}
