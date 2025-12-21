// Controllers/OrderManagerController.cs - Updated with soft delete
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
        public async Task<IActionResult> Index(string status = "all", string sort = "newest",
            string search = "", bool showDeleted = false)
        {
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSort = sort;
            ViewBag.SearchTerm = search;
            ViewBag.ShowDeleted = showDeleted;

            // Start with base query
            var orders = _context.Checkouts.AsQueryable();

            // Filter deleted orders - only show non-deleted by default
            if (!showDeleted)
            {
                orders = orders.Where(o => !o.IsDeleted);
            }

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
                    (o.TrackingNumber != null && o.TrackingNumber.Contains(search)) ||
                    (o.InternalReference != null && o.InternalReference.Contains(search)));
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

            // Get statistics - exclude deleted orders
            ViewBag.TotalOrders = await _context.Checkouts.CountAsync(o => !o.IsDeleted);
            ViewBag.PendingOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Pending" && !o.IsDeleted);
            ViewBag.ProcessingOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Processing" && !o.IsDeleted);
            ViewBag.ShippedOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Shipped" && !o.IsDeleted);
            ViewBag.DeliveredOrders = await _context.Checkouts.CountAsync(o => o.OrderStatus == "Delivered" && !o.IsDeleted);
            ViewBag.TodayOrders = await _context.Checkouts.CountAsync(o => o.OrderDate.Date == DateTime.Today && !o.IsDeleted);
            ViewBag.TotalRevenue = await _context.Checkouts.Where(o => o.PaymentStatus == "Paid" && !o.IsDeleted).SumAsync(o => o.Total);

            // New: Count deleted orders
            ViewBag.DeletedOrders = await _context.Checkouts.CountAsync(o => o.IsDeleted);

            return View(orderList);
        }

        // GET: /OrderManager/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || order.IsDeleted)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: /OrderManager/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || order.IsDeleted)
            {
                return NotFound();
            }

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

            var existingOrder = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (existingOrder == null || existingOrder.IsDeleted)
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
                        case "mark-processing":
                            existingOrder.OrderStatus = "Processing";
                            TempData["SuccessMessage"] = $"Order #{id} marked as Processing";
                            break;
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
                        case "set-high-priority":
                            existingOrder.Priority = "High";
                            TempData["SuccessMessage"] = $"Order #{id} set to High Priority";
                            break;
                        case "put-on-hold":
                            existingOrder.OrderStatus = "On Hold";
                            TempData["SuccessMessage"] = $"Order #{id} put on Hold";
                            break;
                        default: // save
                            TempData["SuccessMessage"] = $"Order #{id} updated successfully";
                            break;
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

        // GET: /OrderManager/Delete/{id} - Soft Delete
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || order.IsDeleted)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: /OrderManager/Delete/{id} - Soft Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Soft delete - mark as deleted instead of removing from database
            order.IsDeleted = true;
            order.DeletedAt = DateTime.Now;

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} has been moved to trash";
            return RedirectToAction(nameof(Index));
        }

        // GET: /OrderManager/Restore/{id}
        public async Task<IActionResult> Restore(int id)
        {
            var order = await _context.Checkouts.FindAsync(id);

            if (order == null || !order.IsDeleted)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: /OrderManager/Restore/{id}
        [HttpPost, ActionName("Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConfirmed(int id)
        {
            var order = await _context.Checkouts.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            // Restore the order
            order.IsDeleted = false;
            order.DeletedAt = null;

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} has been restored";
            return RedirectToAction(nameof(Index));
        }

        // GET: /OrderManager/PermanentDelete/{id}
        public async Task<IActionResult> PermanentDelete(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.IsDeleted);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: /OrderManager/PermanentDelete/{id}
        [HttpPost, ActionName("PermanentDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDeleteConfirmed(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.IsDeleted);

            if (order != null)
            {
                // Permanent delete - remove from database
                // Delete order items first (due to foreign key constraint)
                if (order.OrderItems != null && order.OrderItems.Any())
                {
                    _context.OrderItems.RemoveRange(order.OrderItems);
                }

                _context.Checkouts.Remove(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order #{id} permanently deleted";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /OrderManager/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                TempData["ErrorMessage"] = "No orders selected";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var orders = await _context.Checkouts
                    .Where(o => orderIds.Contains(o.OrderId) && !o.IsDeleted)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    order.IsDeleted = true;
                    order.DeletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{orders.Count} orders moved to trash";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk delete");
                TempData["ErrorMessage"] = "Error deleting orders";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /OrderManager/Print/{id}
        public async Task<IActionResult> Print(int id)
        {
            var order = await _context.Checkouts
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || order.IsDeleted)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: /OrderManager/Export
        public async Task<IActionResult> Export(string format = "csv", DateTime? startDate = null,
            DateTime? endDate = null, string orderIds = "", bool includeDeleted = false)
        {
            var orders = _context.Checkouts.AsQueryable();

            // Filter out deleted orders by default
            if (!includeDeleted)
            {
                orders = orders.Where(o => !o.IsDeleted);
            }

            // Apply date filters
            if (startDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= endDate.Value);
            }

            // Filter by selected order IDs if provided
            if (!string.IsNullOrEmpty(orderIds))
            {
                var ids = orderIds.Split(',').Select(int.Parse).ToList();
                orders = orders.Where(o => ids.Contains(o.OrderId));
            }

            var orderList = await orders.ToListAsync();

            return ExportToCsv(orderList);
        }

        // GET: /OrderManager/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Get statistics for dashboard - exclude deleted orders
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Monthly statistics (non-deleted only)
            var monthlyOrders = await _context.Checkouts
                .Where(o => o.OrderDate >= startOfMonth && !o.IsDeleted)
                .ToListAsync();

            ViewBag.MonthlyOrders = monthlyOrders.Count;
            ViewBag.MonthlyRevenue = monthlyOrders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.Total);
            ViewBag.MonthlyAverage = monthlyOrders.Any() ? monthlyOrders.Average(o => o.Total) : 0;

            // Yearly statistics (non-deleted only)
            var yearlyOrders = await _context.Checkouts
                .Where(o => o.OrderDate >= startOfYear && !o.IsDeleted)
                .ToListAsync();

            ViewBag.YearlyOrders = yearlyOrders.Count;
            ViewBag.YearlyRevenue = yearlyOrders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.Total);

            // Top products (from non-deleted orders)
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => !oi.Order.IsDeleted)
                .GroupBy(oi => oi.ProductName)
                .Select(g => new {
                    ProductName = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Total)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            ViewBag.TopProducts = topProducts;

            // Recent orders (non-deleted only)
            var recentOrders = await _context.Checkouts
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentOrders = recentOrders;

            // Order status distribution (non-deleted only)
            var statusDistribution = await _context.Checkouts
                .Where(o => !o.IsDeleted)
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key ?? "Unknown", Count = g.Count() })
                .ToListAsync();

            ViewBag.StatusDistribution = statusDistribution;

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
                    .Where(o => orderIds.Contains(o.OrderId) && !o.IsDeleted)
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
                            // Will be handled by Export action
                            break;
                        case "soft-delete":
                            order.IsDeleted = true;
                            order.DeletedAt = DateTime.Now;
                            break;
                    }
                }

                if (action != "export-selected")
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{orders.Count} orders updated successfully";
                }

                if (action == "export-selected")
                {
                    var ids = string.Join(",", orderIds);
                    return RedirectToAction("Export", new { orderIds = ids });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action");
                TempData["ErrorMessage"] = "Error performing bulk action";
            }

            return RedirectToAction(nameof(Index));
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

        private FileResult ExportToCsv(List<Checkout> orders)
        {
            var csv = new List<string>
            {
                "OrderID,OrderDate,Customer,Email,Phone,Total,Status,PaymentStatus,TrackingNumber,ShippingCarrier,IsDeleted"
            };

            foreach (var order in orders)
            {
                var isDeleted = order.IsDeleted ? "Deleted" : "Active";
                csv.Add($"\"{order.OrderId}\",\"{order.OrderDate:yyyy-MM-dd HH:mm}\",\"{order.FirstName} {order.LastName}\",\"{order.Email}\",\"{order.Phone}\",\"{order.Total}\",\"{order.OrderStatus}\",\"{order.PaymentStatus}\",\"{order.TrackingNumber}\",\"{order.ShippingCarrier}\",\"{isDeleted}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", csv));
            return File(bytes, "text/csv", $"orders_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        // Add these new actions to OrderManagerController.cs

        // GET: /OrderManager/Trash
        public async Task<IActionResult> Trash(string search = "", string sort = "newest")
        {
            ViewBag.CurrentSort = sort;
            ViewBag.SearchTerm = search;

            var orders = _context.Checkouts
                .Where(o => o.IsDeleted)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o =>
                    o.OrderId.ToString().Contains(search) ||
                    o.FirstName.Contains(search) ||
                    o.LastName.Contains(search) ||
                    o.Email.Contains(search) ||
                    (o.TrackingNumber != null && o.TrackingNumber.Contains(search)));
            }

            // Sort
            switch (sort)
            {
                case "oldest":
                    orders = orders.OrderBy(o => o.DeletedAt);
                    break;
                case "oldest-order":
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
                default: // newest
                    orders = orders.OrderByDescending(o => o.DeletedAt);
                    break;
            }

            var orderList = await orders.ToListAsync();
            ViewBag.DeletedCount = orderList.Count;

            return View(orderList);
        }

        // POST: /OrderManager/EmptyTrash
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            try
            {
                // Get all deleted orders with their items
                var deletedOrders = await _context.Checkouts
                    .Include(o => o.OrderItems)
                    .Where(o => o.IsDeleted)
                    .ToListAsync();

                int count = 0;
                foreach (var order in deletedOrders)
                {
                    // Delete order items first
                    if (order.OrderItems != null && order.OrderItems.Any())
                    {
                        _context.OrderItems.RemoveRange(order.OrderItems);
                    }

                    // Delete the order
                    _context.Checkouts.Remove(order);
                    count++;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Trash emptied. {count} orders permanently deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error emptying trash");
                TempData["ErrorMessage"] = "Error emptying trash";
            }

            return RedirectToAction(nameof(Trash));
        }

        // POST: /OrderManager/BulkRestore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkRestore(int[] orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                TempData["ErrorMessage"] = "No orders selected";
                return RedirectToAction(nameof(Trash));
            }

            try
            {
                var orders = await _context.Checkouts
                    .Where(o => orderIds.Contains(o.OrderId) && o.IsDeleted)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    order.IsDeleted = false;
                    order.DeletedAt = null;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{orders.Count} orders restored";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk restore");
                TempData["ErrorMessage"] = "Error restoring orders";
            }

            return RedirectToAction(nameof(Trash));
        }
     

        // POST: /OrderManager/BulkPermanentDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkPermanentDelete(int[] orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                TempData["ErrorMessage"] = "No orders selected";
                return RedirectToAction(nameof(Trash));
            }

            try
            {
                var orders = await _context.Checkouts
                    .Include(o => o.OrderItems)
                    .Where(o => orderIds.Contains(o.OrderId) && o.IsDeleted)
                    .ToListAsync();

                int count = 0;
                foreach (var order in orders)
                {
                    // Delete order items first
                    if (order.OrderItems != null && order.OrderItems.Any())
                    {
                        _context.OrderItems.RemoveRange(order.OrderItems);
                    }

                    // Delete the order
                    _context.Checkouts.Remove(order);
                    count++;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{count} orders permanently deleted";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk permanent delete");
                TempData["ErrorMessage"] = "Error deleting orders";
            }

            return RedirectToAction(nameof(Trash));
        }
    }

}