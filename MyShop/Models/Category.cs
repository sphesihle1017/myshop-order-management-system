namespace MyShop.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Category Name cannot exceed 100 characters.")]
        public string CategoryName { get; set; }
       // public string Gender { get; set; }

        // Navigation property: One category can have many products
        public virtual ICollection<Product> Products { get; set; }
    }
}
