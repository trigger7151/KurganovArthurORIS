namespace CardsApp.Web.ViewModels;

// Модели представлений: всё, что выводит шаблон, уже отформатировано строкой,
// а состояния (активный чип, выбранная опция) заданы булевыми флагами —
// так шаблоны остаются простыми: ${...}, $if(...) и $foreach(...).

/// <summary>Модель общего макета страницы (Templates/Shared/_Layout.html).</summary>
public class LayoutVm
{
    public string Title { get; set; } = "";
    public string SiteName { get; set; } = "Light Cone Wiki";
    public string BodyClass { get; set; } = "";
    public bool IsAdminArea { get; set; }
    public bool IsSignedIn { get; set; }
    public string UserName { get; set; } = "";
    public bool IsAdmin { get; set; }
    public List<string> Styles { get; set; } = [];
    public List<string> Scripts { get; set; } = [];
}

public class ChipVm
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsActive { get; set; }
}

public class OptionVm
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>Короткая подпись для мобильной версии («По редкости»).</summary>
    public string ShortLabel { get; set; } = "";
    public bool IsSelected { get; set; }
}

/// <summary>Карточка в сетке каталога. Те же поля отдаёт AJAX-эндпоинт /api/cones.</summary>
public class ConeCardVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Rarity { get; set; }
    public string Stars { get; set; } = "";
    public string PathName { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public bool HasImage { get; set; }
    public string Url { get; set; } = "";
}

public class CatalogPageVm
{
    public string Query { get; set; } = "";
    public string Rarity { get; set; } = "";
    public string Path { get; set; } = "";
    public string Sort { get; set; } = "";
    public string CountText { get; set; } = "";
    /// <summary>Короткий счётчик для мобильной версии («18 конусов»).</summary>
    public string CountShort { get; set; } = "";
    public string SortLabel { get; set; } = "";
    public string SortShortLabel { get; set; } = "";
    public bool HasItems { get; set; }
    public bool IsFiltered { get; set; }
    public List<ConeCardVm> Items { get; set; } = [];
    public List<ChipVm> RarityChips { get; set; } = [];
    public List<ChipVm> PathChips { get; set; } = [];
    public List<OptionVm> SortOptions { get; set; } = [];
}

public class StatLevelVm
{
    public string Label { get; set; } = "";
    public string Hp { get; set; } = "";
    public string Atk { get; set; } = "";
    public string Def { get; set; } = "";
    public bool IsActive { get; set; }
}

public class SuperimpositionVm
{
    public string Label { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsActive { get; set; }
}

public class ConeDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Rarity { get; set; }
    public string Stars { get; set; } = "";
    public string PathName { get; set; } = "";
    public string EffectName { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public bool HasImage { get; set; }
    public bool HasLevels { get; set; }
    public List<StatLevelVm> Levels { get; set; } = [];
    public StatLevelVm CurrentStats { get; set; } = new();
    public bool HasEffects { get; set; }
    public List<SuperimpositionVm> Superimpositions { get; set; } = [];
    public string CurrentEffectText { get; set; } = "";
    public bool HasDescription { get; set; }
    /// <summary>Очищенный HTML описания. Вставляется в страницу не шаблонизатором, а через слот raw (см. ViewResult).</summary>
    public string DescriptionHtml { get; set; } = "";
}

public class MessagePageVm
{
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
}
