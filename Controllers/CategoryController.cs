using Microsoft.AspNetCore.Mvc;

namespace Retetar.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
