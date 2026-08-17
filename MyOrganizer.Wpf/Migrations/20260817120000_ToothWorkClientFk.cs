using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817120000_ToothWorkClientFk")]
public partial class ToothWorkClientFk : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DELETE FROM ToothWorks WHERE ClientId NOT IN (SELECT Id FROM Clients);");

        migrationBuilder.CreateIndex(
            name: "IX_ToothWorks_ClientId",
            table: "ToothWorks",
            column: "ClientId");

        migrationBuilder.AddForeignKey(
            name: "FK_ToothWorks_Clients_ClientId",
            table: "ToothWorks",
            column: "ClientId",
            principalTable: "Clients",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ToothWorks_Clients_ClientId",
            table: "ToothWorks");

        migrationBuilder.DropIndex(
            name: "IX_ToothWorks_ClientId",
            table: "ToothWorks");
    }
}
