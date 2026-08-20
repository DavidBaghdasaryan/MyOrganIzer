using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820221000_ExpensesAndSuppliersLocalization")]
public partial class ExpensesAndSuppliersLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void Seed(MigrationBuilder migrationBuilder)
    {
        var keys = new (string Key, string En, string Hy, string Ru)[]
        {
            ("Suppliers", "Suppliers", "Մատակարարներ", "Поставщики"),
            ("SuppliersSubtitle", "Labs, technicians, and vendors that supply products or services.", "Լաբորատորիաներ, տեխնիկներ և վաճառողներ, որոնք մատակարարում են ապրանքներ կամ ծառայություններ։", "Лаборатории, техники и вендоры, поставляющие товары или услуги."),
            ("AddSupplier", "Add supplier", "Ավելացնել մատակարար", "Добавить поставщика"),
            ("EditSupplier", "Edit supplier", "Խմբագրել մատակարարին", "Редактировать поставщика"),
            ("DeleteSupplier", "Hide supplier?", "Թաքցնե՞լ մատակարարին", "Скрыть поставщика?"),
            ("DeleteSupplierMessage", "\"{0}\" will be hidden from the list.", "«{0}»-ը կթաքցվի ցանկից։", "«{0}» будет скрыт из списка."),
            ("NoSuppliersYet", "No suppliers yet", "Մատակարարներ չկան", "Поставщиков пока нет"),
            ("AddFirstSupplier", "Add a supplier to get started.", "Ավելացրեք մատակարար՝ սկսելու համար։", "Добавьте поставщика, чтобы начать."),
            ("NoSuppliersFound", "No suppliers found", "Մատակարարներ չեն գտնվել", "Поставщики не найдены"),
            ("SearchSuppliers", "Search suppliers...", "Որոնել մատակարարներ...", "Поиск поставщиков..."),
            ("SupplierDetails", "Supplier details", "Մատակարարի տվյալներ", "Данные поставщика"),
            ("Email", "Email", "Էլ. փոստ", "Эл. почта"),
            ("Notes", "Notes", "Նշումներ", "Заметки"),
            ("Products", "Products", "Ապրանքներ", "Товары"),
            ("Services", "Services", "Ծառայություններ", "Услуги"),
            ("AddProduct", "Add product", "Ավելացնել ապրանք", "Добавить товар"),
            ("AddService", "Add service", "Ավելացնել ծառայություն", "Добавить услугу"),
            ("EditOffering", "Edit item", "Խմբագրել դիրքը", "Редактировать позицию"),
            ("DeleteOffering", "Hide item?", "Թաքցնե՞լ դիրքը", "Скрыть позицию?"),
            ("DeleteOfferingMessage", "\"{0}\" will be hidden.", "«{0}»-ը կթաքցվի։", "«{0}» будет скрыта."),
            ("NoProductsYet", "No products yet", "Ապրանքներ չկան", "Товаров пока нет"),
            ("NoServicesYet", "No services yet", "Ծառայություններ չկան", "Услуг пока нет"),
            ("DefaultPrice", "Default price", "Լռելյայն գին", "Цена по умолчанию"),
            ("ExpenseHistory", "Expense history", "Ծախսերի պատմություն", "История расходов"),
            ("NoExpenseHistory", "No expenses for this supplier yet.", "Այս մատակարարի համար ծախսեր չկան։", "По этому поставщику расходов пока нет."),
            ("Expenses", "Expenses", "Ծախսեր", "Расходы"),
            ("ExpensesSubtitle", "Supplier invoices with products and services.", "Մատակարարների հաշիվներ՝ ապրանքներով և ծառայություններով։", "Счета поставщиков с товарами и услугами."),
            ("AddExpense", "Add expense", "Ավելացնել ծախս", "Добавить расход"),
            ("EditExpense", "Edit expense", "Խմբագրել ծախսը", "Редактировать расход"),
            ("DeleteExpense", "Delete expense?", "Ջնջե՞լ ծախսը", "Удалить расход?"),
            ("DeleteExpenseMessage", "This expense and its lines will be removed.", "Այս ծախսը և նրա տողերը կջնջվեն։", "Этот расход и его строки будут удалены."),
            ("NoExpensesYet", "No expenses yet", "Ծախսեր չկան", "Расходов пока нет"),
            ("AddFirstExpense", "Create the first expense to begin.", "Ստեղծեք առաջին ծախսը՝ սկսելու համար։", "Создайте первый расход, чтобы начать."),
            ("NoExpensesFound", "No expenses found", "Ծախսեր չեն գտնվել", "Расходы не найдены"),
            ("SearchExpenses", "Search expenses...", "Որոնել ծախսեր...", "Поиск расходов..."),
            ("Supplier", "Supplier", "Մատակարար", "Поставщик"),
            ("Reference", "Reference", "Հղում", "Ссылка"),
            ("Total", "Total", "Ընդամենը", "Итого"),
            ("Quantity", "Qty", "Քանակ", "Кол-во"),
            ("UnitPrice", "Unit price", "Միավորի գին", "Цена единицы"),
            ("LineTotal", "Line total", "Տողի գումար", "Сумма строки"),
            ("Description", "Description", "Նկարագրություն", "Описание"),
            ("Kind", "Kind", "Տեսակ", "Тип"),
            ("Product", "Product", "Ապրանք", "Товар"),
            ("Service", "Service", "Ծառայություն", "Услуга"),
            ("AddLine", "Add line", "Ավելացնել տող", "Добавить строку"),
            ("RemoveLine", "Remove line", "Հեռացնել տողը", "Удалить строку"),
            ("ExpenseLines", "Lines", "Տողեր", "Строки"),
            ("Tooth", "Tooth", "Ատամ", "Зуб"),
            ("CatalogProcedure", "Procedure", "Միջամտություն", "Процедура"),
            ("SelectSupplier", "Select a supplier.", "Ընտրեք մատակարար։", "Выберите поставщика."),
            ("SelectOffering", "Select a product or service on every line.", "Յուրաքանչյուր տողում ընտրեք ապրանք կամ ծառայություն։", "На каждой строке выберите товар или услугу."),
            ("NeedLine", "Add at least one expense line.", "Ավելացրեք առնվազն մեկ տող։", "Добавьте хотя бы одну строку."),
            ("OptionalNone", "(none)", "(չկա)", "(нет)"),
            ("LinesCount", "Lines", "Տողեր", "Строки"),
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
