using Authentication_and_authorization_demo.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authentication_and_authorization_demo.AppDbContext
{
    public class FoodDbContext:IdentityDbContext
    {
        public FoodDbContext(DbContextOptions<FoodDbContext> options) : base(options)
        {

        }

        public DbSet<Food> Foods { get; set; }
        public new DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> refreshTokens { get; set; }
    }
}
