using Microsoft.EntityFrameworkCore;
using MyShop.Models;
namespace MyShop.Data
{

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Category { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
