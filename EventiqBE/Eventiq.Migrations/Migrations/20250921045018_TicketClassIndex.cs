using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TicketClassIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPerUser",
                schema: "identity",
                table: "TicketClasses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleEnd",
                schema: "identity",
                table: "TicketClasses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleStart",
                schema: "identity",
                table: "TicketClasses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SoldQuantity",
                schema: "identity",
                table: "TicketClasses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuantity",
                schema: "identity",
                table: "TicketClasses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TicketClasses_Name",
                schema: "identity",
                table: "TicketClasses",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketClasses_Name",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.DropColumn(
                name: "MaxPerUser",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.DropColumn(
                name: "SaleEnd",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.DropColumn(
                name: "SaleStart",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.DropColumn(
                name: "SoldQuantity",
                schema: "identity",
                table: "TicketClasses");

            migrationBuilder.DropColumn(
                name: "TotalQuantity",
                schema: "identity",
                table: "TicketClasses");
        }
    }
}
