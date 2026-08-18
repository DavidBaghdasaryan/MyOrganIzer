using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817900000_ProcedureScopeContextLocalization")]
public partial class ProcedureScopeContextLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("SelectProcedure", "Select a procedure to continue.", "Ընտրեք միջամտություն՝ շարունակելու համար։", "Выберите процедуру, чтобы продолжить."),
            ("EndodonticContextHint", "Root canal context. The canal diagram will be added in a later phase.",
                "Արմատախողովակի համատեքստ։ Խողովակի գծագիրը կավելացվի հաջորդ փուլում։",
                "Контекст корневого канала. Схема каналов будет добавлена позже."),
            ("WholeToothContextHint", "This procedure applies to the whole tooth.",
                "Այս միջամտությունը կիրառվում է ամբողջ ատամի վրա։",
                "Эта процедура применяется ко всему зубу."),
            ("CanalVisualPlaceholder", "Canal visual", "Խողովակի պատկեր", "Схема канала"),
            ("UnclassifiedProcedureHint", "This procedure has no surface or canal context. Use Apply procedure.",
                "Այս միջամտությունը մակերեսի կամ խողովակի համատեքստ չունի։ Օգտագործեք «Կիրառել միջամտություն»։",
                "У этой процедуры нет контекста поверхности или канала. Используйте «Назначить процедуру»."),
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
