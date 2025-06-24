using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.Models.Entities
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        [MaxLength(200)]
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // 導航屬性
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}