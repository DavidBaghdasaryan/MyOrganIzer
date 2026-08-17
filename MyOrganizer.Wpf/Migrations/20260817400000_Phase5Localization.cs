using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817400000_Phase5Localization")]
public partial class Phase5Localization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("Maximize", "Maximize", "Ծավալել", "Развернуть"),
            ("Restore", "Restore", "Վերականգնել", "Восстановить"),
            ("Apply", "Apply", "Կիրառել", "Применить"),
            ("SearchProcedures", "Search procedures...", "Որոնել միջամտություններ...", "Поиск процедур..."),
            ("ProcedureApplied", "Procedure applied", "Միջամտությունը կիրառված է", "Процедура назначена"),
            ("AddPatient", "Add patient", "Ավելացնել հաճախորդ", "Добавить пациента"),
            ("EditPatient", "Edit patient", "Խմբագրել հաճախորդին", "Редактировать пациента"),
            ("FieldRequired", "This field is required.", "Այս դաշտը պարտադիր է։", "Это поле обязательно."),
            ("Confirm", "Confirm", "Հաստատել", "Подтвердить"),
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
