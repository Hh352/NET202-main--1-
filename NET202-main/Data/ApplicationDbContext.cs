using Microsoft.EntityFrameworkCore;
using ASM.Models; 

namespace ASM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

        public DbSet<Category> Categories { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Voucher>().ToTable("Voucher");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<Cart>().ToTable("Cart");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetail");
            modelBuilder.Entity<Review>().ToTable("Review");

            modelBuilder.Entity<Review>()
                .HasOne(d => d.User).WithMany(n => n.Reviews)
                .HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(d => d.User).WithMany(n => n.Orders)
                .HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Cart>()
                .HasOne(d => d.User).WithMany(n => n.Carts)
                .HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}