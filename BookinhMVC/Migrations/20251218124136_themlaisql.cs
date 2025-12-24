using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookinhMVC.Migrations
{
    /// <inheritdoc />
    public partial class themlaisql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenderRole",
                table: "ChatMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderRole",
                table: "ChatMessages");
        }
    }
}
