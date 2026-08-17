using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817180000_ShellLocalization")]
public partial class ShellLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        SeedShellLocalization(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void SeedShellLocalization(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("Dashboard", "Dashboard", "Գլխավոր", "Главная"),
            ("Settings", "Settings", "Կարգավորումներ", "Настройки"),
            ("Procedures", "Procedures", "Միջամտություններ", "Процедуры"),
            ("ShellWelcome", "Welcome", "Բարի գալուստ", "Добро пожаловать"),
            ("ShellWelcomeHint",
                "Choose a section in the sidebar. Existing screens stay available until they are moved into this workspace.",
                "Ընտրեք բաժինը կողային վահանակից։ Գործող էկրանները հասանելի կմնան, մինչև տեղափոխվեն այստեղ։",
                "Выберите раздел в меню. Текущие окна остаются доступны, пока не будут перенесены сюда."),
            ("OpenExistingWindow", "Open current window", "Բացել ընթացիկ պատուհանը", "Открыть текущее окно"),
            ("ComingSoon",
                "This section will open here in a later update.",
                "Այս բաժինը կբացվի այստեղ հաջորդ թարմացումներում։",
                "Этот раздел появится здесь в следующем обновлении."),
            ("NoRemindersToday", "No appointments today", "Այսօր այցեր չկան", "На сегодня записей нет"),
            ("TodayAppointments", "Today's appointments", "Այսօրվա այցեր", "Записи на сегодня"),
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
