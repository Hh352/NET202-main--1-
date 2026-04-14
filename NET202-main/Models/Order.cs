using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("Order")]
    public class Order
    {
        [Key] public int OrderId { get; set; }
        
        public int UserId { get; set; }
        [ForeignKey("UserId")] public virtual User? User { get; set; }
        
        public int? VoucherId { get; set; }
        [ForeignKey("VoucherId")] public virtual Voucher? Voucher { get; set; }

        [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal FinalAmount { get; set; }
        
        public int PaymentMethod { get; set; } // 1: Cash, 2: Transfer
        public int PaymentStatus { get; set; } // 0: Unpaid, 1: Paid
        public int OrderStatus { get; set; } // 1: Pending, 2: Preparing, 3: Shipping, 4: Completed, 5: Cancelled
        public int OrderType { get; set; } = 1; // 1: Online, 2: Offline (tại quầy)
        
        [StringLength(20)] public string? PhoneNumber { get; set; }
        [StringLength(500)] public string? ShippingAddress { get; set; }
        
        [StringLength(500)] public string? CancelReason { get; set; }
        [StringLength(500)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}