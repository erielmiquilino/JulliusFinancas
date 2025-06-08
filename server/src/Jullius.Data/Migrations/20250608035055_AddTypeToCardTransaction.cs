using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeToCardTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CardTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "CardTransactions");
        }
    }
}
