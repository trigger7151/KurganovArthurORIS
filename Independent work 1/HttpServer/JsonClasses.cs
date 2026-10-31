namespace JsonClasses;

using System;

public class Settings
{
    public string responsePath { get; set; }
    public string responsePathType { get; set; }
    public string[] prefixes { get; set; }
}
public class AuthenticationInfo
{
    public string Email { get; set; }
    public string Password { get; set; }
}
