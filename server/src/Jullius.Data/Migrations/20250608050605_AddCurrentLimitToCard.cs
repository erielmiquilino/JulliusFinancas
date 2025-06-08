using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentLimitToCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLimit",
                table: "Cards",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Atualiza todos os registros existentes para definir CurrentLimit = Limit inicialmente
            migrationBuilder.Sql(@"
                UPDATE ""Cards"" SET ""CurrentLimit"" = ""Limit"";
            ");

            // Script SQL para calcular o CurrentLimit baseado nas transações existentes
            migrationBuilder.Sql(@"
                UPDATE ""Cards"" 
                SET ""CurrentLimit"" = ""Limit"" + COALESCE(
                    (SELECT SUM(
                        CASE 
                            WHEN ""Type"" = 1 THEN ""Amount""  -- Income (receita) soma
                            WHEN ""Type"" = 0 THEN -""Amount"" -- Expense (despesa) subtrai
                            ELSE 0
                        END
                    )
                    FROM ""CardTransactions"" 
                    WHERE ""CardTransactions"".""CardId"" = ""Cards"".""Id""), 
                    0
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLimit",
                table: "Cards");
        }
    }
}
