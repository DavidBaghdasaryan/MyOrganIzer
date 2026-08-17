using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817200000_ClientWorkspaceLocalization")]
public partial class ClientWorkspaceLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("OpenPatient", "Open patient", "Բացել հաճախորդին", "Открыть пациента"),
            ("Overview", "Overview", "Ընդհանուր", "Обзор"),
            ("Contact", "Contact", "Կոնտակտ", "Контакт"),
            ("Financial", "Financial", "Ֆինանսներ", "Финансы"),
            ("More", "More", "Ավելին", "Ещё"),
            ("LoadingPatient", "Loading patient...", "Հաճախորդը բեռնվում է...", "Загрузка пациента..."),
            ("ClientNotFound", "This patient could not be found.", "Հաճախորդը չի գտնվել։", "Пациент не найден."),
            ("DentalChartHint",
                "The dental chart will open here in a later update. Use the button to open the current chart.",
                "Ատամնաքարտը կբացվի այստեղ հաջորդ թարմացումում։ Այժմ օգտագործեք կոճակը՝ գործող քարտը բացելու համար։",
                "Зубная карта появится здесь в следующем обновлении. Сейчас откройте текущую карту кнопкой."),
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
