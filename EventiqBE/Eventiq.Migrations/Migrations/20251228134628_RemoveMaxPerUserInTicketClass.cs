using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMaxPerUserInTicketClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPerUser",
                schema: "identity",
                table: "TicketClasses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPerUser",
                schema: "identity",
                table: "TicketClasses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
