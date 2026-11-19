using System;

namespace MiniHttpServer.Framework.Configuration;

public class SettingsModel
{
    public string responsePath { get; set; }
    public string responsePathType { get; set; }
    public string[] prefixes { get; set; }
    public string connectionString { get; set; }
}
