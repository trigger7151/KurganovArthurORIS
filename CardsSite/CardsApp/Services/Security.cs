using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CardsApp.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}

/// <summary>PBKDF2-SHA256. Формат хеша: pbkdf2-sha256$итерации$соль$хеш (base64).</summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "pbkdf2-sha256";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations))
            return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>
/// Очищает HTML из WYSIWYG-редактора: оставляет только разрешённые теги,
/// у ссылок — только http(s)-адрес. Скрипты, стили и обработчики событий удаляются.
/// </summary>
public static partial class HtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "u", "s", "a", "ul", "ol", "li", "blockquote", "h3", "h4"
    };

    [GeneratedRegex(@"<(script|style|iframe|object|embed|svg|math)\b[\s\S]*?</\1\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousBlocks();

    [GeneratedRegex(@"<!--[\s\S]*?-->")]
    private static partial Regex Comments();

    [GeneratedRegex(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)\b([^>]*)>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\bhref\s*=\s*(""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex Href();

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var source = Comments().Replace(DangerousBlocks().Replace(html, ""), "");
        var output = new System.Text.StringBuilder();
        var position = 0;
        foreach (Match match in Tag().Matches(source))
        {
            // Текст между тегами: любые оставшиеся < и > становятся текстом,
            // поэтому незакрытый «тег» вроде <img onerror=... не попадёт в разметку.
            output.Append(EncodeAngles(source[position..match.Index]));
            output.Append(RebuildTag(match));
            position = match.Index + match.Length;
        }
        output.Append(EncodeAngles(source[position..]));
        return output.ToString().Trim();
    }

    private static string EncodeAngles(string text) => text.Replace("<", "&lt;").Replace(">", "&gt;");

    private static string RebuildTag(Match match)
    {
        var closing = match.Groups[1].Value == "/";
        var name = match.Groups[2].Value.ToLowerInvariant();
        if (!AllowedTags.Contains(name))
            return "";
        if (closing)
            return $"</{name}>";
        if (name != "a")
            return $"<{name}>";

        var hrefMatch = Href().Match(match.Groups[3].Value);
        var href = System.Net.WebUtility.HtmlDecode(
            hrefMatch.Groups[2].Success ? hrefMatch.Groups[2].Value : hrefMatch.Groups[3].Value).Trim();
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            return "<a>";
        return $"<a href=\"{System.Net.WebUtility.HtmlEncode(uri.AbsoluteUri)}\" target=\"_blank\" rel=\"noopener noreferrer\">";
    }

    /// <summary>Есть ли в HTML видимый текст (пустой редактор Quill отдаёт &lt;p&gt;&lt;br&gt;&lt;/p&gt;).</summary>
    public static bool HasText(string html)
        => Regex.Replace(html, "<[^>]+>", "").Replace("&nbsp;", "").Trim().Length > 0;
}
