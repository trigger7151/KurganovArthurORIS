using System.Text.RegularExpressions;

namespace CardsApp.Services.Validation;

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
