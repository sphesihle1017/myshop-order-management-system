using Microsoft.AspNetCore.Identity;

namespace MyShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FirstName { get; set; }

        [PersonalData]
        public string? LastName { get; set; }

        [PersonalData]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [PersonalData]
        public bool IsActive { get; set; } = true;
    }
}