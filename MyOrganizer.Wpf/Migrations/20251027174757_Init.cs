using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyOrganizer.Wpf.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                // ----- SQL SERVER -----
                migrationBuilder.CreateTable(
                    name: "Clients",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                        LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                        MidlName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                        Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                        Debet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                        PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                        DateJoin = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                        DateDobleJoin = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Clients", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "L10nKeys",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        Group = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                        Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_L10nKeys", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Languages",
                    columns: table => new
                    {
                        Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                        Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Languages", x => x.Code);
                    });

                migrationBuilder.CreateTable(
                    name: "Procedures",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Procedures", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Products",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        Count = table.Column<int>(type: "int", nullable: false),
                        Value = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Products", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Technics",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Type = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                        Price = table.Column<int>(type: "int", nullable: false),
                        Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                        Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Technics", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "ToothWorks",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        ClientId = table.Column<int>(type: "int", nullable: false),
                        ToothFdi = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                        ProcedureName = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                        Tier = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                        Price = table.Column<int>(type: "int", nullable: false),
                        CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ToothWorks", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Teeth",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        ClientId = table.Column<int>(type: "int", nullable: false),
                        ToothNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                        Byugel = table.Column<int>(type: "int", nullable: false),
                        Protez = table.Column<int>(type: "int", nullable: false),
                        Implantzr = table.Column<int>(type: "int", nullable: false),
                        Implmk = table.Column<int>(type: "int", nullable: false),
                        Zrkemax = table.Column<int>(type: "int", nullable: false),
                        Mk30 = table.Column<int>(type: "int", nullable: false),
                        Rest = table.Column<int>(type: "int", nullable: false),
                        Plomb = table.Column<int>(type: "int", nullable: false),
                        Shift = table.Column<int>(type: "int", nullable: false),
                        Endo = table.Column<int>(type: "int", nullable: false),
                        Up = table.Column<bool>(type: "bit", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Teeth", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Teeth_Clients_ClientId",
                            column: x => x.ClientId,
                            principalTable: "Clients",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    name: "L10nValues",
                    columns: table => new
                    {
                        KeyId = table.Column<int>(type: "int", nullable: false),
                        Lang = table.Column<string>(type: "nvarchar(450)", nullable: false),
                        Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_L10nValues", x => new { x.KeyId, x.Lang });
                        table.ForeignKey(
                            name: "FK_L10nValues_L10nKeys_KeyId",
                            column: x => x.KeyId,
                            principalTable: "L10nKeys",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    name: "ProcedurePrices",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        ProcedureId = table.Column<int>(type: "int", nullable: false),
                        Tier1 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                        Tier2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                        Tier3 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                        Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ProcedurePrices", x => x.Id);
                        table.ForeignKey(
                            name: "FK_ProcedurePrices_Procedures_ProcedureId",
                            column: x => x.ProcedureId,
                            principalTable: "Procedures",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });
            }
            else if (ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // ----- SQLITE -----
                migrationBuilder.CreateTable(
                    name: "Clients",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                        LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                        MidlName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                        // decimal -> TEXT in SQLite (EF handles conversion)
                        Price = table.Column<decimal>(type: "TEXT", nullable: true),
                        Debet = table.Column<decimal>(type: "TEXT", nullable: true),
                        PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                        // DateTime as TEXT default
                        DateJoin = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                        DateDobleJoin = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Clients", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "L10nKeys",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                        Group = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                        Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_L10nKeys", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Languages",
                    columns: table => new
                    {
                        Code = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                        Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Languages", x => x.Code);
                    });

                migrationBuilder.CreateTable(
                    name: "Procedures",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                        // bool -> INTEGER in SQLite
                        IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Procedures", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Products",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                        Count = table.Column<int>(type: "INTEGER", nullable: false),
                        Value = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Products", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Technics",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        Type = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                        Price = table.Column<int>(type: "INTEGER", nullable: false),
                        Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                        Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Technics", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "ToothWorks",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                        ToothFdi = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                        ProcedureName = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                        Tier = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                        Price = table.Column<int>(type: "INTEGER", nullable: false),
                        CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ToothWorks", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Teeth",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                        ToothNumber = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                        Byugel = table.Column<int>(type: "INTEGER", nullable: false),
                        Protez = table.Column<int>(type: "INTEGER", nullable: false),
                        Implantzr = table.Column<int>(type: "INTEGER", nullable: false),
                        Implmk = table.Column<int>(type: "INTEGER", nullable: false),
                        Zrkemax = table.Column<int>(type: "INTEGER", nullable: false),
                        Mk30 = table.Column<int>(type: "INTEGER", nullable: false),
                        Rest = table.Column<int>(type: "INTEGER", nullable: false),
                        Plomb = table.Column<int>(type: "INTEGER", nullable: false),
                        Shift = table.Column<int>(type: "INTEGER", nullable: false),
                        Endo = table.Column<int>(type: "INTEGER", nullable: false),
                        Up = table.Column<bool>(type: "INTEGER", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Teeth", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Teeth_Clients_ClientId",
                            column: x => x.ClientId,
                            principalTable: "Clients",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    name: "L10nValues",
                    columns: table => new
                    {
                        KeyId = table.Column<int>(type: "INTEGER", nullable: false),
                        Lang = table.Column<string>(type: "TEXT", nullable: false),
                        Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_L10nValues", x => new { x.KeyId, x.Lang });
                        table.ForeignKey(
                            name: "FK_L10nValues_L10nKeys_KeyId",
                            column: x => x.KeyId,
                            principalTable: "L10nKeys",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    name: "ProcedurePrices",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        ProcedureId = table.Column<int>(type: "INTEGER", nullable: false),
                        // decimal -> TEXT
                        Tier1 = table.Column<decimal>(type: "TEXT", nullable: false),
                        Tier2 = table.Column<decimal>(type: "TEXT", nullable: false),
                        Tier3 = table.Column<decimal>(type: "TEXT", nullable: false),
                        Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ProcedurePrices", x => x.Id);
                        table.ForeignKey(
                            name: "FK_ProcedurePrices_Procedures_ProcedureId",
                            column: x => x.ProcedureId,
                            principalTable: "Procedures",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });
            }

            // ---- COMMON (runs on both) ----
            migrationBuilder.InsertData(
                table: "Procedures",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Removable Partial Denture (Metal Framework)" },
                    { 2, true, "Full Denture" },
                    { 3, true, "Implant with Zirconia Crown" },
                    { 4, true, "Implant with Metal-Ceramic Crown" },
                    { 5, true, "Zirconia or E-max Crown" },
                    { 6, true, "Metal-Ceramic Crown" },
                    { 7, true, "Composite or Inlay Restoration" },
                    { 8, true, "Filling (Composite / Amalgam)" },
                    { 9, true, "Work Shift / Appointment Slot" },
                    { 10, true, "Endodontic Treatment (Root Canal)" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_L10nKeys_Key",
                table: "L10nKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcedurePrices_ProcedureId",
                table: "ProcedurePrices",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_Teeth_Client_ToothNumber",
                table: "Teeth",
                columns: new[] { "ClientId", "ToothNumber" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "L10nValues");
            migrationBuilder.DropTable(name: "Languages");
            migrationBuilder.DropTable(name: "ProcedurePrices");
            migrationBuilder.DropTable(name: "Products");
            migrationBuilder.DropTable(name: "Technics");
            migrationBuilder.DropTable(name: "Teeth");
            migrationBuilder.DropTable(name: "ToothWorks");
            migrationBuilder.DropTable(name: "L10nKeys");
            migrationBuilder.DropTable(name: "Procedures");
            migrationBuilder.DropTable(name: "Clients");
        }
    }
}
