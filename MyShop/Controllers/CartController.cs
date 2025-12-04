// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyShop.Controllers
{
    public class CartController : Controller
    {
        // GET: /Cart
        public IActionResult Index()
        {
            // For server-side cart, you would retrieve from session/database
            // But since we're using localStorage, just return an empty view
            // The JavaScript will populate the cart from localStorage

            // Optionally, you could pass a flag to indicate cart is client-side
            ViewBag.IsClientSideCart = true;
            return View();
        }

        // API Endpoints for server-side cart (optional)

        // GET: /Cart/GetCart
        [HttpGet]
        public IActionResult GetCart()
        {
            // If you want to sync localStorage with server, implement this
            return Json(new
            {
                message = "Cart is managed client-side using localStorage",
                hasServerCart = false
            });
        }

        // POST: /Cart/Sync
        [HttpPost]
        public IActionResult Sync([FromBody] List<CartItem> cartItems)
        {
            // Optional: Sync localStorage cart with server/database
            // This would require authentication and a database table for cart items

            return Json(new
            {
                success = true,
                message = "Cart synced successfully",
                itemCount = cartItems?.Count ?? 0
            });
        }
    }

    // Cart Item Model
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }
}