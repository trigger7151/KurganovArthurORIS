using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Results;

namespace HttpServer.Controllers;

/// <summary>Вход и выход из административной панели.</summary>
[Route("/admin")]
public class AuthController : AppController
{
    [Method("GET")]
    [Route("login")]
    public Task<IHttpResult> ProcessLoginForm(string? returnUrl)
        => SafeAsync(async () =>
        {
            if (await GetCurrentUserAsync() is not null)
                return Redirect(SafeReturnUrl(returnUrl));
            return LoginView(new LoginPageVm { ReturnUrl = SafeReturnUrl(returnUrl) });
        });

    [Method("POST")]
    [Route("login")]
    public Task<IHttpResult> ProcessLogin(string? login, string? password, string? returnUrl)
        => SafeAsync(async () =>
        {
            var errors = new ValidationErrors();
            var loginText = errors.Text("login", login, "Логин", 20, ValidationRules.Login(), minLength: 3,
                ruleMessage: "Логин: латинские буквы, цифры и _, от 3 до 20 символов.");
            if (string.IsNullOrEmpty(password))
                errors.Add("password", "Введите пароль.");

            User? user = null;
            if (errors.IsValid)
                user = await Services.Auth.SignInAsync(loginText, password, Ct);

            if (user is null)
            {
                return LoginView(new LoginPageVm
                {
                    Login = FieldVm.Of("login", "Логин", loginText, errors),
                    Password = FieldVm.Of("password", "Пароль", "", errors),
                    ReturnUrl = SafeReturnUrl(returnUrl),
                    Error = errors.IsValid ? "Неверный логин или пароль." : "",
                }, 400);
            }

            SetSessionCookie(SessionService.CreateSession(user.Id));
            return Redirect(SafeReturnUrl(returnUrl));
        });

    [Method("POST")]
    [Route("logout")]
    public Task<IHttpResult> ProcessLogout()
        => SafeAsync(() =>
        {
            SessionService.RemoveSession(SessionId);
            ClearSessionCookie();
            return Task.FromResult(Redirect("/admin/login"));
        });

    private IHttpResult LoginView(LoginPageVm model, int statusCode = 200)
        => View("Admin/Login.html", model, "Вход в админку", statusCode,
            scripts: ["/js/validation.js"], bodyClass: "admin admin-login", adminArea: true);

    /// <summary>Возвращать можно только на страницы админки этого же сайта (защита от открытого редиректа).</summary>
    private static string SafeReturnUrl(string? url)
        => url is not null && url.StartsWith("/admin", StringComparison.Ordinal) && !url.StartsWith("//") && !url.Contains('\\')
            && !url.StartsWith("/admin/login", StringComparison.OrdinalIgnoreCase)
            ? url : "/admin";
}

/// <summary>Страница 3: административная панель — управление световыми конусами.</summary>
[Route("/admin")]
public class AdminController : AdminAreaController
{
    private static readonly string[] FormScripts = ["/vendor/quill/quill.js", "/js/validation.js", "/js/admin.js"];

    [Method("GET")]
    [Route("")]
    public Task<IHttpResult> ProcessIndex(string? msg)
        => AuthorizedAsync(async user =>
        {
            var rows = await Services.ConesAdmin.ListAsync(Ct);
            var (notice, isError) = Notice(msg);
            var model = new AdminConesPageVm
            {
                Items = rows.Select(r => new AdminConeRowVm
                {
                    Id = r.Cone.Id,
                    Name = r.Cone.Name,
                    Rarity = r.Cone.Rarity,
                    Stars = CatalogService.Stars(r.Cone.Rarity),
                    PathName = r.PathName,
                    EditUrl = $"/admin/cone?id={r.Cone.Id}",
                    ViewUrl = $"/cone?id={r.Cone.Id}",
                }).ToList(),
                CountText = $"Всего: {rows.Count}",
                CanDelete = user.Role == UserRoles.Admin,
                Notice = notice,
                NoticeIsError = isError,
            };
            return AdminView("Admin/Index.html", model, "Световые конусы", user, scripts: ["/js/admin.js"]);
        });

