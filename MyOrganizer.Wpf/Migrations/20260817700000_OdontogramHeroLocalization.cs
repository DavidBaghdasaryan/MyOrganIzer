using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817700000_OdontogramHeroLocalization")]
public partial class OdontogramHeroLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("Palatal", "Palatal", "Պալատինալ", "Нёбная"),
            ("CentralIncisor", "Central incisor", "Կենտրոնական կտրիչ", "Центральный резец"),
            ("LateralIncisor", "Lateral incisor", "Կողային կտրիչ", "Боковой резец"),
            ("Canine", "Canine", "Ժանիք", "Клык"),
            ("FirstPremolar", "First premolar", "Առաջին նախամոլյար", "Первый премоляр"),
            ("SecondPremolar", "Second premolar", "Երկրորդ նախամոլյար", "Второй премоляр"),
            ("FirstMolar", "First molar", "Առաջին մոլյար", "Первый моляр"),
            ("SecondMolar", "Second molar", "Երկրորդ մոլյար", "Второй моляр"),
            ("ThirdMolar", "Third molar", "Երրորդ մոլյար", "Третий моляр"),
            ("CurrentConditions", "Current conditions", "Առկա վիճակ", "Текущие состояния"),
            ("Healthy", "Healthy", "Առողջ", "Здоровый"),
            ("ConditionFilling", "Filling", "Պլոմբ", "Пломба"),
            ("ConditionRestoration", "Restoration", "Ռեստավրացիա", "Реставрация"),
            ("ConditionCrown", "Crown", "Պսակ", "Коронка"),
            ("ConditionImplant", "Implant", "Իմպլանտ", "Имплант"),
            ("ConditionEndo", "Endodontic treatment", "Էնդոդոնտիա", "Эндодонтия"),
            ("ConditionPartialDenture", "Partial denture", "Բյուգել", "Бюгель"),
            ("ConditionFullDenture", "Full denture", "Պրոթեզ", "Полный протез"),
            ("ConditionOther", "Other", "Այլ", "Другое"),
            ("ChartLegend", "Legend", "Լեգենդ", "Легенда"),
            ("NoConditionsYet", "No recorded treatment on this tooth.", "Այս ատամի վրա գրանցված միջամտություն չկա։", "На этом зубе нет записанного лечения."),
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
