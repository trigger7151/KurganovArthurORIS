namespace MiniTemplateEngine.Tests;

internal class TestBook
{
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public int Year { get; set; }
}

internal class TestUser
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool Gender { get; set; }
    public List<TestBook> Books { get; set; } = new();
}

internal class TestShelf
{
    public string Title { get; set; } = "";
    public List<TestBook> Books { get; set; } = new();
}

internal class TestLibrary
{
    public List<TestShelf> Shelves { get; set; } = new();
}

[TestClass]
public class BlocksAndLoopsTests
{
    private HtmlTemplateRenderer renderer { get; set; } = null!;

    [TestInitialize]
    public void SetUp()
    {
        renderer = new HtmlTemplateRenderer();
    }

    // ---------- Несколько блоков $if ----------

    [TestMethod]
    public void TwoIfBlocks_BothProcessed()
    {
        var template = "$if(u.A)A$endif-$if(u.B)B$endif";
        Assert.AreEqual("A-B", renderer.RenderFromString(template, new { A = true, B = true }));
        Assert.AreEqual("-B", renderer.RenderFromString(template, new { A = false, B = true }));
        Assert.AreEqual("A-", renderer.RenderFromString(template, new { A = true, B = false }));
    }

    [TestMethod]
    public void ThreeIfBlocksWithElse_AllProcessed()
    {
        var template = "$if(u.A)1$else0$endif$if(u.B)1$else0$endif$if(u.C)1$else0$endif";
        Assert.AreEqual("101", renderer.RenderFromString(template, new { A = true, B = false, C = true }));
    }

    // ---------- Вложенные $if ----------

    [TestMethod]
    [DataRow(true, true, "AB")]
    [DataRow(true, false, "Ab")]
    [DataRow(false, true, "a")]
    [DataRow(false, false, "a")]
    public void NestedIf_WithElseOnBoth(bool a, bool b, string expected)
    {
        // $else внутреннего if не должен восприниматься как $else внешнего
        var template = "$if(u.A)A$if(u.B)B$elseb$endif$elsea$endif";
        Assert.AreEqual(expected, renderer.RenderFromString(template, new { A = a, B = b }));
    }

    [TestMethod]
    public void NestedIf_InElseBranch()
    {
        var template = "$if(u.A)A$else$if(u.B)B$elseC$endif$endif";
        Assert.AreEqual("B", renderer.RenderFromString(template, new { A = false, B = true }));
        Assert.AreEqual("C", renderer.RenderFromString(template, new { A = false, B = false }));
    }

    [TestMethod]
    public void ThreeLevelsOfIf()
    {
        var template = "$if(u.A)1$if(u.B)2$if(u.C)3$endif$endif$endif!";
        Assert.AreEqual("123!", renderer.RenderFromString(template, new { A = true, B = true, C = true }));
        Assert.AreEqual("12!", renderer.RenderFromString(template, new { A = true, B = true, C = false }));
    }

