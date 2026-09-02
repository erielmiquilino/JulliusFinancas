using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationItemLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LinkMarkAsPaid",
                table: "ReconciliationItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LinkUpdateAmount",
                table: "ReconciliationItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LinkUpdateDueDate",
                table: "ReconciliationItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedTransactionId",
                table: "ReconciliationItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedTransactionId",
                table: "ReconciliationItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_LinkedTransactionId",
                table: "ReconciliationItems",
                column: "LinkedTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReconciliationItems_LinkedTransactionId",
                table: "ReconciliationItems");

            migrationBuilder.DropColumn(
                name: "LinkMarkAsPaid",
                table: "ReconciliationItems");

            migrationBuilder.DropColumn(
                name: "LinkUpdateAmount",
                table: "ReconciliationItems");

            migrationBuilder.DropColumn(
                name: "LinkUpdateDueDate",
                table: "ReconciliationItems");

            migrationBuilder.DropColumn(
                name: "LinkedTransactionId",
                table: "ReconciliationItems");

            migrationBuilder.DropColumn(
                name: "SuggestedTransactionId",
                table: "ReconciliationItems");
        }
    }
}
