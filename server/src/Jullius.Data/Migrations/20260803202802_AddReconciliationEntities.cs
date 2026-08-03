using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jullius.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Institution = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    HolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PluggyItemId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PluggyAccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OpeningBalanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HasOpeningBalance = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OpeningBalanceTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastKnownBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LastBalanceSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RawDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RawAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RawDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawCategory = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CounterpartyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CounterpartyDocument = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ProposedDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProposedCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ReviewFlag = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MatchedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationItems_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationItems_ReconciliationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ReconciliationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_PluggyAccountId",
                table: "BankAccounts",
                column: "PluggyAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_BankAccountId",
                table: "ReconciliationItems",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_ExternalId",
                table: "ReconciliationItems",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_SessionId",
                table: "ReconciliationItems",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_Status",
                table: "ReconciliationItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_Status",
                table: "ReconciliationSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationItems");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "ReconciliationSessions");
        }
    }
}
