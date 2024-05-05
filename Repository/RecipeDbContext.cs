using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Retetar.Models;

namespace Retetar.Repository
{
    public class RecipeDbContext : IdentityDbContext<User>
    {
        public DbSet<Ingredient> Ingredient { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Recipe> Recipe { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<RecipeCategory> RecipeCategories { get; set; }
        public DbSet<RecipeIngredients> RecipeIngredients { get; set; }
        public DbSet<IngredientQuantities> IngredientQuantities { get; set; }
        public DbSet<PreparedRecipeHistory> PreparedRecipeHistory { get; set; }
        public DbSet<Settings> Settings { get; set; }

        public RecipeDbContext(DbContextOptions<RecipeDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuring the relationship for Ingredient - IngredientQuantity (one-to-many)
            modelBuilder.Entity<IngredientQuantities>()
                .HasOne(iq => iq.Ingredient)
                .WithMany()
                .HasForeignKey(iq => iq.IngredientId);

            // Configuring the relationship for Ingredient - Category (many-to-one)
            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId);

            // Configuring relationships for RecipeCategory (many-to-many)
            modelBuilder.Entity<RecipeCategory>()
                .HasOne(rc => rc.Recipe)
                .WithMany(r => r.RecipeCategories)
                .HasForeignKey(rc => rc.RecipeId);

            modelBuilder.Entity<RecipeCategory>()
                .HasOne(rc => rc.Category)
                .WithMany()
                .HasForeignKey(rc => rc.CategoryId);

            // Configuring relationships for RecipeIngredient (many-to-many)
            modelBuilder.Entity<RecipeIngredients>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId);

            modelBuilder.Entity<RecipeIngredients>()
                .HasOne(ri => ri.Ingredient)
                .WithMany()
                .HasForeignKey(ri => ri.IngredientId);

            // Configure relationship for PreparedRecipeHistory - Recipe (one-to-many)
            modelBuilder.Entity<PreparedRecipeHistory>()
                .HasOne(prh => prh.Recipe)
                .WithMany()
                .HasForeignKey(prh => prh.RecipeId);
        }
    }
}
