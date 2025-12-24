using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventiq.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Seat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventSeats",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatKey = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Section = table.Column<string>(type: "text", nullable: true),
                    Row = table.Column<string>(type: "text", nullable: true),
                    Number = table.Column<string>(type: "text", nullable: true),
                    CategoryKey = table.Column<string>(type: "text", nullable: true),
                    ExtraData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSeats_Charts_ChartId",
                        column: x => x.ChartId,
                        principalSchema: "identity",
                        principalTable: "Charts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventSeatStates",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventSeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSeatStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSeatStates_EventItem_EventItemId",
                        column: x => x.EventItemId,
                        principalSchema: "identity",
                        principalTable: "EventItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventSeatStates_EventSeats_EventSeatId",
                        column: x => x.EventSeatId,
                        principalSchema: "identity",
                        principalTable: "EventSeats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventSeatStates_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "identity",
                        principalTable: "Tickets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventSeats_ChartId_SeatKey",
                schema: "identity",
                table: "EventSeats",
                columns: new[] { "ChartId", "SeatKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSeatStates_EventItemId_EventSeatId",
                schema: "identity",
                table: "EventSeatStates",
                columns: new[] { "EventItemId", "EventSeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSeatStates_EventSeatId",
                schema: "identity",
                table: "EventSeatStates",
                column: "EventSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeatStates_TicketId",
                schema: "identity",
                table: "EventSeatStates",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventSeatStates",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "EventSeats",
                schema: "identity");
        }
    }
}
