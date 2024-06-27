using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retetar.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO Category ([Name], [Description],[Picture],[IsRecipe]) VALUES ('Uncategorized Recipes', 'This category contains all the new recipes that do not yet have a designated category.', NULL, '1')");
            migrationBuilder.Sql("INSERT INTO Category ([Name], [Description],[Picture],[IsRecipe]) VALUES ('Uncategorized Ingredients', 'This category contains all the new ingredients that do not yet have a designated category.',NULL,'0')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
