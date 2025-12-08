using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyShop.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters.")]
        public string Name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        // Add Description field
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        // Add Size field
        [StringLength(50, ErrorMessage = "Size cannot exceed 50 characters.")]
        [Display(Name = "Size")]
        public string? Size { get; set; }

        // Stock quantity fields
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; } = 0;

        [Display(Name = "Low Stock Threshold")]
        [Range(1, int.MaxValue, ErrorMessage = "Threshold must be at least 1")]
        public int LowStockThreshold { get; set; } = 10; // Default threshold

        // Image property
        [StringLength(500)]
        public string? ImageFileName { get; set; }

        [NotMapped]
        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        public bool IsActive { get; set; } = true;

        // Foreign key
        public int? CategoryId { get; set; }

        // Navigation property
        public virtual Category? Category { get; set; }

        // Helper properties (not mapped to database)
        [NotMapped]
        public bool IsLowStock => StockQuantity <= LowStockThreshold;

        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (StockQuantity == 0)
                    return "Out of Stock";
                else if (IsLowStock)
                    return "Low Stock";
                else
                    return "In Stock";
            }
        }

        [NotMapped]
        public string StockStatusColor
        {
            get
            {
                if (StockQuantity == 0)
                    return "danger";
                else if (IsLowStock)
                    return "warning";
                else
                    return "success";
            }
        }
    }
}