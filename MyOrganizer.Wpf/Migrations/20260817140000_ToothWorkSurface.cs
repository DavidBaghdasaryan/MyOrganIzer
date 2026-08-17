using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817140000_ToothWorkSurface")]
public partial class ToothWorkSurface : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            migrationBuilder.AddColumn<string>(
                name: "Surface",
                table: "ToothWorks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
        else
        {
            migrationBuilder.AddColumn<string>(
                name: "Surface",
                table: "ToothWorks",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        SeedChartLocalization(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Surface", table: "ToothWorks");
    }

    private static void SeedChartLocalization(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("Mesial", "Mesial", "Մեզիալ", "Мезиальная"),
            ("Distal", "Distal", "Դիստալ", "Дистальная"),
            ("Buccal", "Buccal", "Բուկալ", "Щечная"),
            ("Lingual", "Lingual", "Լինգվալ", "Язычная"),
            ("Occlusal", "Occlusal", "Օկլյուզալ", "Окклюзионная"),
            ("Incisal", "Incisal", "Ինցիզալ", "Режущая"),
            ("WholeTooth", "Whole tooth", "Ամբողջ ատամ", "Весь зуб"),
            ("ClearTooth", "Clear tooth", "Մաքրել ատամը", "Очистить зуб"),
            ("ClearSelectedSurfaces", "Clear selected surfaces", "Մաքրել ընտրված մակերեսները", "Очистить выбранные поверхности"),
            ("UpperArch", "Upper arch", "Վերին աղեղ", "Верхняя челюсть"),
            ("LowerArch", "Lower arch", "Ստորին աղեղ", "Нижняя челюсть"),
            ("ToothChart", "Tooth chart", "Ատամնաքարտ", "Зубная карта"),
            ("ToothChartHint", "Click a surface. Ctrl+click to select several. Right-click to apply a procedure.",
                "Սեղմեք մակերեսը։ Ctrl+սեղմում՝ մի քանիսը ընտրելու համար։ Աջ սեղմում՝ միջամտություն կիրառելու համար։",
                "Нажмите на поверхность. Ctrl+клик — несколько. Правый клик — назначить процедуру."),
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
