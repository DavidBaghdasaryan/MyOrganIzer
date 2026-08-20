using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820231000_CatalogAndExpenseEditorLocalization")]
public partial class CatalogAndExpenseEditorLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("ProductsAndServices", "Products & Services", "Ապրանքներ և ծառայություններ", "Товары и услуги"),
            ("ProductsAndServicesSubtitle", "A shared catalog. Attach suppliers and prices when you need them.", "Ընդհանուր ցանկ. մատակարարները և գները կցեք ըստ անհրաժեշտության։", "Общий каталог. Привязывайте поставщиков и цены по необходимости."),
            ("AddCatalogItem", "Add product or service", "Ավելացնել ապրանք կամ ծառայություն", "Добавить товар или услугу"),
            ("EditCatalogItem", "Edit product or service", "Խմբագրել ապրանքը կամ ծառայությունը", "Редактировать товар или услугу"),
            ("DeleteCatalogItem", "Hide this item?", "Թաքցնե՞լ այս դիրքը", "Скрыть эту позицию?"),
            ("DeleteCatalogItemMessage", "\"{0}\" will be hidden from the catalog.", "«{0}»-ը կթաքցվի ցանկից։", "«{0}» будет скрыта из каталога."),
            ("NoCatalogItemsYet", "No products or services yet", "Ապրանքներ և ծառայություններ չկան", "Товаров и услуг пока нет"),
            ("AddFirstCatalogItem", "Add a product or service to get started.", "Ավելացրեք ապրանք կամ ծառայություն՝ սկսելու համար։", "Добавьте товар или услугу, чтобы начать."),
            ("NoCatalogItemsFound", "No matching items", "Համընկնումներ չկան", "Совпадений нет"),
            ("SearchCatalogItems", "Search products and services...", "Որոնել ապրանքներ և ծառայություններ...", "Поиск товаров и услуг..."),
            ("AllKinds", "All", "Բոլորը", "Все"),
            ("Units", "Units", "Չափման միավորներ", "Единицы измерения"),
            ("AddUnit", "Add unit", "Ավելացնել միավոր", "Добавить единицу"),
            ("EditUnit", "Edit unit", "Խմբագրել միավորը", "Редактировать единицу"),
            ("DeleteUnit", "Hide this unit?", "Թաքցնե՞լ այս միավորը", "Скрыть эту единицу?"),
            ("DeleteUnitMessage", "\"{0}\" will be hidden.", "«{0}»-ը կթաքցվի։", "«{0}» будет скрыта."),
            ("NoUnitsYet", "No units yet", "Միավորներ չկան", "Единиц пока нет"),
            ("BaseUnit", "Equals", "Հավասար է", "Равно"),
            ("ConversionFactor", "Quantity", "Քանակ", "Количество"),
            ("ConversionHint", "Optional. Example: 1 Syringe = 4 Gram.", "Ոչ պարտադիր։ Օրինակ՝ 1 ներարկիչ = 4 գրամ։", "Необязательно. Пример: 1 шприц = 4 грамма."),
            ("AssociateProduct", "Add product", "Ավելացնել ապրանք", "Добавить товар"),
            ("AssociateService", "Add service", "Ավելացնել ծառայություն", "Добавить услугу"),
            ("SupplierPrice", "Price", "Գին", "Цена"),
            ("ManualEntry", "Enter manually", "Մուտքագրել ձեռքով", "Ввести вручную"),
            ("AddItem", "+ Add item", "+ Ավելացնել դիրք", "+ Добавить позицию"),
            ("LinkToPatient", "Link to patient case", "Կապել հիվանդի հետ", "Связать со случаем пациента"),
            ("ProductOrService", "Product / Service", "Ապրանք / ծառայություն", "Товар / услуга"),
            ("Unit", "Unit", "Միավոր", "Единица"),
            ("Other", "Other", "Այլ", "Прочее"),
            ("OptionalSupplier", "(no supplier)", "(առանց մատակարարի)", "(без поставщика)"),
            ("CreateNewCatalogItem", "Create new…", "Ստեղծել նորը…", "Создать новый…"),
            ("AssociatedSuppliers", "Suppliers and prices", "Մատակարարներ և գներ", "Поставщики и цены"),
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
