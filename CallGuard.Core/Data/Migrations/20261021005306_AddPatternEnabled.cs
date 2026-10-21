using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallGuard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatternEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Patterns",
                type: "INTEGER",
                nullable: false,
                defaultValue: true); // existing patterns stay switched on
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Patterns");
        }
    }
}
