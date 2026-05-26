using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using UserService.Models;

namespace UserService.Models
{
    public class UserDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("UserServiceDb");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed initial user data
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Admin User", Email = "admin@snappay.com", Phone = "9876543210", Role = "Admin", Status = "Active", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new User { Id = 2, Name = "Regular User", Email = "user@snappay.com", Phone = "9876543211", Role = "User", Status = "Active", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            );
        }
    }
}
