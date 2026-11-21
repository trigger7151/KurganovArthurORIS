namespace MiniHttpServer.Framework.Configuration;
using System;
using System.Text.Json;
using MiniHttpServer.Framework.Content;


/// <summary>
/// Настройки сервера, прочитанные из Settings/settings.json. Создаются один раз (одиночка).
/// </summary>
public class ServerSettings
{

    /// <summary>
    /// Единственный экземпляр настроек; создаётся при первом обращении к <see cref="Instance"/>.
    /// </summary>
    private static ServerSettings? PrivateInstance{get;set;}
    /// <summary>
    /// Объект блокировки, чтобы экземпляр не создался дважды при одновременном обращении из разных потоков.
    /// </summary>
    private static readonly object Lock = new object();
    /// <summary>
    /// Абсолютный путь к странице, которая отдаётся по адресу <c>/</c>.
    /// </summary>
    public string ResponsePath{get;}
    /// <summary>
    /// Адреса, которые прослушивает сервер.
    /// </summary>
    public string[] Prefixes{get;}
    /// <summary>
    /// Строка подключения к базе данных.
    /// </summary>
    public string ConnectionString { get; }
    /// <summary>
    /// Читает и проверяет файл настроек.
    /// </summary>
    /// <exception cref="FileNotFoundException">Файл настроек не найден.</exception>
    /// <exception cref="JsonException">В поле <c>responsePathType</c> указано недопустимое значение.</exception>
    private ServerSettings()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "Settings/settings.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException($"Файл настроек {settingsPath} не найден");
        }
        var settingsObj = DeserializeSettings(settingsPath);
        string responsePath;
        if (settingsObj.responsePathType == "relative")
        {
            responsePath = Path.Combine(AppContext.BaseDirectory, settingsObj.responsePath);
        }
        else if (settingsObj.responsePathType == "absolute")
        {
            responsePath = settingsObj.responsePath;
        }
        else
        {
            throw new JsonException("Введены неверные данные в поле responsePathType" +
            " в файле настроек. Возможные варианты: relative, absolute");
        }
        ResponsePath = responsePath;
        Prefixes = settingsObj.prefixes;
        ConnectionString = settingsObj.connectionString;
    }
    /// <summary>
    /// Десериализует файл настроек в <see cref="SettingsModel"/>.
    /// </summary>
    /// <param name="settingsPath">Путь к файлу настроек.</param>
    /// <returns>Прочитанные настройки.</returns>
    private static SettingsModel DeserializeSettings(string settingsPath)
    {
        var settingsJson = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<SettingsModel>(settingsJson);
    }
    /// <summary>
    /// Общий экземпляр настроек. При первом обращении читает файл настроек
    /// (с блокировкой с двойной проверкой).
    /// </summary>
    public static ServerSettings Instance 
    {
        get 
        {
            if (PrivateInstance == null) 
            {
                lock (Lock) 
                {
                    if (PrivateInstance == null) 
                    {
                        PrivateInstance = new ServerSettings();
                    }
                }
            }
            return PrivateInstance;
        }
    }
}