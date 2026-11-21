namespace MiniTemplateEngine.Tests;

[TestClass]
public sealed class MiniTemplateEngineTests
{
    private HtmlTemplateRenderer renderer{get;set;}
    [TestInitialize]
    public void SetUp()
    {
        renderer = new HtmlTemplateRenderer();
    }
    [TestMethod]
    public void SimpleLine()
    {
        var result = renderer.RenderFromString("<p>${name}</p>", "Вася");
        Assert.AreEqual("<p>Вася</p>", result);
    }

    [TestMethod]
    public void LineWithObjectProperty()
    {
        var result = renderer.RenderFromString("<p>${User.Name}</p>", new {Name = "Вася"});
        Assert.AreEqual("<p>Вася</p>", result);
    }

    [TestMethod]
    public void MultipleVariables()
    {
        var result = renderer
        .RenderFromString("<p>${User.Name} is ${User.Age} years old</p>", new {Name = "Skibidi Sigma", Age = 67});
        Assert.AreEqual("<p>Skibidi Sigma is 67 years old</p>", result);
    }

    [TestMethod]
    public void Variable_WithScriptTag_IsHtmlEncoded()
    {
        var result = renderer.RenderFromString("<p>${u.Name}</p>", new { Name = "<script>alert('xss')</script>" });
        Assert.AreEqual("<p>&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;</p>", result);
    }

    [TestMethod]
    public void Variable_InAttribute_QuotesAreHtmlEncoded()
    {
        var result = renderer.RenderFromString("<img alt=\"${u.Title}\">", new { Title = "\" onerror=\"alert(1)" });
        Assert.AreEqual("<img alt=\"&quot; onerror=&quot;alert(1)\">", result);
    }

    [TestMethod]
    public void Variable_WithAmpersand_IsHtmlEncoded()
    {
        var result = renderer.RenderFromString("${u.Name}", new { Name = "Tom & Jerry" });
        Assert.AreEqual("Tom &amp; Jerry", result);
    }

    [TestMethod]
    public void Foreach_ItemValues_AreHtmlEncoded()
    {
        var result = renderer.RenderFromString(
            "$foreach(var t in u.Tags)<li>${t}</li>$endfor",
            new { Tags = new[] { "<b>", "ok" } });
        Assert.AreEqual("<li>&lt;b&gt;</li><li>ok</li>", result);
    }

    [TestMethod]
    public void TemplateMarkup_IsNotEncoded_OnlyValues()
    {
        var result = renderer.RenderFromString("<h1 class=\"title\">${u.Name}</h1>", new { Name = "Вася" });
        Assert.AreEqual("<h1 class=\"title\">Вася</h1>", result);
    }

    [TestMethod]
    public void SameVariableTwice()
    {
        var result = renderer.RenderFromString("${var.A} ${var.A}", new {A = "кофе помогает от альцгеймера"});
        Assert.AreEqual("кофе помогает от альцгеймера кофе помогает от альцгеймера", result);
    }

    [TestMethod]
    public void NumAndBool()
    {
        var result = renderer.RenderFromString("${Object.IsActive}, ${Object.Age}", new {IsActive = true, Age = 42});
        Assert.AreEqual("True, 42", result);
    }

    [TestMethod]
    public void NestedProperty()
    {
        var result = renderer.RenderFromString("${bag.Brand.Name}", new {Brand = new {Name = "Prada"}});
        Assert.AreEqual("Prada", result);
    }

    [TestMethod]
    public void NoVariables()
    {
        var result = renderer.RenderFromString("cogito ergo sum", "A");
        Assert.AreEqual("cogito ergo sum", result);
    }

    [TestMethod]
    public void ModelIsNull()
    {
        var result = renderer.RenderFromString("${Name}", null);
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void NullInMiddleOfChain()
    {
        var result = renderer.RenderFromString("${Person.Father.Name}", new {Father = (string)null});
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void IncorrectBrackets()
    {
        var emptyBrackets = renderer.RenderFromString("${}", new {});
        var numsInBrackets = renderer.RenderFromString("${213}", new {});
        var numAfterDollarSign = renderer.RenderFromString("$5", new {});

        Assert.AreEqual("${}", emptyBrackets);
        Assert.AreEqual("${213}", numsInBrackets);
        Assert.AreEqual("$5", numAfterDollarSign);
    }

    [TestMethod]
    public void NonexistentProperty()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() => renderer.RenderFromString("${P.Name}", new {}));
    }

    [TestMethod]
    public void TemplateIsNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => renderer.RenderFromString(null, new {}));
    }

    [TestMethod]
    public void Variable_PropertyIsNull_RendersEmpty()
    {
        var result = renderer.RenderFromString("<p>${u.Name}</p>", new { Name = (string)null });
        Assert.AreEqual("<p></p>", result);
    }

    [TestMethod]
    public void If_AtStartAndEndOfTemplate_RendersBody()
    {
        var result = renderer.RenderFromString("$if(u.IsActive)X$endif", new { IsActive = true });
        Assert.AreEqual("X", result);
    }

    [TestMethod]
    public void If_EmptyBody_RendersNothing()
    {
        var result = renderer.RenderFromString("A$if(u.IsActive)$endif B", new { IsActive = true });
        Assert.AreEqual("A B", result);
    }

    [TestMethod]
    public void If_MissingPropertyInSkippedBranch_DoesNotThrow()
    {
        var result = renderer.RenderFromString("A$if(u.IsActive)${u.Missing}$endif B", new { IsActive = false });
        Assert.AreEqual("A B", result);
    }

    [TestMethod]
    public void If_MultilineBody_KeepsLineBreaks()
    {
        var template = "<ul>\n$if(u.IsActive)\n<li>${u.Name}</li>\n$endif\n</ul>";
        var result = renderer.RenderFromString(template, new { Name = "Вася", IsActive = true });
        Assert.AreEqual("<ul>\n\n<li>Вася</li>\n\n</ul>", result);
    }

    [TestMethod]
    public void Else_ConditionTrue_RendersIf()
    {
        var template = "<p>$if(u.IsTrue)\noption1\n$else\noption2$endif</p>";
        var result = renderer.RenderFromString(template, new {IsTrue = true});
        Assert.AreEqual("<p>\noption1\n</p>", result);
    }

    [TestMethod]
    public void Else_ConditionFalse_RendersElse()
    {
        var template = "<p>$if(u.IsTrue)\noption1\n$else\noption2\n$endif</p>";
        var result = renderer.RenderFromString(template, new {IsTrue = false});
        Assert.AreEqual("<p>\noption2\n</p>", result);
    }
}
