using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820220000_ExpensesAndSuppliers")]
public partial class ExpensesAndSuppliers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            UpSqlServer(migrationBuilder);
        else
            UpSqlite(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ExpenseLines");
        migrationBuilder.DropTable(name: "Expenses");
        migrationBuilder.DropTable(name: "SupplierOfferings");
        migrationBuilder.DropTable(name: "Suppliers");
    }

    private static void UpSqlServer(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Suppliers", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_Suppliers_Name", table: "Suppliers", column: "Name");

        migrationBuilder.CreateTable(
            name: "SupplierOfferings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SupplierId = table.Column<int>(type: "int", nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                DefaultUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Sku = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierOfferings", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierOfferings_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierOfferings_SupplierId",
            table: "SupplierOfferings",
            column: "SupplierId");

        CreateExpenseTables(migrationBuilder, "int", "datetime2", "nvarchar(80)", "nvarchar(2000)", "nvarchar(200)", "nvarchar(20)", "decimal(18,2)", "decimal(18,3)", sqlServer: true);
    }

    private static void UpSqlite(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Suppliers", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_Suppliers_Name", table: "Suppliers", column: "Name");

        migrationBuilder.CreateTable(
            name: "SupplierOfferings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                Kind = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                DefaultUnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Sku = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierOfferings", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierOfferings_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierOfferings_SupplierId",
            table: "SupplierOfferings",
            column: "SupplierId");

        CreateExpenseTables(migrationBuilder, "INTEGER", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", sqlServer: false);
    }

    private static void CreateExpenseTables(
        MigrationBuilder migrationBuilder,
        string integer,
        string dateTime,
        string str80,
        string str2000,
        string str200,
        string str20,
        string money,
        string qty,
        bool sqlServer)
    {
        migrationBuilder.CreateTable(
            name: "Expenses",
            columns: table => new
            {
                Id = sqlServer
                    ? table.Column<int>(type: integer, nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                    : table.Column<int>(type: integer, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                SupplierId = table.Column<int>(type: integer, nullable: false),
                Date = table.Column<DateTime>(type: dateTime, nullable: false),
                Reference = table.Column<string>(type: str80, maxLength: 80, nullable: false),
                Notes = table.Column<string>(type: str2000, maxLength: 2000, nullable: false),
                TotalAmount = table.Column<decimal>(type: money, precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTime>(type: dateTime, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: dateTime, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Expenses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Expenses_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Expenses_SupplierId", table: "Expenses", column: "SupplierId");
        migrationBuilder.CreateIndex(name: "IX_Expenses_Date", table: "Expenses", column: "Date");

        migrationBuilder.CreateTable(
            name: "ExpenseLines",
            columns: table => new
            {
                Id = sqlServer
                    ? table.Column<int>(type: integer, nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                    : table.Column<int>(type: integer, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                ExpenseId = table.Column<int>(type: integer, nullable: false),
                OfferingId = table.Column<int>(type: integer, nullable: false),
                Kind = table.Column<int>(type: integer, nullable: false),
                Description = table.Column<string>(type: str200, maxLength: 200, nullable: false),
                Quantity = table.Column<decimal>(type: qty, precision: 18, scale: 3, nullable: false),
                UnitPrice = table.Column<decimal>(type: money, precision: 18, scale: 2, nullable: false),
                LineTotal = table.Column<decimal>(type: money, precision: 18, scale: 2, nullable: false),
                ClientId = table.Column<int>(type: integer, nullable: true),
                ToothFdi = table.Column<string>(type: str20, maxLength: 20, nullable: true),
                CatalogProcedureId = table.Column<int>(type: integer, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExpenseLines_Expenses_ExpenseId",
                    column: x => x.ExpenseId,
                    principalTable: "Expenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ExpenseLines_SupplierOfferings_OfferingId",
                    column: x => x.OfferingId,
                    principalTable: "SupplierOfferings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExpenseLines_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ExpenseLines_Procedures_CatalogProcedureId",
                    column: x => x.CatalogProcedureId,
                    principalTable: "Procedures",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_ExpenseLines_ExpenseId", table: "ExpenseLines", column: "ExpenseId");
        migrationBuilder.CreateIndex(name: "IX_ExpenseLines_OfferingId", table: "ExpenseLines", column: "OfferingId");
        migrationBuilder.CreateIndex(name: "IX_ExpenseLines_ClientId", table: "ExpenseLines", column: "ClientId");
        migrationBuilder.CreateIndex(
            name: "IX_ExpenseLines_CatalogProcedureId",
            table: "ExpenseLines",
            column: "CatalogProcedureId");
    }
}
