using System.Text.RegularExpressions;

namespace CardsApp.Services.Validation;

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
