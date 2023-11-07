using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Retetar.Interfaces;

namespace Retetar.Repository
{
    public class RecipeDbContext : IdentityDbContext<User>
    {
        public DbSet<Recipe> Recipe { get; set; }
        public DbSet<Ingredient> Ingredient { get; set; }
        public DbSet<Category> Category { get; set; }

        public RecipeDbContext(DbContextOptions<RecipeDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure primary keys
            modelBuilder.Entity<Recipe>().HasKey(r => r.Id);
            modelBuilder.Entity<Ingredient>().HasKey(i => i.Id);
            modelBuilder.Entity<Category>().HasKey(c => c.Id);

            // Configure foreign key relationships
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Recipes)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
