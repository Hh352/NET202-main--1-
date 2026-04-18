using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("Voucher")]
    public class Voucher
    {
        [Key] public int VoucherId { get; set; }
        [Required, StringLength(50)] public string Code { get; set; }
        [StringLength(200)] public string? Name { get; set; } // Tên voucher
        public int DiscountType { get; set; } // 1: %, 2: Amount
        [Column(TypeName = "decimal(18,2)")] public decimal DiscountValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal MinOrderValue { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0; // Số lượt đã dùng
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [StringLength(50)] public string Status { get; set; } = "Active";

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
    }
}