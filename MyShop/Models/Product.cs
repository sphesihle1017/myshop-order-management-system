namespace MyShop.Models
{
    using System;

    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Product Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required]
        public int Price { get; set; }

        [RegularExpression("([1-9][0-9]*)", ErrorMessage = "Please Enter Only Numeric Terms")]

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        // Foreign key reference to Category
        [Required]
        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        //To be used in the soft delete
        public bool IsActive { get; set; } = true; // Default to true

        // Navigation property
        public virtual Category Category { get; set; }
    }
}
