using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817190000_ClientsViewLocalization")]
public partial class ClientsViewLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("AddClient", "Add client", "Ավելացնել հաճախորդ", "Добавить клиента"),
            ("ClientsSubtitle", "Manage patients and their dental records.", "Կառավարեք հաճախորդներին և ատամնաքարտերը։", "Управляйте пациентами и зубными картами."),
            ("SearchPlaceholder", "Search...", "Որոնել...", "Поиск..."),
            ("FilterByMonth", "Registration month", "Գրանցման ամիս", "Месяц регистрации"),
            ("NoClientsYet", "No clients yet", "Հաճախորդներ չկան", "Клиентов пока нет"),
            ("NoClientsFound", "No clients found", "Հաճախորդներ չեն գտնվել", "Клиенты не найдены"),
            ("AddFirstClient", "Add your first client to get started.", "Ավելացրեք առաջին հաճախորդին։", "Добавьте первого клиента, чтобы начать."),
            ("TryChangingFilters", "Try changing your search or filters.", "Փոխեք որոնումը կամ ֆիլտրերը։", "Измените поиск или фильтры."),
            ("Open", "Open", "Բացել", "Открыть"),
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
