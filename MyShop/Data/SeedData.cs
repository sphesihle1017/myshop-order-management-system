using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyShop.Models;

namespace MyShop.Data
{
    public static class SeedData
    {
        private const string DefaultAdminEmail = "admin@myshop.com";
        private const string DefaultAdminPassword = "Admin@123";
        private const string DefaultManagerEmail = "manager@myshop.com";
        private const string DefaultManagerPassword = "Manager@123";
        private const string DefaultCustomerEmail = "customer@myshop.com";
        private const string DefaultCustomerPassword = "Customer@123";

        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Create roles if they don't exist
            string[] roleNames = { "Admin", "Manager", "Staff", "Customer" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default Admin user if it doesn't exist
            await CreateUserIfNotExists(
                userManager,
                DefaultAdminEmail,
                DefaultAdminPassword,
                "Admin",
                "User",
                new[] { "Admin", "Manager" }
            );

            // Create default Manager user if it doesn't exist
            await CreateUserIfNotExists(
                userManager,
                DefaultManagerEmail,
                DefaultManagerPassword,
                "Shop",
                "Manager",
                new[] { "Manager" }
            );

            // Create default Customer user if it doesn't exist
            await CreateUserIfNotExists(
                userManager,
                DefaultCustomerEmail,
                DefaultCustomerPassword,
                "John",
                "Doe",
                new[] { "Customer" }
            );
        }

        private static async Task CreateUserIfNotExists(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string firstName,
            string lastName,
            string[] roles)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    foreach (var role in roles)
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                }
            }
            else
            {
                // Ensure user has all specified roles
                var existingRoles = await userManager.GetRolesAsync(user);
                foreach (var role in roles.Where(r => !existingRoles.Contains(r)))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}