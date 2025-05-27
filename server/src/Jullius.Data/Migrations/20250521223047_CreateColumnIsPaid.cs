using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateColumnIsPaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "FinancialTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "FinancialTransactions");
        }
    }
}
