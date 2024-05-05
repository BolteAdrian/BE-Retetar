using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retetar.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO Settings ( NightMode, Currency, Language ) VALUES ( '0', 'RON', 'RO')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
