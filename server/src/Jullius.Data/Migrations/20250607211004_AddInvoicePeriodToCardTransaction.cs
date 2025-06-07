using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePeriodToCardTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceMonth",
                table: "CardTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceYear",
                table: "CardTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Script PostgreSQL para atualizar os registros existentes
            migrationBuilder.Sql(@"
                UPDATE ""CardTransactions"" 
                SET 
                    ""InvoiceYear"" = CASE 
                        WHEN EXTRACT(DAY FROM ""Date"") > c.""ClosingDay"" THEN 
                            EXTRACT(YEAR FROM (""Date"" + INTERVAL '1 month'))
                        ELSE 
                            EXTRACT(YEAR FROM ""Date"")
                    END,
                    ""InvoiceMonth"" = CASE 
                        WHEN EXTRACT(DAY FROM ""Date"") > c.""ClosingDay"" THEN 
                            EXTRACT(MONTH FROM (""Date"" + INTERVAL '1 month'))
                        ELSE 
                            EXTRACT(MONTH FROM ""Date"")
                    END
                FROM ""Cards"" c 
                WHERE ""CardTransactions"".""CardId"" = c.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceMonth",
                table: "CardTransactions");

            migrationBuilder.DropColumn(
                name: "InvoiceYear",
                table: "CardTransactions");
        }
    }
}
