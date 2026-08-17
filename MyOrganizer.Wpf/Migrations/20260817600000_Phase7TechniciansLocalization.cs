using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817600000_Phase7TechniciansLocalization")]
public partial class Phase7TechniciansLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("TechniciansSubtitle", "Record technician payments for lab work.", "Գրանցեք տեխնիկներին վճարումները լաբորատոր աշխատանքի համար։", "Учёт выплат техникам за лабораторные работы."),
            ("AddTechnician", "Add technician", "Ավելացնել տեխնիկ", "Добавить техника"),
            ("EditTechnician", "Edit technician", "Խմբագրել տեխնիկին", "Редактировать техника"),
            ("DeleteTechnician", "Delete technician?", "Ջնջե՞լ տեխնիկի գրառումը", "Удалить запись техника?"),
            ("DeleteTechnicianMessage", "\"{0}\" will be removed.", "«{0}»-ը կջնջվի։", "«{0}» будет удалена."),
            ("NoTechniciansYet", "No technicians yet", "Տեխնիկներ չկան", "Техников пока нет"),
            ("AddFirstTechnician", "Add a technician to get started.", "Ավելացրեք տեխնիկ՝ սկսելու համար։", "Добавьте техника, чтобы начать."),
            ("NoTechniciansFound", "No technicians found", "Տեխնիկներ չեն գտնվել", "Техники не найдены"),
            ("SearchTechnicians", "Search technicians...", "Որոնել տեխնիկներ...", "Поиск техников..."),
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
