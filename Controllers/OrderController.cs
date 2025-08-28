using Microsoft.AspNetCore.Mvc;
using ABCRetailer.Models;
using ABCRetailer.Models.ViewModels;
using ABCRetailer.Services;
using System.Text.Json;
using System.Linq;

namespace ABCRetailer.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();

            // Assign Username to each order based on CustomerId
            foreach (var order in orders)
            {
                var customer = customers.FirstOrDefault(c => c.CustomerId == order.CustomerId);
                if (customer != null)
                {
                    order.Username = customer.Name + " " + customer.Surname;
                }
            }

            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropDowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropDowns(model);
                        return View(model);
                    }

                    // Create order and set Username here
                    var order = new Order
                    {
                        PartitionKey = "Order",
                        RowKey = Guid.NewGuid().ToString(), 
                        //OrderId = Guid.NewGuid().ToString(),
                        CustomerId = model.CustomerId,
                        Username = customer.Name + " " + customer.Surname, // Set username here
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted", // Always starts as Submitted
                        OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc)

                    };

                    await _storageService.AddEntityAsync(order);

                    // Update product stock
                    product.StockAvailable -= model.Quantity; // Fix: Subtract quantity from stock
                    await _storageService.UpdateEntityAsync(product);

                    // Send message for new order
                    var orderMessage = new
                    {
                        OrderId = order.RowKey, 
                        CustomerId = customer.CustomerId,
                        CustomerName = customer.Name + " " + customer.Surname,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        Status = order.Status
                    };

                    await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(orderMessage));

                    // Send stock update message
                    var stockMessage = new
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        PreviousStock = product.StockAvailable + model.Quantity, // previous stock before update
                        NewStock = product.StockAvailable,
                        UpdatedBy = "Order System",
                        UpdateDate = DateTime.UtcNow
                    };

                    await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                    await PopulateDropDowns(model);
                    return View(model);
                }
            }

            await PopulateDropDowns(model);
            return View(model);
        }

        private async Task PopulateDropDowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve original order to preserve metadata
                    var originalOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    if (originalOrder == null)
                    {
                        return NotFound();
                    }

                    // Update fields
                    originalOrder.Quantity = order.Quantity;
                    originalOrder.Status = order.Status;
                    originalOrder.TotalPrice = order.UnitPrice * order.Quantity;

                    await _storageService.UpdateEntityAsync(originalOrder);

                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
    


[HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var previousStatus = order.Status;
                order.Status = newStatus;
                await _storageService.UpdateEntityAsync(order);

                // Send queue message for status update
                var statusMessage = new
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Username,
                    ProductName = order.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
