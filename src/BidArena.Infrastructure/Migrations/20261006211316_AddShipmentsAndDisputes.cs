using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidArena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentsAndDisputes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "Shipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Shipments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Resolution",
                table: "Disputes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Disputes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AuctionId",
                table: "Shipments",
                column: "AuctionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DueDate",
                table: "Shipments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_AuctionId",
                table: "Disputes",
                column: "AuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_OpenedByUserId",
                table: "Disputes",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_Status",
                table: "Disputes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_AuctionId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_DueDate",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_AuctionId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_OpenedByUserId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_Status",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Shipments");

            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "Shipments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Resolution",
                table: "Disputes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Disputes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);
        }
    }
}
