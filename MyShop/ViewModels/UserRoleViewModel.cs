using System.ComponentModel.DataAnnotations;

namespace MyShop.Models
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Display(Name = "Phone")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Lockout End")]
        public DateTimeOffset? LockoutEnd { get; set; }

        [Display(Name = "Failed Logins")]
        public int AccessFailedCount { get; set; }

        [Display(Name = "Is Locked")]
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow;

        [Display(Name = "Status")]
        public string Status => IsLockedOut ? "Locked" : EmailConfirmed ? "Active" : "Pending";
    }
}