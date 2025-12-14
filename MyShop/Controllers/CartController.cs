// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MyShop.Models;

namespace MyShop.Controllers
{
    public class CartController : Controller
    {
        // GET: /Cart
        public IActionResult Index()
        {

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
        // GET: /Cart/Checkout
        public IActionResult Checkout()
        {
            ViewBag.IsClientSideCart = true; // Keep consistent with Index
            return View();
        }

    }

}