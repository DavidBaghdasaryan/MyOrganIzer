using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817500000_Phase6ProceduresLocalization")]
public partial class Phase6ProceduresLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("ProceduresSubtitle", "Manage dental procedures and pricing.", "Կառավարեք ատամնաբուժական միջամտությունները և գները։", "Управление стоматологическими процедурами и ценами."),
            ("AddProcedure", "Add procedure", "Ավելացնել միջամտություն", "Добавить процедуру"),
            ("EditProcedure", "Edit procedure", "Խմբագրել միջամտությունը", "Редактировать процедуру"),
            ("DeleteProcedure", "Delete procedure?", "Ջնջե՞լ միջամտությունը", "Удалить процедуру?"),
            ("DeleteProcedureMessage", "\"{0}\" will be removed.", "«{0}»-ը կջնջվի։", "«{0}» будет удалена."),
            ("NoProceduresYet", "No procedures yet", "Միջամտություններ չկան", "Процедур пока нет"),
            ("AddFirstProcedure", "Create the first procedure to begin.", "Ստեղծեք առաջին միջամտությունը՝ սկսելու համար։", "Создайте первую процедуру, чтобы начать."),
            ("NoProceduresFound", "No procedures found", "Միջամտություններ չեն գտնվել", "Процедуры не найдены"),
        };

        foreach (var (key, en, hy, ru) in keys)
        {
            migrationBuilder.Sql($"""
                INSERT INTO L10nKeys ([Key])
                SELECT '{key}'
                WHERE NOT EXISTS (SELECT 1 FROM L10nKeys WHERE [Key] = '{key}');
                """);

            SeedValue(migrationBuilder, key, "en", en.Replace("'", "''"));
            SeedValue(migrationBuilder, key, "hy", hy.Replace("'", "''"));
            SeedValue(migrationBuilder, key, "ru", ru.Replace("'", "''"));
        }
    }

    private static void SeedValue(MigrationBuilder migrationBuilder, string key, string lang, string value) =>
        migrationBuilder.Sql($"""
            INSERT INTO L10nValues (KeyId, Lang, Value)
            SELECT k.Id, '{lang}', '{value}'
            FROM L10nKeys k
            WHERE k.[Key] = '{key}'
              AND NOT EXISTS (
                  SELECT 1 FROM L10nValues v
                  WHERE v.KeyId = k.Id AND v.Lang = '{lang}');
            """);
}
