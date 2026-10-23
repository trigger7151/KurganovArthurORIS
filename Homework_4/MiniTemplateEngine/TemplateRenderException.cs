namespace MiniTemplateEngine;

/// <summary>
/// Исключение, которое выбрасывается при ошибке в HTML-шаблоне:
/// обращение к несуществующему свойству модели, незакрытая конструкция и т. п.
/// </summary>
public class TemplateRenderException : Exception
{
    /// <summary>
    /// Создаёт исключение с указанным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Описание ошибки в шаблоне.</param>
    public TemplateRenderException(string message) : base(message) { }
}
