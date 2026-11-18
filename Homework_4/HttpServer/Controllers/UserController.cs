using System;
namespace HttpServer.Controllers;

[Route("/user")]
public class UserController : ControllerBase
{

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

