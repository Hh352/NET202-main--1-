using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("Review")]
    public class Review
    {
        [Key] public int ReviewId { get; set; }
        
        public int UserId { get; set; }
        [ForeignKey("UserId")] public virtual User? User { get; set; }
        
        public int ProductId { get; set; }
        [ForeignKey("ProductId")] public virtual Product? Product { get; set; }
        
        public int? OrderId { get; set; }
        [ForeignKey("OrderId")] public virtual Order? Order { get; set; }
        
        public int Rating { get; set; }
        [StringLength(1000)] public string? Comment { get; set; }
        [StringLength(255)] public string? Image { get; set; }
        [StringLength(1000)] public string? AdminReply { get; set; }
        public int Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}