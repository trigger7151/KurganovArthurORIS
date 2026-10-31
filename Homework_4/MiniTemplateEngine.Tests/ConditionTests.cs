using System;

namespace MiniTemplateEngine.Tests;

internal class ExampleClass
{
    public string str{get;set;} = "abc";
}

internal class ComparableClass : IComparable
{
    public int Num{get;set;}

    public int CompareTo(object? obj)
    {
        if (obj is ComparableClass comparableObj)
            return Num.CompareTo(comparableObj.Num);
        else
            throw new ArgumentException("Incorrect parameter value");
    }

    public ComparableClass(int num)
    {
        Num = num;
    }
}

[TestClass]
public class ConditionTests
{
    private HtmlTemplateRenderer renderer{get;set;}
    [TestInitialize]
    public void SetUp()
    {
        renderer = new HtmlTemplateRenderer();
    }

    [TestMethod]
    public void GreaterThanOrEqualToOperator_CorrectResult()
    {
        var resultGreater = renderer.RenderFromString("$if(5 >= 3)Hello World $endif", new {});
        var resultEqual =  renderer.RenderFromString("$if(5 >= 5)Hello World $endif", new {});
        Assert.AreEqual("Hello World ", resultGreater);
        Assert.AreEqual("Hello world ", resultEqual);
    }

    [TestMethod]
    public void LessThanOrEqualToOperator_CorrectResult()
    {
        var resultLess = renderer.RenderFromString("$if(2 <= 3)Hello World $endif", new {});
        var resultEqual =  renderer.RenderFromString("$if(5 <= 5)Hello World $endif", new {});
        Assert.AreEqual("Hello World ", resultLess);
        Assert.AreEqual("Hello world ", resultEqual);
    }

    [TestMethod]
    public void GreaterThanOperator_CorrectResult()
    {
        var resultGreater = renderer.RenderFromString("$if(5 > 3)Hello World $endif", new {});
        Assert.AreEqual("Hello World ", resultGreater);
    }

    [TestMethod]
    public void LessThanOperator_CorrectResult()
    {
        var resultLess = renderer.RenderFromString("$if(2 < 3)Hello World $endif", new {});
        Assert.AreEqual("Hello World ", resultLess);
    }

    [TestMethod]
    public void ValuesEqual_CorrectResult()
    {
        var result = renderer.RenderFromString(@"$if(""abc"" == ""abc"")Hello World $endif", new {});
        Assert.AreEqual("Hello World ", result);
    }

    [TestMethod]
    public void ValuesNotEqual_CorrectResult()
    {
        var result = renderer.RenderFromString(@"$if(""abc"" != ""agc"")Hello World $endif", new {});
        Assert.AreEqual("Hello World ", result);
    }

    private const string IfTemplate = "<p>A</p>$if(u.IsActive)<p>${u.Name}</p>$endif<p>B</p>";

    [TestMethod]
    public void NoOperatorObjectNull_RemovesBody()
    {
        var result = renderer.RenderFromString(IfTemplate, new { Name = "Вася", IsActive = (bool?)null });
        Assert.AreEqual("<p>A</p><p>B</p>", result);
    }

    [TestMethod]
    public void ConditionIsBool_CorrectResult()
    {
        var result = renderer.RenderFromString(@"$if(u.IsTrue)Hello World $endif", new {IsTrue = true});
        Assert.AreEqual("Hello World ", result);
    }

    [TestMethod]
    public void TwoOperandsAreObjects_CorrectResult()
    {
        var result = renderer.RenderFromString(@"$if(u.Obj1 < u.Obj2 )Hello World $endif", new {Obj1 = new ComparableClass(2), Obj2 = new ComparableClass(3)});
        Assert.AreEqual("Hello World ", result);
    }

    [TestMethod]
    public void EmptyCondition_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$if()X$endif", new {}));
    }

    [TestMethod]
    public void ObjectDoesNotImplementIComparable_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$if(u.Obj1 > u.Obj2)X$endif", new {Obj1 = new ExampleClass(), Obj2 = new ExampleClass()}));
    }

    [TestMethod]
    public void NoOperatorObjectNotBool_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$if(u.Obj1)X$endif", new {Obj1 = new ExampleClass()}));
    }

    [TestMethod]
    public void MissingProperty_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$if(u.Missing)X$endif", new { IsActive = true }));
    }

    [TestMethod]
    public void ComparingDifferentTypes_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$if(u.Obj1 > u.Obj2)X$endif", new {Obj1 = new ExampleClass(), Obj2 = 5}));
    }


}
