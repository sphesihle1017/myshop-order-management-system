// Controllers/CheckoutController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.Data;
using MyShop.Models;
using System.Globalization;

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
                    // Set order details
                    checkout.OrderDate = DateTime.Now;
                    checkout.OrderStatus = "Pending";
                    checkout.PaymentStatus = "Pending";

                    // Debug: Check the total value
                    Console.WriteLine($"Order Total before save: {checkout.Total}");

                    // Store order details for confirmation page
                    // IMPORTANT: Store as string to avoid serialization issues
                    TempData["OrderId"] = checkout.OrderId.ToString();
                    TempData["OrderTotalString"] = checkout.Total.ToString("F2", CultureInfo.InvariantCulture); // Store as invariant string
                    TempData["OrderDate"] = checkout.OrderDate.ToString("o"); // ISO format

                    // Save to database
                    _context.Add(checkout);
                    await _context.SaveChangesAsync();

                    // Get the actual order ID after save
                    var orderId = checkout.OrderId;
                    var orderTotal = checkout.Total;

                    // Update TempData with actual saved values
                    TempData["OrderId"] = orderId.ToString();
                    TempData["OrderTotalString"] = orderTotal.ToString("F2", CultureInfo.InvariantCulture);

                    // Keep these in session too as backup
                    HttpContext.Session.SetString("LastOrderId", orderId.ToString());
                    HttpContext.Session.SetString("LastOrderTotal", orderTotal.ToString("F2", CultureInfo.InvariantCulture));

                    return RedirectToAction("Confirmation", new { id = orderId });
                }
                catch (Exception ex)
                {
                    // Log error
                    Console.WriteLine($"Checkout error: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while processing your order. Please try again.");
                }
            }

            return View(checkout);
        }

        // GET: /Checkout/Confirmation/{id}
        public async Task<IActionResult> Confirmation(int id)
        {
            decimal total = 0m;
            var orderId = id;

            try
            {
                // Try to get the order from database first
                var order = await _context.Checkouts
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order != null)
                {
                    // Use actual order data from database
                    orderId = order.OrderId;
                    total = order.Total;
                }
                else
                {
                    // If not found in DB, try TempData
                    if (TempData["OrderTotalString"] != null)
                    {
                        // Parse the string back to decimal
                        if (decimal.TryParse(TempData["OrderTotalString"].ToString(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out decimal parsedTotal))
                        {
                            total = parsedTotal;
                        }
                    }
                    else if (TempData["OrderTotal"] != null)
                    {
                        // Try to parse whatever is in TempData
                        var totalValue = TempData["OrderTotal"].ToString();
                        if (decimal.TryParse(totalValue, out decimal tempTotal))
                        {
                            total = tempTotal;
                        }
                    }

                    // Try session as last resort
                    if (total == 0m)
                    {
                        var sessionTotal = HttpContext.Session.GetString("LastOrderTotal");
                        if (!string.IsNullOrEmpty(sessionTotal) &&
                            decimal.TryParse(sessionTotal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal sessionTotalDecimal))
                        {
                            total = sessionTotalDecimal;
                        }
                    }
                }

                // Pass to view
                ViewBag.OrderId = orderId;
                ViewBag.OrderTotal = total;

                // Clear session data
                HttpContext.Session.Remove("LastOrderId");
                HttpContext.Session.Remove("LastOrderTotal");
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Confirmation error: {ex.Message}");
                ViewBag.OrderId = orderId;
                ViewBag.OrderTotal = 0m;
            }

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
                decimal subtotal = cartItems.Sum(item => item.Price * item.Quantity);
                decimal tax = subtotal * 0.15m; // 15% VAT for South Africa
                decimal shipping = subtotal > 500 ? 0 : 50; // Free shipping over R500
                decimal total = subtotal + tax + shipping;

                return Json(new
                {
                    success = true,
                    subtotal = subtotal.ToString("F2", CultureInfo.InvariantCulture),
                    tax = tax.ToString("F2", CultureInfo.InvariantCulture),
                    shipping = shipping.ToString("F2", CultureInfo.InvariantCulture),
                    total = total.ToString("F2", CultureInfo.InvariantCulture),
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

    // Add this ViewModel class if not exists
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}