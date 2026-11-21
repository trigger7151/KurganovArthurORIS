using System;
namespace HttpServer.Controllers;

/// <summary>
/// Страницы пользователя.
/// </summary>
[Route("/user")]
public class UserController : ControllerBase
{

    /// <summary>
    /// GET /user/profile — личный кабинет. Доступен только с действующим сеансом (<see cref="AuthorizeAttribute"/>).
    /// </summary>
    /// <returns>Страница, построенная по шаблону <c>Templates/User/Profile.html</c>.</returns>
    [Route("/profile")]
    [Method("GET")]
    [Authorize]
    public async Task<IHttpResult> ProcessProfile()
    {
        var user = new {
            Name = "Alex",
            Age = 20,
            Email = "alex@example.com"
        };
        return Page("User/Profile.html", user);
    }

}

