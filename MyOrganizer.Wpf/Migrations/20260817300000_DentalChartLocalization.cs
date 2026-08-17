using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817300000_DentalChartLocalization")]
public partial class DentalChartLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("SelectedTooth", "Selected tooth", "Ընտրված ատամ", "Выбранный зуб"),
            ("SelectedSurfaces", "Surfaces", "Մակերեսներ", "Поверхности"),
            ("NoSurfaceSelected", "Select a tooth surface to begin.", "Ընտրեք ատամի մակերես՝ սկսելու համար։", "Выберите поверхность зуба, чтобы начать."),
            ("ApplyProcedure", "Apply procedure", "Կիրառել միջամտություն", "Назначить процедуру"),
            ("ChartLoadFailed", "Could not load the dental chart.", "Ատամնաքարտը չհաջողվեց բեռնել։", "Не удалось загрузить зубную карту."),
            ("Retry", "Retry", "Կրկնել", "Повторить"),
            ("LoadingChart", "Loading chart...", "Ատամնաքարտը բեռնվում է...", "Загрузка карты..."),
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
