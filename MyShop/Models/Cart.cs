using System.ComponentModel.DataAnnotations;

namespace MyShop.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        // Optional: link cart to logged-in user
        public string? UserId { get; set; }

        // Navigation property
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}

