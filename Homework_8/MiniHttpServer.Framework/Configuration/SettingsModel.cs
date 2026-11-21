using System;

namespace MiniHttpServer.Framework.Configuration;

/// <summary>
/// Содержимое файла Settings/settings.json в том виде, в каком оно записано в JSON.
/// </summary>
public class SettingsModel
{
    /// <summary>
    /// Путь к странице, которая отдаётся по адресу <c>/</c>.
    /// </summary>
    public string responsePath { get; set; }
    /// <summary>
    /// Как понимать <see cref="responsePath"/>: <c>relative</c> — относительно каталога приложения,
    /// <c>absolute</c> — как абсолютный путь.
    /// </summary>
    public string responsePathType { get; set; }
    /// <summary>
    /// Адреса, которые прослушивает сервер, например <c>http://127.0.0.1:8888/</c>.
    /// </summary>
    public string[] prefixes { get; set; }
    /// <summary>
    /// Строка подключения к базе данных.
    /// </summary>
    public string connectionString { get; set; }
}
