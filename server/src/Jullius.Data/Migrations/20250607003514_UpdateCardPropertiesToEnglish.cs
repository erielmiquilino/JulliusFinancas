using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCardPropertiesToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Cards",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Limite",
                table: "Cards",
                newName: "Limit");

            migrationBuilder.RenameColumn(
                name: "DiaFechamento",
                table: "Cards",
                newName: "ClosingDay");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Cards",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Cards",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "Limit",
                table: "Cards",
                newName: "Limite");

            migrationBuilder.RenameColumn(
                name: "ClosingDay",
                table: "Cards",
                newName: "DiaFechamento");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Cards",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
