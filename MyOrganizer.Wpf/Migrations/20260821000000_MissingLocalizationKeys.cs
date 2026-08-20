using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260821000000_MissingLocalizationKeys")]
public partial class MissingLocalizationKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("ToothLab", "Tooth Lab", "Ատամների լաբորատորիա", "Зубная лаборатория"),
            ("ClientsList", "Clients", "Հաճախորդներ", "Клиенты"),
            ("DayOfRegistration", "Registration date", "Գրանցման ամսաթիվ", "Дата регистрации"),
            ("Payment", "Payment", "Վճարում", "Оплата"),
            ("Remains", "Balance", "Մնացորդ", "Остаток"),
            ("DoubleVisit", "Follow-up", "Կրկնակի այց", "Повторный визит"),
            ("Incorrectpassword", "Incorrect password", "Սխալ գաղտնաբառ", "Неверный пароль"),
            ("SelectClient", "Select a client.", "Ընտրեք հաճախորդ։", "Выберите клиента."),
            ("Selecttheclienttodelete", "Select the client to delete.", "Ընտրեք հաճախորդին ջնջելու համար։", "Выберите клиента для удаления."),
            ("Deletelient.", "Delete this client?", "Ջնջե՞լ այս հաճախորդին։", "Удалить этого клиента?"),
            ("session", "visit", "այց", "визит"),
            ("Remove", "Remove", "Հեռացնել", "Удалить"),
            ("S.N", "No.", "Հ/հ", "№"),
            ("Info", "Info", "Տեղեկություն", "Справка"),
            ("MidlName", "Middle name", "Հայրանուն", "Отчество"),
            ("ConditionMissing", "Missing", "Բացակայում է", "Отсутствует"),
            ("SurfaceCaries", "Surface caries", "Մակերեսային կարիես", "Поверхностный кариес"),
            ("MediumCaries", "Medium caries", "Միջին կարիես", "Средний кариес"),
            ("DeepCaries", "Deep caries", "Խորը կարիես", "Глубокий кариес"),
            ("OcclusalView", "Occlusal view", "Օկլյուզալ տեսք", "Окклюзионный вид"),
            ("ResetView", "Reset view", "Վերականգնել տեսքը", "Сбросить вид"),
            ("ShowSurfaceSegmentation", "Show surface segmentation", "Ցույց տալ մակերեսների բաժանումը", "Показать сегментацию поверхностей"),
            ("Inspect", "Inspect", "Ստուգել", "Просмотр"),
            ("Patient", "Patient", "Հիվանդ", "Пациент"),
            ("SelectedAsset", "Selected asset", "Ընտրված մոդել", "Выбранная модель"),
            ("CreateProcedure", "Create procedure", "Ստեղծել միջամտություն", "Создать процедуру"),
            ("SaveProcedure", "Save procedure", "Պահպանել միջամտությունը", "Сохранить процедуру"),
            ("NewProcedure", "New procedure", "Նոր միջամտություն", "Новая процедура"),
            ("FdiNotImported", "This FDI is not imported yet.", "Այս FDI-ն դեռ ներմուծված չէ։", "Этот FDI ещё не импортирован."),
            ("ProcedureType", "Procedure type", "Միջամտության տեսակ", "Тип процедуры"),
            ("NoProcedureRecordsYet", "No procedure records yet.", "Միջամտությունների գրառումներ չկան։", "Записей процедур пока нет."),
            ("SelectedSurfacesCount", "Selected surfaces: {0}", "Ընտրված մակերեսներ՝ {0}", "Выбранные поверхности: {0}"),
            ("SelectedCanalsCount", "Selected roots/canals: {0}", "Ընտրված արմատներ/խողովակներ՝ {0}", "Выбранные корни/каналы: {0}"),
        };

        foreach (var (key, en, hy, ru) in keys)
        {
            var escapedKey = key.Replace("'", "''");
            migrationBuilder.Sql($"""
                INSERT INTO L10nKeys ([Key])
                SELECT '{escapedKey}'
                WHERE NOT EXISTS (SELECT 1 FROM L10nKeys WHERE [Key] = '{escapedKey}');
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
            WHERE k.[Key] = '{key.Replace("'", "''")}'
              AND NOT EXISTS (
                  SELECT 1 FROM L10nValues v
                  WHERE v.KeyId = k.Id AND v.Lang = '{lang}');
            """);
}
