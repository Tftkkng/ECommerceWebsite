using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.Models.Entities
{
    public class ShoppingCartItem
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // 導航屬性
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}