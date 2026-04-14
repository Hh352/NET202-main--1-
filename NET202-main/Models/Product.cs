using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("Product")]
    public class Product
    {
        [Key] public int ProductId { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")] public virtual Category? Category { get; set; }
        
        [Required, StringLength(150)] public string ProductName { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
        [StringLength(50)] public string? Unit { get; set; } // kg, lít, cái...
        public int Stock { get; set; } = 0;
        [StringLength(255)] public string? Image { get; set; }
        [StringLength(1000)] public string? Description { get; set; }
        [StringLength(50)] public string Status { get; set; } = "Active";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}