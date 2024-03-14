using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retetar.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO AspNetRoles (Id, [Name], NormalizedName) VALUES ('1', 'Manager', 'MANAGER')");
            migrationBuilder.Sql("INSERT INTO AspNetRoles (Id, [Name], NormalizedName) VALUES ('2', 'User', 'USER')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