    [TestMethod]
    public void UnclosedNestedIf_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$if(u.A)$if(u.B)X$endif", new { A = true, B = true }));
    }

    // ---------- $foreach ----------

    private static TestUser MakeUser() => new()
    {
        Name = "Вася",
        Age = 21,
        Books =
        [
            new TestBook { Name = "Война и мир", Author = "Толстой", Year = 1869 },
            new TestBook { Name = "Мастер и Маргарита", Author = "Булгаков", Year = 1967 },
        ],
    };

    [TestMethod]
    public void Foreach_RendersEachItem()
    {
        var template = "<ul>$foreach(var book in user.Books)<li>${book.Name}, ${book.Author}</li>$endfor</ul>";
        Assert.AreEqual(
            "<ul><li>Война и мир, Толстой</li><li>Мастер и Маргарита, Булгаков</li></ul>",
            renderer.RenderFromString(template, MakeUser()));
    }

    [TestMethod]
    public void Foreach_ExampleFromTask_Multiline()
    {
        var template = "<ul>\n$foreach(var book in user.Books)\n  <li>${book.Name}, ${book.Author}</li>\n$endfor\n</ul>";
        Assert.AreEqual(
            "<ul>\n\n  <li>Война и мир, Толстой</li>\n\n  <li>Мастер и Маргарита, Булгаков</li>\n\n</ul>",
            renderer.RenderFromString(template, MakeUser()));
    }

    [TestMethod]
    public void Foreach_CanUseRootModelInside()
    {
        var template = "$foreach(var book in user.Books)${user.Name}:${book.Year};$endfor";
        Assert.AreEqual("Вася:1869;Вася:1967;", renderer.RenderFromString(template, MakeUser()));
    }

    [TestMethod]
    public void Foreach_EmptyCollection_RendersNothing()
    {
        var template = "[$foreach(var book in user.Books)${book.Name}$endfor]";
        Assert.AreEqual("[]", renderer.RenderFromString(template, new TestUser()));
    }

    [TestMethod]
    public void Foreach_NullCollection_RendersNothing()
    {
        var template = "[$foreach(var book in user.Books)${book.Name}$endfor]";
        Assert.AreEqual("[]", renderer.RenderFromString(template, new { Books = (List<TestBook>?)null }));
    }

    [TestMethod]
    public void Foreach_OverPrimitives()
    {
        var template = "$foreach(var n in m.Numbers)(${n})$endfor";
        Assert.AreEqual("(1)(2)(3)", renderer.RenderFromString(template, new { Numbers = new[] { 1, 2, 3 } }));
    }

    [TestMethod]
    public void Foreach_ModelIsCollectionItself()
    {
        var template = "$foreach(var s in items)${s};$endfor";
        Assert.AreEqual("a;b;", renderer.RenderFromString(template, new List<string> { "a", "b" }));
    }

    [TestMethod]
    public void TwoForeachBlocks_BothProcessed()
    {
        var template = "$foreach(var b in u.Books)${b.Year}$endfor|$foreach(var b in u.Books)${b.Author}$endfor";
        Assert.AreEqual("18691967|ТолстойБулгаков", renderer.RenderFromString(template, MakeUser()));
    }

    [TestMethod]
    public void IfInsideForeach()
    {
        var template = "$foreach(var book in user.Books)$if(book.Year > 1900)${book.Name}$elseстарая$endif;$endfor";
        Assert.AreEqual("старая;Мастер и Маргарита;", renderer.RenderFromString(template, MakeUser()));
    }

    [TestMethod]
    public void ForeachInsideIf()
    {
        var template = "$if(user.Age >= 18)$foreach(var book in user.Books)${book.Year};$endfor$elseнельзя$endif";
        Assert.AreEqual("1869;1967;", renderer.RenderFromString(template, MakeUser()));

        var child = MakeUser();
        child.Age = 10;
        Assert.AreEqual("нельзя", renderer.RenderFromString(template, child));
    }

    [TestMethod]
    public void NestedForeach()
    {
        var library = new TestLibrary
        {
            Shelves =
            [
                new TestShelf { Title = "A", Books = [new TestBook { Name = "a1" }, new TestBook { Name = "a2" }] },
                new TestShelf { Title = "B", Books = [new TestBook { Name = "b1" }] },
            ],
        };
        var template = "$foreach(var shelf in lib.Shelves)${shelf.Title}:[$foreach(var book in shelf.Books)${book.Name}$endfor]$endfor";
        Assert.AreEqual("A:[a1a2]B:[b1]", renderer.RenderFromString(template, library));
    }

    [TestMethod]
    public void NestedForeach_WithIfInside()
    {
        var library = new TestLibrary
        {
            Shelves =
            [
                new TestShelf { Title = "A", Books = [new TestBook { Name = "a1", Year = 1800 }, new TestBook { Name = "a2", Year = 2000 }] },
                new TestShelf { Title = "B", Books = [new TestBook { Name = "b1", Year = 2010 }] },
            ],
        };
        var template = "$foreach(var shelf in lib.Shelves)$foreach(var book in shelf.Books)$if(book.Year >= 2000)${shelf.Title}${book.Name} $endif$endfor$endfor";
        Assert.AreEqual("Aa2 Bb1 ", renderer.RenderFromString(template, library));
    }

    [TestMethod]
    public void Foreach_NotACollection_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$foreach(var x in u.Age)${x}$endfor", new { Age = 5 }));
    }

    [TestMethod]
    public void Foreach_StringIsNotTreatedAsCollection_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$foreach(var x in u.Name)${x}$endfor", new { Name = "abc" }));
    }

    [TestMethod]
    public void Foreach_MissingEndfor_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$foreach(var b in u.Books)${b.Name}", MakeUser()));
    }

    [TestMethod]
    public void Foreach_BadHeader_Throws()
    {
        Assert.ThrowsExactly<TemplateRenderException>(() =>
            renderer.RenderFromString("$foreach(book in u.Books)x$endfor", MakeUser()));
    }

    // ---------- Файлы ----------

    [TestMethod]
    public void RenderFromFile_ReadsTemplate()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<p>${user.Name}</p>");
            Assert.AreEqual("<p>Вася</p>", renderer.RenderFromFile(path, MakeUser()));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void RenderToFile_WritesAndReturnsResult()
    {
        var input = Path.GetTempFileName();
        var output = Path.GetTempFileName();
        try
        {
            File.WriteAllText(input, "$foreach(var b in u.Books)${b.Year} $endfor");
            var result = renderer.RenderToFile(input, output, MakeUser());
            Assert.AreEqual("1869 1967 ", result);
            Assert.AreEqual(result, File.ReadAllText(output));
        }
        finally
        {
            File.Delete(input);
            File.Delete(output);
        }
    }
}
