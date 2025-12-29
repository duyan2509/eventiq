using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEventKeyInEventItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                schema: "identity",
                table: "EventItem",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventKey",
                schema: "identity",
                table: "EventItem");
        }
    }
}
