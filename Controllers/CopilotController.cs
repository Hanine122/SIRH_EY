using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SIRH.EY.Controllers
{
    [Authorize]
    public class CopilotController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "HR Copilot";
            return View();
        }
    }
}
