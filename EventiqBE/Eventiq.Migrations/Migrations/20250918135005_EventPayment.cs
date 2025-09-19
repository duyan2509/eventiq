using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class EventPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentQR",
                schema: "identity",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                schema: "identity",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                schema: "identity",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BankCode",
                schema: "identity",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                schema: "identity",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                schema: "identity",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BankCode",
                schema: "identity",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "PaymentQR",
                schema: "identity",
                table: "Events",
                type: "text",
                nullable: true);
        }
    }
}
