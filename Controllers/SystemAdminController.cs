
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class SystemAdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Roles()
        {
            return RedirectToAction("Index", "Role");
        }
    }
}

