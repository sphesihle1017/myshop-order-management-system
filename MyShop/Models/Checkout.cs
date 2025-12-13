// Models/Checkout.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyShop.Models
{
    public class Checkout
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "City")]
        public string City { get; set; }

        [Required]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Country { get; set; } = "South Africa";

        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Deleted At")]
        public DateTime? DeletedAt { get; set; }

        // Payment Information
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Credit Card";

        [Display(Name = "Card Number")]
        [CreditCard]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiry Month")]
        public string? ExpiryMonth { get; set; }

        [Display(Name = "Expiry Year")]
        public string? ExpiryYear { get; set; }

        [Display(Name = "CVV")]
        public string? CVV { get; set; }

        // Order Details
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Shipping { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public string? OrderStatus { get; set; } = "Pending";

        public string? OrderNotes { get; set; }

        // Navigation property for order items
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [Display(Name = "Tracking Number")]
        public string? TrackingNumber { get; set; }

        [Display(Name = "Estimated Delivery")]
        public DateTime? EstimatedDelivery { get; set; }

        [Display(Name = "Actual Delivery")]
        public DateTime? ActualDelivery { get; set; }

        [Display(Name = "Shipping Carrier")]
        public string? ShippingCarrier { get; set; }

        [Display(Name = "Shipping Service")]
        public string? ShippingService { get; set; }

        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [Display(Name = "Priority")]
        public string? Priority { get; set; } = "Normal";

        [Display(Name = "Payment Status")]
        public string? PaymentStatus { get; set; } = "Pending";

        [Display(Name = "Shipping Cost")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        [Display(Name = "Discount Applied")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Display(Name = "Internal Reference")]
        public string? InternalReference { get; set; }

        [Display(Name = "Customer IP Address")]
        public string? CustomerIP { get; set; }

        [Display(Name = "Browser/Device")]
        public string? UserAgent { get; set; }

        [Display(Name = "Marketing Source")]
        public string? MarketingSource { get; set; }

        [Display(Name = "Campaign")]
        public string? Campaign { get; set; }

        // NotMapped properties for form handling
        [NotMapped]
        public List<CartItemViewModel>? CartItems { get; set; }

        [NotMapped]
        [Display(Name = "Save this information for next time")]
        public bool SaveInfo { get; set; }

        [NotMapped]
        [Display(Name = "Agree to terms and conditions")]
        public bool AgreeToTerms { get; set; }
    }

    // ViewModel for cart items in checkout (keep this in Checkout.cs as it's a view model)
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }
}