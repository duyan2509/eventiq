using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddColorTicketClassNEventKeyEventItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                schema: "identity",
                table: "TicketClasses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPerUser",
                schema: "identity",
                table: "EventItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.DropColumn(
                name: "MaxPerUser",
                schema: "identity",
                table: "EventItem");
        }
    }
}
