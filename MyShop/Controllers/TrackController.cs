// Controllers/TrackController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.Data;
using MyShop.Models;

namespace MyShop.Controllers
{
    public class TrackController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrackController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Track
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Track/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string orderId, string email)
        {
            if (string.IsNullOrEmpty(orderId) && string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter either an Order ID or Email address");
                return View("Index");
            }

            try
            {
                List<Checkout> orders = new List<Checkout>();

                if (!string.IsNullOrEmpty(orderId))
                {
                    // Search by order ID
                    if (int.TryParse(orderId, out int id))
                    {
                        var order = await _context.Checkouts
                            .Include(o => o.OrderItems)
                            .FirstOrDefaultAsync(o => o.OrderId == id);

                        if (order != null)
                        {
                            orders.Add(order);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(email))
                {
                    // Search by email
                    var emailOrders = await _context.Checkouts
                        .Include(o => o.OrderItems)
                        .Where(o => o.Email.ToLower() == email.ToLower())
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();

                    if (emailOrders.Any())
                    {
                        orders.AddRange(emailOrders);
                    }
                }

                // Remove duplicates
                orders = orders.DistinctBy(o => o.OrderId).ToList();

                if (orders.Any())
                {
                    ViewBag.SearchTerm = !string.IsNullOrEmpty(orderId) ? $"Order #{orderId}" : $"Email: {email}";
                    return View("Results", orders);
                }
                else
                {
                    ModelState.AddModelError("", "No orders found. Please check your Order ID or Email address.");
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while searching for your order. Please try again.");
                return View("Index");
            }
        }

        // GET: /Track/Details/{id}
        public async Task<IActionResult> Details(int id)
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

        // GET: /Track/Timeline/{id} - JSON endpoint for timeline
        public async Task<IActionResult> Timeline(int id)
        {
            var order = await _context.Checkouts.FindAsync(id);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            var timeline = GetOrderTimeline(order);

            return Json(new
            {
                success = true,
                timeline = timeline,
                trackingNumber = order.TrackingNumber,
                shippingCarrier = order.ShippingCarrier,
                estimatedDelivery = order.EstimatedDelivery?.ToString("dd MMM yyyy")
            });
        }

        // Generate order timeline
        private List<TimelineEvent> GetOrderTimeline(Checkout order)
        {
            var timeline = new List<TimelineEvent>();

            // Order Placed
            timeline.Add(new TimelineEvent
            {
                Title = "Order Placed",
                Description = "Your order has been received",
                Date = order.OrderDate,
                Status = "completed",
                Icon = "bi-cart-check"
            });

            // Order Confirmed (1 hour after order)
            timeline.Add(new TimelineEvent
            {
                Title = "Order Confirmed",
                Description = "Your order has been confirmed",
                Date = order.OrderDate.AddHours(1),
                Status = order.OrderDate.AddHours(1) <= DateTime.Now ? "completed" : "pending",
                Icon = "bi-check-circle"
            });

            // Processing (next day)
            timeline.Add(new TimelineEvent
            {
                Title = "Processing",
                Description = "Your order is being prepared",
                Date = order.OrderDate.AddDays(1),
                Status = order.OrderDate.AddDays(1) <= DateTime.Now ? "completed" : "pending",
                Icon = "bi-gear"
            });

            // Shipped (2 days after order or based on tracking)
            var shippedDate = !string.IsNullOrEmpty(order.TrackingNumber) ? order.OrderDate.AddDays(2) : order.OrderDate.AddDays(2);
            timeline.Add(new TimelineEvent
            {
                Title = "Shipped",
                Description = !string.IsNullOrEmpty(order.TrackingNumber)
                    ? $"Shipped via {order.ShippingCarrier}"
                    : "Your order has been shipped",
                Date = shippedDate,
                Status = shippedDate <= DateTime.Now ? "completed" : "pending",
                Icon = "bi-truck",
                TrackingNumber = order.TrackingNumber
            });

            // Out for Delivery
            var deliveryDate = order.EstimatedDelivery ?? order.OrderDate.AddDays(3);
            timeline.Add(new TimelineEvent
            {
                Title = "Out for Delivery",
                Description = "Your order is out for delivery",
                Date = deliveryDate,
                Status = deliveryDate <= DateTime.Now ? "completed" : "pending",
                Icon = "bi-box-arrow-in-right"
            });

            // Delivered
            timeline.Add(new TimelineEvent
            {
                Title = "Delivered",
                Description = order.ActualDelivery.HasValue
                    ? $"Delivered on {order.ActualDelivery.Value.ToString("dd MMM yyyy")}"
                    : "Expected delivery",
                Date = order.ActualDelivery ?? deliveryDate,
                Status = order.ActualDelivery.HasValue ? "completed" : "pending",
                Icon = "bi-house-check"
            });

            return timeline;
        }

        // GET: /Track/Subscribe - Subscribe to updates
        public IActionResult Subscribe(int orderId, string email)
        {
            ViewBag.OrderId = orderId;
            ViewBag.Email = email;
            return View();
        }

        // POST: /Track/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubscribeNotification(int orderId, string email)
        {
            // In a real app, you would save this to a database
            // For now, just show a success message

            TempData["SuccessMessage"] = $"You will receive email updates for order #{orderId} to {email}";
            return RedirectToAction("Details", new { id = orderId });
        }
    }

    // Timeline event model
    public class TimelineEvent
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } // completed, current, pending
        public string Icon { get; set; }
        public string? TrackingNumber { get; set; }
        public string? TrackingUrl { get; set; }
    }
}