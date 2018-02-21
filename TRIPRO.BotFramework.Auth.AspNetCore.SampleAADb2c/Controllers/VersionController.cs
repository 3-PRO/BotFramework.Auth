using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Controllers
{
    [Route("api/[controller]")]
    public class VersionController : Controller
    {

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetVersion()
        {
            return new OkObjectResult(new { Version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion });
        }


    }
}
