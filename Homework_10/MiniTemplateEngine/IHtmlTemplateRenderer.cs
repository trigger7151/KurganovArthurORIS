using System;

namespace MiniTemplateEngine;

/// <summary>
/// Шаблонизатор HTML. Поддерживает подстановки <c>${модель.Свойство}</c>, условия
/// <c>$if(...)…$else…$endif</c> и циклы <c>$foreach(var x in модель.Коллекция)…$endfor</c>.
/// Первое слово пути — имя модели (любое) или переменная цикла. Подставляемые значения HTML-кодируются.
/// </summary>
public interface IHtmlTemplateRenderer
{
    /// <summary>
    /// Рендерит шаблон из строки.
    /// </summary>
    /// <param name="htmlTemplate">Текст шаблона.</param>
    /// <param name="dataModel">Модель данных.</param>
    /// <returns>Готовый HTML.</returns>
    /// <exception cref="TemplateRenderException">Ошибка в шаблоне.</exception>
    string RenderFromString(string htmlTemplate, object dataModel);
    /// <summary>
    /// Рендерит шаблон из файла.
    /// </summary>
    /// <param name="filePath">Путь к файлу шаблона.</param>
    /// <param name="dataModel">Модель данных.</param>
    /// <returns>Готовый HTML.</returns>
    /// <exception cref="TemplateRenderException">Ошибка в шаблоне.</exception>
    string RenderFromFile(string filePath, object dataModel);
    /// <summary>
    /// Рендерит шаблон из файла и сохраняет результат в другой файл.
    /// </summary>
    /// <param name="inputFilePath">Путь к файлу шаблона.</param>
    /// <param name="outputFilePath">Путь к файлу результата.</param>
    /// <param name="dataModel">Модель данных.</param>
    /// <returns>Готовый HTML.</returns>
    /// <exception cref="TemplateRenderException">Ошибка в шаблоне.</exception>
    string RenderToFile(string inputFilePath, string outputFilePath, object dataModel);
}
