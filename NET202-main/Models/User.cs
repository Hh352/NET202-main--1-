using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("User")]
    public class User
    {
        [Key] public int UserId { get; set; }
        [Required, StringLength(100)] public string FullName { get; set; }
        [Required, EmailAddress, StringLength(100)] public string Email { get; set; }
        [StringLength(20)] public string? Phone { get; set; }
        [Required, StringLength(255)] public string Password { get; set; }
        [StringLength(255)] public string? Address { get; set; }
        [StringLength(255)] public string? Avatar { get; set; }
        public int Role { get; set; } = 0; 
        [StringLength(50)] public string Status { get; set; } = "Active"; 
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}