using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardIdToFinancialTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CardId",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            // Atualiza faturas existentes com o CardId correto baseado na descrição
            migrationBuilder.Sql(@"
                UPDATE ""FinancialTransactions"" 
                SET ""CardId"" = c.""Id""
                FROM ""Cards"" c
                WHERE ""FinancialTransactions"".""Description"" = 'Fatura ' || c.""Name""
                  AND ""FinancialTransactions"".""CardId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardId",
                table: "FinancialTransactions");
        }
    }
}
