using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.Data;
using MyShop.Models;
using System.Diagnostics;

namespace MyShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        //public HomeController(ILogger<HomeController> logger)
        //{
        //    _logger = logger;
        //}
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        //Retrieving all Products including their category
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                .Include(p => p.Category)
                                .Where(p => p.IsActive)
                                .ToListAsync();


            return View(products);

            //return View(await _context.Product.ToListAsync());
        }
        public async Task<IActionResult> Search(string query)
        {
            var filteredProducts = await _context.Products
                                  .Include(p => p.Category)
                                  .Where(p => p.Name.Contains(query))
                                  .ToListAsync();

            return View("Index", filteredProducts);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
