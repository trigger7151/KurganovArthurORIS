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
        Assert.AreEqual("Hello World ", resultEqual);
    }

    [TestMethod]
    public void LessThanOrEqualToOperator_CorrectResult()
    {
        var resultLess = renderer.RenderFromString("$if(2 <= 3)Hello World $endif", new {});
        var resultEqual =  renderer.RenderFromString("$if(5 <= 5)Hello World $endif", new {});
        Assert.AreEqual("Hello World ", resultLess);
        Assert.AreEqual("Hello World ", resultEqual);
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

    // ---------- Перенесено из MiniTemplateEngineTests ----------

    [TestMethod]
    public void BoolCondition_True_RendersBody()
    {
        var result = renderer.RenderFromString(IfTemplate, new { Name = "Вася", IsActive = true });
        Assert.AreEqual("<p>A</p><p>Вася</p><p>B</p>", result);
    }

    [TestMethod]
    public void BoolCondition_False_RemovesBody()
    {
        var result = renderer.RenderFromString(IfTemplate, new { Name = "Вася", IsActive = false });
        Assert.AreEqual("<p>A</p><p>B</p>", result);
    }

    // ---------- Сравнения: Y — условие истинно, N — ложно ----------

    private string Check(string condition, object model) =>
        renderer.RenderFromString($"$if({condition})Y$elseN$endif", model);

    [TestMethod]
    [DataRow("u.Age >= 18", 21, "Y")]
    [DataRow("u.Age >= 18", 18, "Y")]   // граница: равенство
    [DataRow("u.Age >= 18", 17, "N")]
    [DataRow("u.Age > 18", 18, "N")]    // граница: строгое неравенство
    [DataRow("u.Age > 18", 19, "Y")]
    [DataRow("u.Age <= 18", 18, "Y")]
    [DataRow("u.Age <= 18", 19, "N")]
    [DataRow("u.Age < 18", 18, "N")]
    [DataRow("u.Age < 18", 17, "Y")]
    [DataRow("u.Age == 21", 21, "Y")]
    [DataRow("u.Age == 21", 20, "N")]
    [DataRow("u.Age != 21", 21, "N")]
    [DataRow("u.Age != 21", 20, "Y")]
    public void PropertyAndNumber_Compared(string condition, int age, string expected)
    {
        Assert.AreEqual(expected, Check(condition, new { Age = age }));
    }

    [TestMethod]
    [DataRow("u.Age>=18")]              // без пробелов
    [DataRow("u.Age    >=    18")]      // лишние пробелы
    [DataRow("  u.Age >= 18  ")]        // пробелы по краям условия
    public void Whitespace_DoesNotMatter(string condition)
    {
        Assert.AreEqual("Y", Check(condition, new { Age = 18 }));
    }

    [TestMethod]
    public void LiteralOnLeft_Compared()
    {
        Assert.AreEqual("Y", Check("18 <= u.Age", new { Age = 21 }));
    }

    [TestMethod]
    public void PropertyOnRight_Compared()
    {
        Assert.AreEqual("Y", Check("u.Age >= u.MinAge", new { Age = 21, MinAge = 18 }));
        Assert.AreEqual("N", Check("u.Age >= u.MinAge", new { Age = 16, MinAge = 18 }));
    }

    [TestMethod]
    public void DifferentNumericTypes_Compared()
    {
        Assert.AreEqual("Y", Check("u.Price > 9.5", new { Price = 10m }));          // decimal и дробный литерал
        Assert.AreEqual("Y", Check("u.Count == 21.0", new { Count = 21L }));        // long и double
        Assert.AreEqual("Y", Check("u.Weight < u.Limit", new { Weight = 2.5f, Limit = 3 })); // float и int
    }

    [TestMethod]
    public void FractionalLiteral_ParsedWithDot()
    {
        // проверяет InvariantCulture: на русской Windows без неё "9.5" не распознаётся как число
        Assert.AreEqual("N", Check("u.Price > 9.5", new { Price = 9.4 }));
        Assert.AreEqual("Y", Check("u.Price > 9.5", new { Price = 9.6 }));
    }

    [TestMethod]
    public void DoubleRounding_EqualWithinEpsilon()
    {
        // 0.1 + 0.2 = 0.30000000000000004 — равенство должно учитывать погрешность.
        Assert.AreEqual("Y", Check("u.Total == 0.3", new { Total = 0.1 + 0.2 }));
        Assert.AreEqual("Y", Check("u.Total <= 0.3", new { Total = 0.1 + 0.2 }));
    }

    [TestMethod]
    public void PropertyAndString_Compared()
    {
        Assert.AreEqual("Y", Check(@"u.Name == ""Вася""", new { Name = "Вася" }));
        Assert.AreEqual("N", Check(@"u.Name == ""Петя""", new { Name = "Вася" }));
        Assert.AreEqual("Y", Check(@"u.Name != ""Петя""", new { Name = "Вася" }));
    }

    [TestMethod]
    public void NumberAndQuotedNumber_Throws()
    {
        // кавычки делают строку: сравнивать число со строкой даже через == и != — ошибка шаблона
        Assert.ThrowsExactly<TemplateRenderException>(() => Check(@"u.Age == ""21""", new { Age = 21 }));
        Assert.ThrowsExactly<TemplateRenderException>(() => Check(@"u.Age != ""21""", new { Age = 21 }));
    }

    [TestMethod]
    public void BoolLiteral_Compared()
    {
        Assert.AreEqual("Y", Check("u.IsActive == false", new { IsActive = false }));
        Assert.AreEqual("N", Check("u.IsActive == true", new { IsActive = false }));
    }

    [TestMethod]
    public void NullLiteral_Compared()
    {
        Assert.AreEqual("Y", Check("u.Name == null", new { Name = (string?)null }));
        Assert.AreEqual("N", Check("u.Name == null", new { Name = "Вася" }));
        Assert.AreEqual("Y", Check("u.Name != null", new { Name = "Вася" }));
    }

    [TestMethod]
    public void NullInOrderingComparison_IsFalse()
    {
        Assert.AreEqual("N", Check("u.Age > 18", new { Age = (int?)null }));
        Assert.AreEqual("N", Check("u.Age <= 18", new { Age = (int?)null }));
        Assert.AreEqual("N", Check("18 < u.Age", new { Age = (int?)null }));
    }

    [TestMethod]
    public void WhitespaceOnlyCondition_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() => Check("   ", new { }));
    }

    [TestMethod]
    public void StringGreaterThanNumber_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() => Check("u.Name > 5", new { Name = "Вася" }));
    }

    [TestMethod]
    public void MissingPropertyOnRight_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() => Check("u.Age >= u.Missing", new { Age = 21 }));
    }
}
