using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sigo.WebApp.Controllers
{
    [Authorize]
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Home/Views/Index.cshtml");
        }
    }
}