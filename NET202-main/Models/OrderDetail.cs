using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("OrderDetail")]
    public class OrderDetail
    {
        [Key] public int OrderDetailId { get; set; }
        
        public int OrderId { get; set; }
        [ForeignKey("OrderId")] public virtual Order? Order { get; set; }
        
        public int ProductId { get; set; }
        [ForeignKey("ProductId")] public virtual Product? Product { get; set; }
        
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    }
}