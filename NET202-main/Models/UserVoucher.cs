using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("UserVoucher")]
    public class UserVoucher
    {
        [Key]
        public int UserVoucherId { get; set; }
        
        public int UserId { get; set; }
        public int VoucherId { get; set; }
        
        public DateTime SavedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("VoucherId")]
        public virtual Voucher? Voucher { get; set; }
    }
}