    [Method("GET")]
    [Route("cone")]
    public Task<IHttpResult> ProcessConeForm(string? id)
        => AuthorizedAsync(async user =>
        {
            var coneId = ParseId(id);
            ConeInput? input;
            if (coneId == 0)
            {
                input = new ConeInput(0, "", "", "", "", "", "", ["", "", ""], ["", "", ""], new string?[5]);
            }
            else
            {
                input = await Services.ConesAdmin.GetInputAsync(coneId, Ct);
                if (input is null)
                    return MessagePage(404, "Конус не найден", "Возможно, его уже удалили.", user, adminArea: true);
            }
            return await ConeFormView(user, input, new ValidationErrors());
        });

    [Method("POST")]
    [Route("cone")]
    public Task<IHttpResult> ProcessConeSave()
        => AuthorizedAsync(async user =>
        {
            var form = await ReadFormAsync();
            var input = new ConeInput(
                ParseId(form["id"]), form["name"], form["rarity"], form["pathId"], form["effectName"], form["imageUrl"],
                form["description"],
                LightConeAdminService.StatNames.Select(s => form[$"{s}{StatLevels.Min}"]).ToArray(),
                LightConeAdminService.StatNames.Select(s => form[$"{s}{StatLevels.Max}"]).ToArray(),
                Enumerable.Range(1, LightConeAdminService.EffectCount).Select(l => form[$"s{l}"]).ToArray());

            var result = await Services.ConesAdmin.SaveAsync(input, Ct);
            if (result.Success)
                return Redirect($"/admin?msg={(input.Id == 0 ? "created" : "saved")}");
            return await ConeFormView(user, input, result.Errors, 400);
        });

    [Method("POST")]
    [Route("cone/delete")]
    public Task<IHttpResult> ProcessConeDelete(string? id)
        => AuthorizedAsync(async _ =>
        {
            var deleted = await Services.ConesAdmin.DeleteAsync(ParseId(id), Ct);
            return Redirect(deleted ? "/admin?msg=deleted" : "/admin?msg=notfound");
        }, adminOnly: true);

    /// <summary>
    /// Загрузка арта через AJAX: тело запроса — сам файл (Content-Type: image/png и т. п.).
    /// Атрибут [Authorize] фреймворка отвечает 401 в JSON, что и нужно для fetch-запроса.
    /// </summary>
    [Authorize]
    [Method("POST")]
    [Route("upload")]
    public Task<IHttpResult> ProcessUpload()
        => SafeAsync(async () =>
        {
            var (url, error) = await Services.Images.SaveAsync(
                Request.InputStream, Request.ContentType, Request.ContentLength64, Ct);
            return url is null
                ? JsonResponse(new { success = false, message = error }, 400)
                : JsonResponse(new { success = true, url });
        }, json: true);

    private async Task<IHttpResult> ConeFormView(User user, ConeInput input, ValidationErrors errors, int statusCode = 200)
    {
        var paths = await Services.PathsAdmin.GetAllAsync(Ct);
        FieldVm F(string name, string label, string? value) => FieldVm.Of(name, label, value, errors);
        var statLabels = new[] { "HP", "Атака", "Защита" };

        var model = new ConeFormVm
        {
            Id = input.Id,
            Name = F("name", "Название", input.Name),
            Rarity = F("rarity", "Редкость", input.Rarity),
            PathId = F("pathId", "Путь", input.PathId),
            EffectName = F("effectName", "Название эффекта", input.EffectName),
            ImageUrl = F("imageUrl", "Арт (902×1260)", input.ImageUrl),
            Description = F("description", "Описание", input.DescriptionHtml),
            Stats1 = LightConeAdminService.StatNames.Select((s, i) =>
                F($"{s}{StatLevels.Min}", statLabels[i], input.Stats1.ElementAtOrDefault(i))).ToList(),
            Stats80 = LightConeAdminService.StatNames.Select((s, i) =>
                F($"{s}{StatLevels.Max}", statLabels[i], input.Stats80.ElementAtOrDefault(i))).ToList(),
            Effects = Enumerable.Range(1, LightConeAdminService.EffectCount).Select(l =>
                F($"s{l}", $"S{l}", input.Effects.ElementAtOrDefault(l - 1))).ToList(),
            RarityOptions = new[] { "5", "4", "3" }.Select(r => new OptionVm
            {
                Value = r, Label = $"{r}★ — {CatalogService.Stars(int.Parse(r))}", IsSelected = r == input.Rarity,
            }).ToList(),
            PathOptions = paths.OrderBy(p => p.Id).Select(p => new OptionVm
            {
                Value = p.Id.ToString(), Label = p.Name, IsSelected = p.Id.ToString() == input.PathId,
            }).ToList(),
            Error = errors.Get("form") is { Length: > 0 } formError ? formError
                : errors.IsValid ? "" : "Проверьте поля, отмеченные красным.",
        };
        return AdminView("Admin/ConeEdit.html", model, model.PageTitle, user, statusCode, FormScripts);
    }

