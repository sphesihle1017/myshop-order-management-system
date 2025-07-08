using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyShop.Models;
using MyShop.Data;
using Microsoft.AspNetCore.Authorization;

namespace ONT3001EFExample.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
    
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }


        //GET:/Product
        //Retrieving all Products including their category
        public async Task<IActionResult> Index()
        {
            var products = await _context.Product
                                .Include(p => p.Category)
                                .Where(p => p.IsActive)
                                .ToListAsync();


            return View(products);

            //return View(await _context.Product.ToListAsync());
        }

        //This method searches for product names in our database
        public async Task<IActionResult> Search(string query)
        {
            var filteredProducts = await _context.Product
                                  .Include(p => p.Category)
                                  .Where(p => p.Name.Contains(query))
                                  .ToListAsync();

            return View("Index", filteredProducts);
        }

        //GET:Prod
        public IActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(_context.Category.ToList(), "CategoryId", "CategoryName");
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            try
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            catch (DbUpdateException)
            {
                //Log the error (uncomment ex variable name and write a log.)
                ModelState.AddModelError("", "Unable to save changes. " +
                    "Try again, and if the problem persists, " +
                    "see your system administrator.");

            }

            // Repopulate dropdown if model is invalid
            ViewBag.CategoryList = new SelectList(_context.Category.ToList(), "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Product.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.CategoryList = new SelectList(_context.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId)
                return NotFound();


            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Product.Any(p => p.ProductId == id))
                    return NotFound();
                else
                    throw;
            }


            ViewBag.CategoryList = new SelectList(_context.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }



        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Product
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
            var product = await _context.Product.FindAsync(id);
            if (product != null)
            {
                _context.Product.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;//this part performs the soft delete
                _context.Update(product);
                await _context.SaveChangesAsync();

            }
            return RedirectToAction(nameof(Index));
        }






        //GET: A specific product details 

        public async Task<IActionResult> Details(int ID)
        {
            if (ID == null || _context.Product == null)
            {
                return NotFound();
            }

            var product = await _context.Product.FirstOrDefaultAsync(p => p.ProductId == ID);

            return View(product);
        }

    }
}
