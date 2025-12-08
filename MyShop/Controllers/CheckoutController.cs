// Controllers/CheckoutController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.Data;
using MyShop.Models;
using System.Text.Json;

namespace MyShop.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Checkout
        public IActionResult Index()
        {
            // Get cart from localStorage via ViewData (will be populated by JavaScript)
            ViewBag.HasItems = true; // Will be set by JavaScript

            var checkout = new Checkout
            {
                Country = "South Africa"
            };

            return View(checkout);
        }

        // POST: /Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Checkout checkout)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Calculate totals
                    checkout.OrderDate = DateTime.Now;
                    checkout.OrderStatus = "Processing";

                    // Save to database
                    _context.Add(checkout);
                    await _context.SaveChangesAsync();

                    // Get order ID
                    var orderId = checkout.OrderId;

                    // Clear cart after successful checkout
                    TempData["OrderId"] = orderId;
                    TempData["OrderTotal"] = checkout.Total.ToString("C");

                    return RedirectToAction("Confirmation", new { id = orderId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while processing your order. Please try again.");
                    // Log the error
                }
            }

            return View(checkout);
        }

        // GET: /Checkout/Confirmation/{id}
        public IActionResult Confirmation(int id)
        {
            ViewBag.OrderId = id;
            ViewBag.OrderTotal = TempData["OrderTotal"] ?? "R 0.00";

            return View();
        }

        // GET: /Checkout/CalculateTotals (AJAX endpoint)
        [HttpPost]
        public IActionResult CalculateTotals([FromBody] List<CartItemViewModel> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Cart is empty"
                });
            }

            try
            {
                decimal subtotal = cartItems.Sum(item => item.Total);
                decimal tax = subtotal * 0.15m; // 15% VAT for South Africa
                decimal shipping = subtotal > 500 ? 0 : 50; // Free shipping over R500
                decimal total = subtotal + tax + shipping;

                return Json(new
                {
                    success = true,
                    subtotal = subtotal.ToString("0.00", new System.Globalization.CultureInfo("en-ZA")),
                    tax = tax.ToString("0.00", new System.Globalization.CultureInfo("en-ZA")),
                    shipping = shipping.ToString("0.00", new System.Globalization.CultureInfo("en-ZA")),
                    total = total.ToString("0.00", new System.Globalization.CultureInfo("en-ZA")),
                    itemCount = cartItems.Sum(item => item.Quantity)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error calculating totals"
                });
            }
        }

        // GET: /Checkout/Order/{id}
        public async Task<IActionResult> Order(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}