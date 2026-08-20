using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyOrganizer.Wpf.Data;

#nullable disable

namespace MyOrganizer.Wpf.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260821100000_RepairUnicodeLocalization")]
public partial class RepairUnicodeLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => Seed(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private void Seed(MigrationBuilder migrationBuilder)
    {
        var unicode = ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
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
            ("ToothChartHint", "Click a surface. Ctrl+click to select several. Right-click to apply a procedure.", "Սեղմեք մակերեսը։ Ctrl+սեղմում՝ մի քանիսը ընտրելու համար։ Աջ սեղմում՝ միջամտություն կիրառելու համար։", "Нажмите на поверхность. Ctrl+клик — несколько. Правый клик — назначить процедуру."),
            ("Dashboard", "Dashboard", "Գլխավոր", "Главная"),
            ("Settings", "Settings", "Կարգավորումներ", "Настройки"),
            ("Procedures", "Procedures", "Միջամտություններ", "Процедуры"),
            ("ShellWelcome", "Welcome", "Բարի գալուստ", "Добро пожаловать"),
            ("ShellWelcomeHint", "Choose a section in the sidebar. Existing screens stay available until they are moved into this workspace.", "Ընտրեք բաժինը կողային վահանակից։ Գործող էկրանները հասանելի կմնան, մինչև տեղափոխվեն այստեղ։", "Выберите раздел в меню. Текущие окна остаются доступны, пока не будут перенесены сюда."),
            ("OpenExistingWindow", "Open current window", "Բացել ընթացիկ պատուհանը", "Открыть текущее окно"),
            ("ComingSoon", "This section will open here in a later update.", "Այս բաժինը կբացվի այստեղ հաջորդ թարմացումներում։", "Этот раздел появится здесь в следующем обновлении."),
            ("NoRemindersToday", "No appointments today", "Այսօր այցեր չկան", "На сегодня записей нет"),
            ("TodayAppointments", "Today's appointments", "Այսօրվա այցեր", "Записи на сегодня"),
            ("AddClient", "Add client", "Ավելացնել հաճախորդ", "Добавить клиента"),
            ("ClientsSubtitle", "Manage patients and their dental records.", "Կառավարեք հաճախորդներին և ատամնաքարտերը։", "Управляйте пациентами и зубными картами."),
            ("SearchPlaceholder", "Search...", "Որոնել...", "Поиск..."),
            ("FilterByMonth", "Registration month", "Գրանցման ամիս", "Месяц регистрации"),
            ("NoClientsYet", "No clients yet", "Հաճախորդներ չկան", "Клиентов пока нет"),
            ("NoClientsFound", "No clients found", "Հաճախորդներ չեն գտնվել", "Клиенты не найдены"),
            ("AddFirstClient", "Add your first client to get started.", "Ավելացրեք առաջին հաճախորդին։", "Добавьте первого клиента, чтобы начать."),
            ("TryChangingFilters", "Try changing your search or filters.", "Փոխեք որոնումը կամ ֆիլտրերը։", "Измените поиск или фильтры."),
            ("Open", "Open", "Բացել", "Открыть"),
            ("OpenPatient", "Open patient", "Բացել հաճախորդին", "Открыть пациента"),
            ("Overview", "Overview", "Ընդհանուր", "Обзор"),
            ("Contact", "Contact", "Կոնտակտ", "Контакт"),
            ("Financial", "Financial", "Ֆինանսներ", "Финансы"),
            ("More", "More", "Ավելին", "Ещё"),
            ("LoadingPatient", "Loading patient...", "Հաճախորդը բեռնվում է...", "Загрузка пациента..."),
            ("ClientNotFound", "This patient could not be found.", "Հաճախորդը չի գտնվել։", "Пациент не найден."),
            ("DentalChartHint", "The dental chart will open here in a later update. Use the button to open the current chart.", "Ատամնաքարտը կբացվի այստեղ հաջորդ թարմացումում։ Այժմ օգտագործեք կոճակը՝ գործող քարտը բացելու համար։", "Зубная карта появится здесь в следующем обновлении. Сейчас откройте текущую карту кнопкой."),
            ("SelectedTooth", "Selected tooth", "Ընտրված ատամ", "Выбранный зуб"),
            ("SelectedSurfaces", "Surfaces", "Մակերեսներ", "Поверхности"),
            ("NoSurfaceSelected", "Select a tooth surface to begin.", "Ընտրեք ատամի մակերես՝ սկսելու համար։", "Выберите поверхность зуба, чтобы начать."),
            ("ApplyProcedure", "Apply procedure", "Կիրառել միջամտություն", "Назначить процедуру"),
            ("ChartLoadFailed", "Could not load the dental chart.", "Ատամնաքարտը չհաջողվեց բեռնել։", "Не удалось загрузить зубную карту."),
            ("Retry", "Retry", "Կրկնել", "Повторить"),
            ("LoadingChart", "Loading chart...", "Ատամնաքարտը բեռնվում է...", "Загрузка карты..."),
            ("Maximize", "Maximize", "Ծավալել", "Развернуть"),
            ("Restore", "Restore", "Վերականգնել", "Восстановить"),
            ("Apply", "Apply", "Կիրառել", "Применить"),
            ("SearchProcedures", "Search procedures...", "Որոնել միջամտություններ...", "Поиск процедур..."),
            ("ProcedureApplied", "Procedure applied", "Միջամտությունը կիրառված է", "Процедура назначена"),
            ("AddPatient", "Add patient", "Ավելացնել հաճախորդ", "Добавить пациента"),
            ("EditPatient", "Edit patient", "Խմբագրել հաճախորդին", "Редактировать пациента"),
            ("FieldRequired", "This field is required.", "Այս դաշտը պարտադիր է։", "Это поле обязательно."),
            ("Confirm", "Confirm", "Հաստատել", "Подтвердить"),
            ("ProceduresSubtitle", "Manage dental procedures and pricing.", "Կառավարեք ատամնաբուժական միջամտությունները և գները։", "Управление стоматологическими процедурами и ценами."),
            ("AddProcedure", "Add procedure", "Ավելացնել միջամտություն", "Добавить процедуру"),
            ("EditProcedure", "Edit procedure", "Խմբագրել միջամտությունը", "Редактировать процедуру"),
            ("DeleteProcedure", "Delete procedure?", "Ջնջե՞լ միջամտությունը", "Удалить процедуру?"),
            ("DeleteProcedureMessage", "\"{0}\" will be removed.", "«{0}»-ը կջնջվի։", "«{0}» будет удалена."),
            ("NoProceduresYet", "No procedures yet", "Միջամտություններ չկան", "Процедур пока нет"),
            ("AddFirstProcedure", "Create the first procedure to begin.", "Ստեղծեք առաջին միջամտությունը՝ սկսելու համար։", "Создайте первую процедуру, чтобы начать."),
            ("NoProceduresFound", "No procedures found", "Միջամտություններ չեն գտնվել", "Процедуры не найдены"),
            ("TechniciansSubtitle", "Record technician payments for lab work.", "Գրանցեք տեխնիկներին վճարումները լաբորատոր աշխատանքի համար։", "Учёт выплат техникам за лабораторные работы."),
            ("AddTechnician", "Add technician", "Ավելացնել տեխնիկ", "Добавить техника"),
            ("EditTechnician", "Edit technician", "Խմբագրել տեխնիկին", "Редактировать техника"),
            ("DeleteTechnician", "Delete technician?", "Ջնջե՞լ տեխնիկի գրառումը", "Удалить запись техника?"),
            ("DeleteTechnicianMessage", "\"{0}\" will be removed.", "«{0}»-ը կջնջվի։", "«{0}» будет удалена."),
            ("NoTechniciansYet", "No technicians yet", "Տեխնիկներ չկան", "Техников пока нет"),
            ("AddFirstTechnician", "Add a technician to get started.", "Ավելացրեք տեխնիկ՝ սկսելու համար։", "Добавьте техника, чтобы начать."),
            ("NoTechniciansFound", "No technicians found", "Տեխնիկներ չեն գտնվել", "Техники не найдены"),
            ("SearchTechnicians", "Search technicians...", "Որոնել տեխնիկներ...", "Поиск техников..."),
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
            ("SurfaceSelectorHint", "Click a region. Ctrl+click selects several surfaces.", "Սեղմեք մակերեսը։ Ctrl+սեղմում՝ մի քանիսը ընտրելու համար։", "Нажмите на область. Ctrl+клик выбирает несколько поверхностей."),
            ("SelectProcedure", "Select a procedure to continue.", "Ընտրեք միջամտություն՝ շարունակելու համար։", "Выберите процедуру, чтобы продолжить."),
            ("EndodonticContextHint", "Root canal context. The canal diagram will be added in a later phase.", "Արմատախողովակի համատեքստ։ Խողովակի գծագիրը կավելացվի հաջորդ փուլում։", "Контекст корневого канала. Схема каналов будет добавлена позже."),
            ("WholeToothContextHint", "This procedure applies to the whole tooth.", "Այս միջամտությունը կիրառվում է ամբողջ ատամի վրա։", "Эта процедура применяется ко всему зубу."),
            ("CanalVisualPlaceholder", "Canal visual", "Խողովակի պատկեր", "Схема канала"),
            ("UnclassifiedProcedureHint", "This procedure has no surface or canal context. Use Apply procedure.", "Այս միջամտությունը մակերեսի կամ խողովակի համատեքստ չունի։ Օգտագործեք «Կիրառել միջամտություն»։", "У этой процедуры нет контекста поверхности или канала. Используйте «Назначить процедуру»."),
            ("CurrentToothState", "Current state", "Ընթացիկ վիճակ", "Текущее состояние"),
            ("TreatmentHistory", "Treatment history", "Բուժման պատմություն", "История лечения"),
            ("EndodonticNone", "None", "Չկա", "Нет"),
            ("WholeToothNormal", "Normal", "Նորմալ", "Норма"),
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
            Upsert(migrationBuilder, unicode, key, "en", en);
            Upsert(migrationBuilder, unicode, key, "hy", hy);
            Upsert(migrationBuilder, unicode, key, "ru", ru);
        }
    }

    private static string Lit(bool unicode, string value) =>
        (unicode ? "N" : "") + "'" + value.Replace("'", "''") + "'";

    private static void Upsert(MigrationBuilder migrationBuilder, bool unicode, string key, string lang, string value)
    {
        var keyLit = Lit(unicode, key);
        var langLit = Lit(unicode, lang);
        var valLit = Lit(unicode, value);
        migrationBuilder.Sql($"""
            INSERT INTO L10nKeys ([Key])
            SELECT {keyLit}
            WHERE NOT EXISTS (SELECT 1 FROM L10nKeys WHERE [Key] = {keyLit});

            UPDATE L10nValues
            SET Value = {valLit}
            WHERE Lang = {langLit}
              AND KeyId = (SELECT Id FROM L10nKeys WHERE [Key] = {keyLit});

            INSERT INTO L10nValues (KeyId, Lang, Value)
            SELECT k.Id, {langLit}, {valLit}
            FROM L10nKeys k
            WHERE k.[Key] = {keyLit}
              AND NOT EXISTS (
                  SELECT 1 FROM L10nValues v
                  WHERE v.KeyId = k.Id AND v.Lang = {langLit});
            """);
    }
}
