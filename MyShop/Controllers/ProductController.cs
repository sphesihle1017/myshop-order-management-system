using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyShop.Models;
using MyShop.Data;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace ONT3001EFExample.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Product
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                .Include(p => p.Category)
                                .Where(p => p.IsActive)
                                .ToListAsync();
            return View(products);
        }

        // GET: Product/LowStock
        public async Task<IActionResult> LowStock()
        {
            var lowStockProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            ViewBag.LowStockCount = lowStockProducts.Count;
            return View(lowStockProducts);
        }

        // Search method
        public async Task<IActionResult> Search(string query)
        {
            var filteredProducts = await _context.Products
                                  .Include(p => p.Category)
                                  .Where(p => p.Name.Contains(query) && p.IsActive)
                                  .ToListAsync();
            return View("Index", filteredProducts);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            LoadCategories();
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            try
            {
                // Handle image upload
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    product.ImageFileName = await SaveImage(product.ImageFile);
                }

                // Set defaults
                product.IsActive = true;
                if (product.LowStockThreshold <= 0)
                    product.LowStockThreshold = 10;

                _context.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product '{product.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. " +
                    "Try again, and if the problem persists, " +
                    "see your system administrator.");
            }

            LoadCategories(product.CategoryId);
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            LoadCategories(product.CategoryId);
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId)
                return NotFound();

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Handle image upload
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageFileName))
                    {
                        DeleteImage(existingProduct.ImageFileName);
                    }
                    // Save new image
                    existingProduct.ImageFileName = await SaveImage(product.ImageFile);
                }

                // Update properties
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.LowStockThreshold = product.LowStockThreshold;
                existingProduct.IsActive = product.IsActive;

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                    return NotFound();
                else
                    throw;
            }

            LoadCategories(product.CategoryId);
            return View(product);
        }

        // GET: Product/AdjustStock/5
        public async Task<IActionResult> AdjustStock(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Product/AdjustStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, string actionType, int quantity)
        {
            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Quantity must be greater than 0";
                return RedirectToAction(nameof(AdjustStock), new { id });
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            try
            {
                string actionMessage = "";

                switch (actionType.ToLower())
                {
                    case "add":
                        product.StockQuantity += quantity;
                        actionMessage = $"Added {quantity} units to stock";
                        break;

                    case "remove":
                        if (quantity > product.StockQuantity)
                        {
                            TempData["ErrorMessage"] = $"Cannot remove {quantity} units. Only {product.StockQuantity} units in stock.";
                            return RedirectToAction(nameof(AdjustStock), new { id });
                        }
                        product.StockQuantity -= quantity;
                        actionMessage = $"Removed {quantity} units from stock";
                        break;

                    case "set":
                        product.StockQuantity = quantity;
                        actionMessage = $"Stock quantity set to {quantity} units";
                        break;

                    default:
                        TempData["ErrorMessage"] = "Invalid action type";
                        return RedirectToAction(nameof(AdjustStock), new { id });
                }

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{actionMessage}. New quantity: {product.StockQuantity} units";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                    return NotFound();
                else
                    throw;
            }
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Product/Delete/5 (Hard Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Delete associated image if exists
                if (!string.IsNullOrEmpty(product.ImageFileName))
                {
                    DeleteImage(product.ImageFileName);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product '{product.Name}' deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Soft Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product '{product.Name}' deactivated successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Product Details
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // RESTOCK action - Quick add to stock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(int id, int quantity)
        {
            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Quantity must be greater than 0";
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.StockQuantity += quantity;
            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{quantity} units added to '{product.Name}'. New stock: {product.StockQuantity}";
            return RedirectToAction(nameof(Index));
        }

        // Helper methods
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        private void LoadCategories(int? selectedId = null)
        {
            var categories = _context.Categories.ToList();

            if (categories.Any())
            {
                ViewBag.CategoryList = new SelectList(categories, "CategoryId", "CategoryName", selectedId);
            }
            else
            {
                ViewBag.CategoryList = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        private void DeleteImage(string imageFileName)
        {
            var imagePath = Path.Combine(_environment.WebRootPath, "images", "products", imageFileName);

            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }
    }
}