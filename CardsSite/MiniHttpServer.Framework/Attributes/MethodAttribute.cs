using System;

namespace MiniHttpServer.Framework.Attributes;

public class MethodAttribute: Attribute
{
    public string Method{get;}
    public MethodAttribute(string method)
    {
        Method = method;
    }
    
}
