using Microsoft.AspNetCore.Mvc;


namespace RecruitmentTracker.Controllers
{

    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }


    }

}