using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyOrganizer.Wpf.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MidlName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Debet = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateJoin = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    DateDobleJoin = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
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
                    Group = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "Tecnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tecnos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToothWorks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ToothFdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcedureName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_L10nKeys_Key",
                table: "L10nKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teeth_Client_ToothNumber",
                table: "Teeth",
                columns: new[] { "ClientId", "ToothNumber" });

            migrationBuilder.Sql(@"
        INSERT INTO Languages (Code, Name) VALUES 
        ('hy', N'Armenian'),
        ('ru', N'Russian'),
        ('en', N'English');");

            migrationBuilder.Sql(@"CREATE UNIQUE INDEX UX_L10nKeys_Key ON MyOrganizer.dbo.L10nKeys([Key])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "L10nValues");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Tecnos");

            migrationBuilder.DropTable(
                name: "Teeth");

            migrationBuilder.DropTable(
                name: "ToothWorks");

            migrationBuilder.DropTable(
                name: "L10nKeys");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
