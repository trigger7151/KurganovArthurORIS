using System.Text.Json;

namespace CustomHttpServer.Framework.Configuration;

/// <summary>
/// Класс конфигурации приложения (Singleton)
/// </summary>
public sealed class ConfigurationManager
{
    private static readonly ConfigurationManager _instance = new();

    public SettingsModel? Config { get; private set; } = null;
 
    private ConfigurationManager()
    {
        LoadSettings();
    }
 
    public static ConfigurationManager GetInstance()
    {
        return _instance;
    }
    
    // Метод для загрузки настроек из файла
    private void LoadSettings()
    {
        const string fileName = "settings.json";

        try
        {
            // Проверяем, существует ли файл
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"Файл настроек '{fileName}' не найден.");
            }

            // Чтение и десериализация JSON
            string json = File.ReadAllText(fileName);
            Config = JsonSerializer.Deserialize<SettingsModel>(json);
        }
        catch (FileNotFoundException e)
        {
            // Обработка ошибки, если файл не найден
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.ResetColor();
        }
        catch (JsonException e)
        {
            // Обработка ошибки, если JSON файл некорректен
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка чтения настроек: {e.Message}. Файл некорректен.");
            Console.ResetColor();
        }
        catch (Exception e)
        {
            // Обработка других ошибок
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Неизвестная ошибка: {e.Message}");
            Console.ResetColor();
        }
    }
}