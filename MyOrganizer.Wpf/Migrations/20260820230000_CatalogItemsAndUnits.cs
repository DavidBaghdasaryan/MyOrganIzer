using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820230000_CatalogItemsAndUnits")]
public partial class CatalogItemsAndUnits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var sql = ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
        CreateUnits(migrationBuilder, sql);
        CreateCatalogItems(migrationBuilder, sql);
        TransformSupplierOfferings(migrationBuilder, sql);
        TransformExpenses(migrationBuilder, sql);
        SeedUnits(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_ExpenseLines_CatalogItems_CatalogItemId", "ExpenseLines");
        migrationBuilder.DropForeignKey("FK_ExpenseLines_UnitsOfMeasure_UnitOfMeasureId", "ExpenseLines");
        migrationBuilder.DropForeignKey("FK_SupplierOfferings_CatalogItems_CatalogItemId", "SupplierOfferings");
        migrationBuilder.DropTable("CatalogItems");
        migrationBuilder.DropTable("UnitsOfMeasure");
    }

    private static void CreateUnits(MigrationBuilder migrationBuilder, bool sql)
    {
        var integer = sql ? "int" : "INTEGER";
        var str80 = sql ? "nvarchar(80)" : "TEXT";
        var boolean = sql ? "bit" : "INTEGER";
        var factor = sql ? "decimal(18,6)" : "TEXT";

        migrationBuilder.CreateTable(
            name: "UnitsOfMeasure",
            columns: table => new
            {
                Id = sql
                    ? table.Column<int>(type: integer, nullable: false).Annotation("SqlServer:Identity", "1, 1")
                    : table.Column<int>(type: integer, nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: str80, maxLength: 80, nullable: false),
                IsActive = table.Column<bool>(type: boolean, nullable: false, defaultValue: true),
                BaseUnitId = table.Column<int>(type: integer, nullable: true),
                ConversionFactor = table.Column<decimal>(type: factor, precision: 18, scale: 6, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UnitsOfMeasure", x => x.Id);
                table.ForeignKey(
                    name: "FK_UnitsOfMeasure_UnitsOfMeasure_BaseUnitId",
                    column: x => x.BaseUnitId,
                    principalTable: "UnitsOfMeasure",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_UnitsOfMeasure_Name", table: "UnitsOfMeasure", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_UnitsOfMeasure_BaseUnitId", table: "UnitsOfMeasure", column: "BaseUnitId");
    }

    private static void CreateCatalogItems(MigrationBuilder migrationBuilder, bool sql)
    {
        var integer = sql ? "int" : "INTEGER";
        var str200 = sql ? "nvarchar(200)" : "TEXT";
        var str2000 = sql ? "nvarchar(2000)" : "TEXT";
        var boolean = sql ? "bit" : "INTEGER";
        var dt = sql ? "datetime2" : "TEXT";

        migrationBuilder.CreateTable(
            name: "CatalogItems",
            columns: table => new
            {
                Id = sql
                    ? table.Column<int>(type: integer, nullable: false).Annotation("SqlServer:Identity", "1, 1")
                    : table.Column<int>(type: integer, nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: str200, maxLength: 200, nullable: false),
                Kind = table.Column<int>(type: integer, nullable: false),
                UnitOfMeasureId = table.Column<int>(type: integer, nullable: true),
                Notes = table.Column<string>(type: str2000, maxLength: 2000, nullable: false),
                IsActive = table.Column<bool>(type: boolean, nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: dt, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: dt, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CatalogItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_CatalogItems_UnitsOfMeasure_UnitOfMeasureId",
                    column: x => x.UnitOfMeasureId,
                    principalTable: "UnitsOfMeasure",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_CatalogItems_Name", table: "CatalogItems", column: "Name");
        migrationBuilder.CreateIndex(name: "IX_CatalogItems_UnitOfMeasureId", table: "CatalogItems", column: "UnitOfMeasureId");
    }

    private static void TransformSupplierOfferings(MigrationBuilder migrationBuilder, bool sql)
    {
        var integer = sql ? "int" : "INTEGER";
        var money = sql ? "decimal(18,2)" : "TEXT";

        migrationBuilder.AddColumn<int>(
            name: "CatalogItemId",
            table: "SupplierOfferings",
            type: integer,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "SupplierPrice",
            table: "SupplierOfferings",
            type: money,
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        var now = sql ? "SYSUTCDATETIME()" : "datetime('now')";
        migrationBuilder.Sql($"""
            INSERT INTO CatalogItems (Name, Kind, Notes, IsActive, CreatedAt, UpdatedAt)
            SELECT DISTINCT so.Name, so.Kind, '', 1, {now}, {now}
            FROM SupplierOfferings so
            WHERE NOT EXISTS (
                SELECT 1 FROM CatalogItems c
                WHERE c.Name = so.Name AND c.Kind = so.Kind);
            """);

        if (sql)
        {
            migrationBuilder.Sql("""
                UPDATE so
                SET CatalogItemId = c.Id,
                    SupplierPrice = so.DefaultUnitPrice
                FROM SupplierOfferings so
                INNER JOIN CatalogItems c ON c.Name = so.Name AND c.Kind = so.Kind;
                """);

            migrationBuilder.Sql("""
                UPDATE el
                SET OfferingId = kept.KeepId
                FROM ExpenseLines el
                INNER JOIN SupplierOfferings so ON so.Id = el.OfferingId
                INNER JOIN (
                    SELECT SupplierId, CatalogItemId, MIN(Id) AS KeepId
                    FROM SupplierOfferings
                    GROUP BY SupplierId, CatalogItemId
                ) kept ON kept.SupplierId = so.SupplierId AND kept.CatalogItemId = so.CatalogItemId;
                """);

            migrationBuilder.Sql("""
                DELETE FROM SupplierOfferings
                WHERE Id NOT IN (
                    SELECT KeepId FROM (
                        SELECT MIN(Id) AS KeepId
                        FROM SupplierOfferings
                        GROUP BY SupplierId, CatalogItemId
                    ) t);
                """);

            migrationBuilder.Sql("DELETE FROM SupplierOfferings WHERE CatalogItemId IS NULL;");
        }
        else
        {
            migrationBuilder.Sql("""
                UPDATE SupplierOfferings
                SET CatalogItemId = (
                        SELECT c.Id FROM CatalogItems c
                        WHERE c.Name = SupplierOfferings.Name AND c.Kind = SupplierOfferings.Kind),
                    SupplierPrice = DefaultUnitPrice;
                """);

            migrationBuilder.Sql("""
                UPDATE ExpenseLines
                SET OfferingId = (
                    SELECT MIN(so2.Id)
                    FROM SupplierOfferings so2
                    WHERE so2.SupplierId = (
                            SELECT so1.SupplierId FROM SupplierOfferings so1 WHERE so1.Id = ExpenseLines.OfferingId)
                      AND so2.CatalogItemId = (
                            SELECT so1.CatalogItemId FROM SupplierOfferings so1 WHERE so1.Id = ExpenseLines.OfferingId));
                """);

            migrationBuilder.Sql("""
                DELETE FROM SupplierOfferings
                WHERE Id NOT IN (
                    SELECT KeepId FROM (
                        SELECT MIN(Id) AS KeepId
                        FROM SupplierOfferings
                        GROUP BY SupplierId, CatalogItemId
                    ) t);
                """);

            migrationBuilder.Sql("DELETE FROM SupplierOfferings WHERE CatalogItemId IS NULL;");
        }

        migrationBuilder.AlterColumn<int>(
            name: "CatalogItemId",
            table: "SupplierOfferings",
            type: integer,
            nullable: false);

        migrationBuilder.DropIndex(name: "IX_SupplierOfferings_SupplierId", table: "SupplierOfferings");
        migrationBuilder.CreateIndex(
            name: "IX_SupplierOfferings_SupplierId_CatalogItemId",
            table: "SupplierOfferings",
            columns: ["SupplierId", "CatalogItemId"],
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_SupplierOfferings_CatalogItemId",
            table: "SupplierOfferings",
            column: "CatalogItemId");

        migrationBuilder.AddForeignKey(
            name: "FK_SupplierOfferings_CatalogItems_CatalogItemId",
            table: "SupplierOfferings",
            column: "CatalogItemId",
            principalTable: "CatalogItems",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropColumn(name: "Name", table: "SupplierOfferings");
        migrationBuilder.DropColumn(name: "Kind", table: "SupplierOfferings");
        migrationBuilder.DropColumn(name: "DefaultUnitPrice", table: "SupplierOfferings");
    }

    private static void TransformExpenses(MigrationBuilder migrationBuilder, bool sql)
    {
        var integer = sql ? "int" : "INTEGER";

        migrationBuilder.DropForeignKey(name: "FK_ExpenseLines_SupplierOfferings_OfferingId", table: "ExpenseLines");
        migrationBuilder.DropIndex(name: "IX_ExpenseLines_OfferingId", table: "ExpenseLines");

        migrationBuilder.AddColumn<int>(
            name: "CatalogItemId",
            table: "ExpenseLines",
            type: integer,
            nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "UnitOfMeasureId",
            table: "ExpenseLines",
            type: integer,
            nullable: true);

        if (sql)
        {
            migrationBuilder.Sql("""
                UPDATE el
                SET CatalogItemId = so.CatalogItemId
                FROM ExpenseLines el
                INNER JOIN SupplierOfferings so ON so.Id = el.OfferingId;
                """);
        }
        else
        {
            migrationBuilder.Sql("""
                UPDATE ExpenseLines
                SET CatalogItemId = (
                    SELECT so.CatalogItemId FROM SupplierOfferings so
                    WHERE so.Id = ExpenseLines.OfferingId);
                """);
        }

        migrationBuilder.DropColumn(name: "OfferingId", table: "ExpenseLines");
        migrationBuilder.CreateIndex(name: "IX_ExpenseLines_CatalogItemId", table: "ExpenseLines", column: "CatalogItemId");
        migrationBuilder.CreateIndex(name: "IX_ExpenseLines_UnitOfMeasureId", table: "ExpenseLines", column: "UnitOfMeasureId");
        migrationBuilder.AddForeignKey(
            name: "FK_ExpenseLines_CatalogItems_CatalogItemId",
            table: "ExpenseLines",
            column: "CatalogItemId",
            principalTable: "CatalogItems",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(
            name: "FK_ExpenseLines_UnitsOfMeasure_UnitOfMeasureId",
            table: "ExpenseLines",
            column: "UnitOfMeasureId",
            principalTable: "UnitsOfMeasure",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.DropForeignKey(name: "FK_Expenses_Suppliers_SupplierId", table: "Expenses");
        migrationBuilder.AlterColumn<int>(
            name: "SupplierId",
            table: "Expenses",
            type: integer,
            nullable: true);
        migrationBuilder.AddForeignKey(
            name: "FK_Expenses_Suppliers_SupplierId",
            table: "Expenses",
            column: "SupplierId",
            principalTable: "Suppliers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    private static void SeedUnits(MigrationBuilder migrationBuilder)
    {
        foreach (var name in new[] { "Piece", "Gram", "Kilogram", "Milliliter", "Liter", "Pack", "Box", "Meter", "Hour" })
        {
            migrationBuilder.Sql($"""
                INSERT INTO UnitsOfMeasure (Name, IsActive)
                SELECT '{name}', 1
                WHERE NOT EXISTS (SELECT 1 FROM UnitsOfMeasure WHERE Name = '{name}');
                """);
        }
    }
}
