using Microsoft.AspNetCore.Mvc;

namespace ABCRetailer.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
