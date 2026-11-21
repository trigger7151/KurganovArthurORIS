using System.Text.RegularExpressions;

namespace CardsApp.Services.Security;

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
