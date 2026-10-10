namespace HttpServer.Settings;
using System;
using System.Text.Json;
using JsonClasses;


public class ServerSettings
{

    private static ServerSettings? PrivateInstance{get;set;}
    private static readonly object Lock = new object();
    public string ResponsePath{get;}
    public string[] Prefixes{get;}
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
    }
    private static Settings DeserializeSettings(string settingsPath)
    {
        var settingsJson = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<Settings>(settingsJson);
    }
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