    internal static (string Text, bool IsError) Notice(string? code) => code switch
    {
        "created" => ("Световой конус добавлен.", false),
        "saved" => ("Изменения сохранены.", false),
        "deleted" => ("Световой конус удалён.", false),
        "notfound" => ("Запись не найдена: возможно, её уже удалили.", true),
        _ => ("", false),
    };
}

/// <summary>Управление путями — только для администратора.</summary>
[Route("/admin/paths")]
public class AdminPathsController : AdminAreaController
{
    [Method("GET")]
    [Route("")]
    public Task<IHttpResult> ProcessIndex(string? msg)
        => AuthorizedAsync(async user =>
        {
            var rows = await Services.PathsAdmin.ListAsync(Ct);
            var (notice, isError) = msg switch
            {
                "created" => ("Путь добавлен.", false),
                "saved" => ("Изменения сохранены.", false),
                "deleted" => ("Путь удалён.", false),
                "inuse" => ("Нельзя удалить путь, к которому относятся световые конусы. Сначала перенесите их на другой путь.", true),
                _ => AdminController.Notice(msg),
            };
            var model = new AdminPathsPageVm
            {
                Items = rows.Select(r => new AdminPathRowVm
                {
                    Id = r.Path.Id, Name = r.Path.Name, Slug = r.Path.Slug,
                    ConesCount = r.ConesCount.ToString(), EditUrl = $"/admin/paths/edit?id={r.Path.Id}",
                }).ToList(),
                Notice = notice,
                NoticeIsError = isError,
            };
            return AdminView("Admin/Paths.html", model, "Пути", user, scripts: ["/js/admin.js"]);
        }, adminOnly: true);

    [Method("GET")]
    [Route("edit")]
    public Task<IHttpResult> ProcessForm(string? id)
        => AuthorizedAsync(async user =>
        {
            var pathId = ParseId(id);
            var path = pathId == 0 ? new CombatPath() : await Services.PathsAdmin.GetAsync(pathId, Ct);
            if (path is null)
                return MessagePage(404, "Путь не найден", "Возможно, его уже удалили.", user, adminArea: true);
            return FormView(user, new PathInput(path.Id, path.Name, path.Slug, path.Description), new ValidationErrors());
        }, adminOnly: true);

    [Method("POST")]
    [Route("edit")]
    public Task<IHttpResult> ProcessSave(string? id, string? name, string? slug, string? description)
        => AuthorizedAsync(async user =>
        {
            var input = new PathInput(ParseId(id), name, slug, description);
            var result = await Services.PathsAdmin.SaveAsync(input, Ct);
            return result.Success
                ? Redirect($"/admin/paths?msg={(input.Id == 0 ? "created" : "saved")}")
                : FormView(user, input, result.Errors, 400);
        }, adminOnly: true);

    [Method("POST")]
    [Route("delete")]
    public Task<IHttpResult> ProcessDelete(string? id)
        => AuthorizedAsync(async _ =>
        {
            var error = await Services.PathsAdmin.DeleteAsync(ParseId(id), Ct);
            return Redirect(error is null ? "/admin/paths?msg=deleted" : "/admin/paths?msg=inuse");
        }, adminOnly: true);

    private IHttpResult FormView(User user, PathInput input, ValidationErrors errors, int statusCode = 200)
    {
        var model = new PathFormVm
        {
            Id = input.Id,
            Name = FieldVm.Of("name", "Название", input.Name, errors),
            Slug = FieldVm.Of("slug", "Ключ (латиница)", input.Slug, errors),
            Description = FieldVm.Of("description", "Описание", input.Description, errors),
            Error = errors.Get("form") is { Length: > 0 } e ? e : errors.IsValid ? "" : "Проверьте поля, отмеченные красным.",
        };
        return AdminView("Admin/PathEdit.html", model, model.PageTitle, user, statusCode,
            ["/js/validation.js", "/js/admin.js"]);
    }
}
