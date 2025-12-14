using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyShop.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        // Foreign Keys
        public int CartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // Other fields
        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Size { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [NotMapped]
        public decimal Total => Price * Quantity;

        // Navigation Properties
        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
