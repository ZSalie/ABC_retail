using ABCRetailer.Models;
using ABCRetailer.Services;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
//using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public HomeController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            //var products = await _storageService.GetAllEntitiesAsync<Product>();
            //var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            //var orders = await _storageService.GetAllEntitiesAsync<Order>();

            var viewModel = new HomeViewModel
            {
                //FeaturedProducts = products.Take(5).ToList(),
                //ProductCount = products.Count,
                //CustomerCount = customers.Count,
                //OrderCount = orders.Count
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
       /* public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                await _storageService.GetAllEntitiesAsync<Customer>(); // This will trigger initialization
                TempData["Success"] = "Azure Storage initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to initialize storage: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }*/

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
