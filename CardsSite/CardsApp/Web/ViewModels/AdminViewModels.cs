namespace CardsApp.Web.ViewModels;

/// <summary>Поле формы: значение и сообщение об ошибке для повторного показа формы.</summary>
public class FieldVm
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Error { get; set; } = "";
    public bool HasError => Error.Length > 0;

    public static FieldVm Of(string name, string label, string? value, ValidationErrors? errors = null)
        => new() { Name = name, Label = label, Value = value ?? "", Error = errors?.Get(name) ?? "" };
}

public class LoginPageVm
{
    public FieldVm Login { get; set; } = FieldVm.Of("login", "Логин", "");
    public FieldVm Password { get; set; } = FieldVm.Of("password", "Пароль", "");
    public string ReturnUrl { get; set; } = "/admin";
    public string Error { get; set; } = "";
    public bool HasError => Error.Length > 0;
}

public class AdminConeRowVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Rarity { get; set; }
    public string Stars { get; set; } = "";
    public string PathName { get; set; } = "";
    public string EditUrl { get; set; } = "";
    public string ViewUrl { get; set; } = "";
}

public class AdminConesPageVm
{
    public List<AdminConeRowVm> Items { get; set; } = [];
    public string CountText { get; set; } = "";
    public bool HasItems => Items.Count > 0;
    public string Notice { get; set; } = "";
    public bool HasNotice => Notice.Length > 0;
    public bool NoticeIsError { get; set; }
}

public class ConeFormVm
{
    public int Id { get; set; }
    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "Новый световой конус" : "Редактирование конуса";
    public string ViewUrl => $"/cone?id={Id}";
    public FieldVm Name { get; set; } = new();
    public FieldVm Rarity { get; set; } = new();
    public FieldVm PathId { get; set; } = new();
    public FieldVm EffectName { get; set; } = new();
    public FieldVm ImageUrl { get; set; } = new();
    public FieldVm Description { get; set; } = new();
    public List<FieldVm> Stats1 { get; set; } = [];
    public List<FieldVm> Stats80 { get; set; } = [];
    public List<FieldVm> Effects { get; set; } = [];
    public List<OptionVm> RarityOptions { get; set; } = [];
    public List<OptionVm> PathOptions { get; set; } = [];
    public string Error { get; set; } = "";
    public bool HasError => Error.Length > 0;
}

public class AdminPathRowVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ConesCount { get; set; } = "";
    public string EditUrl { get; set; } = "";
}

public class AdminPathsPageVm
{
    public List<AdminPathRowVm> Items { get; set; } = [];
    public string Notice { get; set; } = "";
    public bool HasNotice => Notice.Length > 0;
    public bool NoticeIsError { get; set; }
}

public class PathFormVm
{
    public int Id { get; set; }
    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "Новый путь" : "Редактирование пути";
    public FieldVm Name { get; set; } = new();
    public FieldVm Slug { get; set; } = new();
    public FieldVm Description { get; set; } = new();
    public string Error { get; set; } = "";
    public bool HasError => Error.Length > 0;
}
