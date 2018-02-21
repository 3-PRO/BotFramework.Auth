using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Controllers
{
    [AllowAnonymous]
    [Route("/")]
    public class RootController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
