using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class EventAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventAddresses_EventAddressId",
                schema: "identity",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventAddressId",
                schema: "identity",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventAddressId",
                schema: "identity",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "Detail",
                schema: "identity",
                table: "EventAddresses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                schema: "identity",
                table: "EventAddresses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_EventAddresses_EventId",
                schema: "identity",
                table: "EventAddresses",
                column: "EventId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EventAddresses_Events_EventId",
                schema: "identity",
                table: "EventAddresses",
                column: "EventId",
                principalSchema: "identity",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventAddresses_Events_EventId",
                schema: "identity",
                table: "EventAddresses");

            migrationBuilder.DropIndex(
                name: "IX_EventAddresses_EventId",
                schema: "identity",
                table: "EventAddresses");

            migrationBuilder.DropColumn(
                name: "Detail",
                schema: "identity",
                table: "EventAddresses");

            migrationBuilder.DropColumn(
                name: "EventId",
                schema: "identity",
                table: "EventAddresses");

            migrationBuilder.AddColumn<Guid>(
                name: "EventAddressId",
                schema: "identity",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventAddressId",
                schema: "identity",
                table: "Events",
                column: "EventAddressId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventAddresses_EventAddressId",
                schema: "identity",
                table: "Events",
                column: "EventAddressId",
                principalSchema: "identity",
                principalTable: "EventAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
