using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ECommerceWebsite.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountedPrice { get; set; }

        public int StockQuantity { get; set; }

        [MaxLength(100)]
        public string? SKU { get; set; }
        [MaxLength(200)]
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        //外鍵
        public int CategoryId { get; set; }

        // 導航屬性
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();


    }
}