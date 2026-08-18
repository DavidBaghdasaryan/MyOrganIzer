using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817800000_OdontogramSurfaceSelectorLocalization")]
public partial class OdontogramSurfaceSelectorLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        Seed(migrationBuilder);
        UpdateHint(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("SurfaceSelectorHint",
                "Click a region. Ctrl+click selects several surfaces.",
                "Սեղմեք մակերեսը։ Ctrl+սեղմում՝ մի քանիսը ընտրելու համար։",
                "Нажмите на область. Ctrl+клик выбирает несколько поверхностей."),
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

    private static void UpdateHint(MigrationBuilder migrationBuilder)
    {
        SetValue(migrationBuilder, "ToothChartHint", "en",
            "Click a tooth. Select surfaces on the diagram, then apply a procedure. Ctrl+click selects several surfaces.");
        SetValue(migrationBuilder, "ToothChartHint", "hy",
            "Սեղմեք ատամը։ Ընտրեք մակերեսները գծագրում, ապա կիրառեք միջամտությունը։ Ctrl+սեղմում՝ մի քանի մակերես։");
        SetValue(migrationBuilder, "ToothChartHint", "ru",
            "Нажмите на зуб. Выберите поверхности на схеме, затем назначьте процедуру. Ctrl+клик — несколько поверхностей.");
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

    private static void SetValue(MigrationBuilder migrationBuilder, string key, string lang, string value)
    {
        var escaped = value.Replace("'", "''");
        migrationBuilder.Sql($"""
            UPDATE L10nValues
            SET Value = '{escaped}'
            WHERE Lang = '{lang}'
              AND KeyId = (SELECT Id FROM L10nKeys WHERE [Key] = '{key}');
            """);
    }
}
