// Controllers/OrderManagerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyShop.Data;
using MyShop.Models;

namespace MyShop.Controllers
{
    [Authorize]
    public class OrderManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderManagerController> _logger;

        public OrderManagerController(ApplicationDbContext context, ILogger<OrderManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /OrderManager
        public async Task<IActionResult> Index(string status = "all", string sort = "newest", string search = "")
        {
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSort = sort;
            ViewBag.SearchTerm = search;

            // To this (if you want to include OrderItems):
            var orders = _context.Checkouts.Include(o => o.OrderItems).AsQueryable();
            // OR this (if you just want orders without items temporarily):
            

            

            // Filter by status
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                orders = orders.Where(o => o.OrderStatus == status);
            }

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o =>
                    o.OrderId.ToString().Contains(search) ||
                    o.FirstName.Contains(search) ||
                    o.LastName.Contains(search) ||
                    o.Email.Contains(search) ||
                    o.Phone.Contains(search) ||
                    o.TrackingNumber.Contains(search) ||
                    o.InternalReference.Contains(search));
            }

            // Sort
            switch (sort)
            {
                case "oldest":
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
                case "price-high":
                    orders = orders.OrderByDescending(o => o.Total);
                    break;
                case "price-low":
                    orders = orders.OrderBy(o => o.Total);
                    break;
                case "name":
                    orders = orders.OrderBy(o => o.LastName).ThenBy(o => o.FirstName);
                    break;
                default: // newest
                    orders = orders.OrderByDescending(o => o.OrderDate);
                    break;
            }

            var orderList = await orders.ToListAsync();

            // Get statistics
            ViewBag.TotalOrders = await _context.Checkouts.CountAsync();
            ViewBag.PendingOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Pending");
            ViewBag.ProcessingOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Processing");
            ViewBag.ShippedOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Shipped");
            ViewBag.DeliveredOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Delivered");
            ViewBag.TodayOrders = await _context.Checkouts.CountAsync(o => o.OrderDate.Date == DateTime.Today);
            ViewBag.TotalRevenue = await _context.Checkouts.Where(o => o.PaymentStatus == "Paid").SumAsync(o => o.Total);

            return View(orderList);
        }

        // GET: /OrderManager/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Get order history (in a real app, you'd have an OrderHistory table)
            ViewBag.OrderHistory = GetOrderHistory(id);

            return View(order);
        }

        // GET: /OrderManager/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Checkouts.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            // Prepare dropdown lists
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "Pending", Text = "Pending" },
                new { Value = "Processing", Text = "Processing" },
                new { Value = "Shipped", Text = "Shipped" },
                new { Value = "Delivered", Text = "Delivered" },
                new { Value = "Cancelled", Text = "Cancelled" },
                new { Value = "Refunded", Text = "Refunded" },
                new { Value = "On Hold", Text = "On Hold" }
            }, "Value", "Text", order.OrderStatus);

            ViewBag.PaymentStatusList = new SelectList(new[]
            {
                new { Value = "Pending", Text = "Pending" },
                new { Value = "Paid", Text = "Paid" },
                new { Value = "Failed", Text = "Failed" },
                new { Value = "Refunded", Text = "Refunded" },
                new { Value = "Partially Refunded", Text = "Partially Refunded" }
            }, "Value", "Text", order.PaymentStatus);

            ViewBag.PriorityList = new SelectList(new[]
            {
                new { Value = "Low", Text = "Low" },
                new { Value = "Normal", Text = "Normal" },
                new { Value = "High", Text = "High" },
                new { Value = "Urgent", Text = "Urgent" }
            }, "Value", "Text", order.Priority);

            ViewBag.ShippingCarrierList = new SelectList(new[]
            {
                "The Courier Guy",
                "Fastway",
                "DHL",
                "FedEx",
                "UPS",
                "PostNet",
                "Pargo",
                "Other"
            }, order.ShippingCarrier);

            return View(order);
        }

        // POST: /OrderManager/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Checkout order, string actionType = "save")
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            var existingOrder = await _context.Checkouts.FindAsync(id);
            if (existingOrder == null)
            {
                return NotFound();
            }

            // Store old values for history
            var oldStatus = existingOrder.OrderStatus;
            var oldPaymentStatus = existingOrder.PaymentStatus;

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only the fields that should be updated
                    existingOrder.OrderStatus = order.OrderStatus;
                    existingOrder.PaymentStatus = order.PaymentStatus;
                    existingOrder.Priority = order.Priority;
                    existingOrder.AssignedTo = order.AssignedTo;
                    existingOrder.AdminNotes = order.AdminNotes;
                    existingOrder.TrackingNumber = order.TrackingNumber;
                    existingOrder.ShippingCarrier = order.ShippingCarrier;
                    existingOrder.ShippingService = order.ShippingService;
                    existingOrder.ShippingCost = order.ShippingCost;
                    existingOrder.Discount = order.Discount;
                    existingOrder.EstimatedDelivery = order.EstimatedDelivery;
                    existingOrder.ActualDelivery = order.ActualDelivery;
                    existingOrder.InternalReference = order.InternalReference;

                    // Handle specific actions
                    switch (actionType)
                    {
                        case "mark-paid":
                            existingOrder.PaymentStatus = "Paid";
                            TempData["SuccessMessage"] = $"Order #{id} marked as Paid";
                            break;
                        case "mark-shipped":
                            existingOrder.OrderStatus = "Shipped";
                            if (string.IsNullOrEmpty(existingOrder.TrackingNumber))
                            {
                                existingOrder.TrackingNumber = GenerateTrackingNumber();
                            }
                            TempData["SuccessMessage"] = $"Order #{id} marked as Shipped with tracking: {existingOrder.TrackingNumber}";
                            break;
                        case "mark-delivered":
                            existingOrder.OrderStatus = "Delivered";
                            existingOrder.ActualDelivery = DateTime.Now;
                            TempData["SuccessMessage"] = $"Order #{id} marked as Delivered";
                            break;
                        case "send-tracking":
                            // In a real app, you would send an email here
                            TempData["SuccessMessage"] = $"Tracking email sent for order #{id}";
                            break;
                        case "generate-invoice":
                            // In a real app, you would generate an invoice PDF
                            TempData["SuccessMessage"] = $"Invoice generated for order #{id}";
                            break;
                        default: // save
                            TempData["SuccessMessage"] = $"Order #{id} updated successfully";
                            break;
                    }

                    // Add to order history if status changed
                    if (oldStatus != existingOrder.OrderStatus || oldPaymentStatus != existingOrder.PaymentStatus)
                    {
                        AddOrderHistory(id, oldStatus, existingOrder.OrderStatus,
                                       oldPaymentStatus, existingOrder.PaymentStatus,
                                       User.Identity?.Name);
                    }

                    _context.Update(existingOrder);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // If we got here, something went wrong - reload dropdowns
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "Pending", Text = "Pending" },
                new { Value = "Processing", Text = "Processing" },
                new { Value = "Shipped", Text = "Shipped" },
                new { Value = "Delivered", Text = "Delivered" },
                new { Value = "Cancelled", Text = "Cancelled" },
                new { Value = "Refunded", Text = "Refunded" }
            }, "Value", "Text", order.OrderStatus);

            return View(order);
        }

        // GET: /OrderManager/Delete/{id}
        public async Task<IActionResult> Delete(int id)
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

        // POST: /OrderManager/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Checkouts.FindAsync(id);
            if (order != null)
            {
                _context.Checkouts.Remove(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order #{id} deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /OrderManager/Print/{id}
        public async Task<IActionResult> Print(int id)
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

        // GET: /OrderManager/Export
        public async Task<IActionResult> Export(string format = "csv", DateTime? startDate = null, DateTime? endDate = null)
        {
            var orders = _context.Checkouts.Include(o => o.OrderItems).AsQueryable();

            // Apply date filters
            if (startDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= endDate.Value);
            }

            var orderList = await orders.ToListAsync();

            if (format == "excel")
            {
                // In a real app, you would use EPPlus or similar library
                // For now, return CSV
                return ExportToCsv(orderList);
            }

            return ExportToCsv(orderList);
        }

        // GET: /OrderManager/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Get statistics for dashboard
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Monthly statistics
            var monthlyOrders = await _context.Checkouts
                .Where(o => o.OrderDate >= startOfMonth)
                .ToListAsync();

            ViewBag.MonthlyOrders = monthlyOrders.Count;
            ViewBag.MonthlyRevenue = monthlyOrders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.Total);
            ViewBag.MonthlyAverage = monthlyOrders.Any() ? monthlyOrders.Average(o => o.Total) : 0;

            // Yearly statistics
            var yearlyOrders = await _context.Checkouts
                .Where(o => o.OrderDate >= startOfYear)
                .ToListAsync();

            ViewBag.YearlyOrders = yearlyOrders.Count;
            ViewBag.YearlyRevenue = yearlyOrders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.Total);

            // Top products
            var topProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductName)
                .Select(g => new { ProductName = g.Key, TotalSold = g.Sum(oi => oi.Quantity), Revenue = g.Sum(oi => oi.Total) })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            ViewBag.TopProducts = topProducts;

            // Recent orders
            var recentOrders = await _context.Checkouts
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentOrders = recentOrders;

            // Order status distribution
            var statusDistribution = await _context.Checkouts
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.StatusDistribution = statusDistribution;

            // Monthly revenue chart data
            var monthlyRevenueData = await _context.Checkouts
                .Where(o => o.OrderDate >= startOfYear && o.PaymentStatus == "Paid")
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Revenue = g.Sum(o => o.Total)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            ViewBag.MonthlyRevenueData = monthlyRevenueData;

            return View();
        }

        // POST: /OrderManager/BulkAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(int[] orderIds, string action)
        {
            if (orderIds == null || !orderIds.Any())
            {
                TempData["ErrorMessage"] = "No orders selected";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var orders = await _context.Checkouts
                    .Where(o => orderIds.Contains(o.OrderId))
                    .ToListAsync();

                foreach (var order in orders)
                {
                    switch (action)
                    {
                        case "mark-processing":
                            order.OrderStatus = "Processing";
                            break;
                        case "mark-shipped":
                            order.OrderStatus = "Shipped";
                            if (string.IsNullOrEmpty(order.TrackingNumber))
                            {
                                order.TrackingNumber = GenerateTrackingNumber();
                            }
                            break;
                        case "mark-delivered":
                            order.OrderStatus = "Delivered";
                            order.ActualDelivery = DateTime.Now;
                            break;
                        case "mark-paid":
                            order.PaymentStatus = "Paid";
                            break;
                        case "assign-me":
                            order.AssignedTo = User.Identity?.Name;
                            break;
                        case "export-selected":
                            // Will be handled separately
                            break;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{orders.Count} orders updated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action");
                TempData["ErrorMessage"] = "Error performing bulk action";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Get order statistics
        [HttpGet]
        public async Task<IActionResult> GetStatistics(string period = "today")
        {
            DateTime startDate;
            DateTime endDate = DateTime.Now;

            switch (period)
            {
                case "today":
                    startDate = DateTime.Today;
                    break;
                case "yesterday":
                    startDate = DateTime.Today.AddDays(-1);
                    endDate = DateTime.Today;
                    break;
                case "this-week":
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case "this-month":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
                case "this-year":
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    break;
                default:
                    startDate = DateTime.Today.AddDays(-7);
                    break;
            }

            var orders = await _context.Checkouts
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var stats = new
            {
                totalOrders = orders.Count,
                totalRevenue = orders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.Total),
                pendingOrders = orders.Count(o => o.OrderStatus == "Pending"),
                averageOrderValue = orders.Any() ? orders.Average(o => o.Total) : 0
            };

            return Json(stats);
        }

        // Helper methods
        private bool OrderExists(int id)
        {
            return _context.Checkouts.Any(e => e.OrderId == id);
        }

        private string GenerateTrackingNumber()
        {
            return $"TRK{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private List<OrderHistory> GetOrderHistory(int orderId)
        {
            // In a real app, you would query an OrderHistory table
            // For now, return a mock list
            return new List<OrderHistory>
            {
                new OrderHistory
                {
                    Id = 1,
                    OrderId = orderId,
                    Action = "Order Created",
                    Description = "Order was placed by customer",
                    PerformedBy = "System",
                    PerformedAt = DateTime.Now.AddHours(-2),
                    OldStatus = null,
                    NewStatus = "Pending"
                }
            };
        }

        private void AddOrderHistory(int orderId, string oldStatus, string newStatus,
                                   string oldPaymentStatus, string newPaymentStatus,
                                   string performedBy)
        {
            // In a real app, you would save this to an OrderHistory table
            _logger.LogInformation($"Order {orderId} status changed from {oldStatus} to {newStatus} by {performedBy}");
        }

        private FileResult ExportToCsv(List<Checkout> orders)
        {
            var csv = new List<string>
            {
                "OrderID,OrderDate,Customer,Email,Phone,Total,Status,PaymentStatus,TrackingNumber"
            };

            foreach (var order in orders)
            {
                csv.Add($"\"{order.OrderId}\",\"{order.OrderDate:yyyy-MM-dd HH:mm}\",\"{order.FirstName} {order.LastName}\",\"{order.Email}\",\"{order.Phone}\",\"{order.Total}\",\"{order.OrderStatus}\",\"{order.PaymentStatus}\",\"{order.TrackingNumber}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", csv));
            return File(bytes, "text/csv", $"orders_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }

    // Order History model (for tracking changes)
    public class OrderHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? OldPaymentStatus { get; set; }
        public string? NewPaymentStatus { get; set; }
        public string? Notes { get; set; }
    }
}