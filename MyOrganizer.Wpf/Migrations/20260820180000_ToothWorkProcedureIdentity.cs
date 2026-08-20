using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820180000_ToothWorkProcedureIdentity")]
public partial class ToothWorkProcedureIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProcedureId",
                table: "ToothWorks",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "RootCanalIds",
                table: "ToothWorks",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");
        }
        else
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProcedureId",
                table: "ToothWorks",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "RootCanalIds",
                table: "ToothWorks",
                type: "TEXT",
                maxLength: 400,
                nullable: false,
                defaultValue: "");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ProcedureId", table: "ToothWorks");
        migrationBuilder.DropColumn(name: "RootCanalIds", table: "ToothWorks");
    }
}
