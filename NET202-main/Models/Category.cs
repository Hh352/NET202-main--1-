using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM.Models
{
    [Table("Category")]
    public class Category
    {
        [Key] public int CategoryId { get; set; }
        [Required, StringLength(100)] public string CategoryName { get; set; }
        [StringLength(500)] public string? Description { get; set; }
        [StringLength(255)] public string? Image { get; set; }
        [StringLength(50)] public string Status { get; set; } = "Active";
        
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}