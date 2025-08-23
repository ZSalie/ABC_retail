using Microsoft.AspNetCore.Mvc;

namespace ABCRetailer.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
