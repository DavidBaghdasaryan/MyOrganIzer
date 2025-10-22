using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyOrganizer.Wpf.Migrations
{
    /// <inheritdoc />
    public partial class MigrstionforLang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT [dbo].[Languages] ([Code], [Name]) VALUES (N'en', N'English')
GO
INSERT [dbo].[Languages] ([Code], [Name]) VALUES (N'hy', N'Armenian')
GO
INSERT [dbo].[Languages] ([Code], [Name]) VALUES (N'ru', N'Russian')
GO
");
            migrationBuilder.Sql(@"

/****** Object:  StoredProcedure [dbo].[usp_L10n_UpsertValue]    Script Date: 10/22/2025 2:45:53 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[usp_L10n_UpsertValue]
  @Key nvarchar(200), @Lang nvarchar(5), @Value nvarchar(2000)
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @KeyId int = (SELECT Id FROM L10nKeys WHERE [Key]=@Key);
  IF @KeyId IS NULL BEGIN
    INSERT INTO L10nKeys([Key]) VALUES (@Key);
    SET @KeyId = SCOPE_IDENTITY();
  END
  IF EXISTS (SELECT 1 FROM L10nValues WHERE KeyId=@KeyId AND Lang=@Lang)
    UPDATE L10nValues SET [Value]=@Value WHERE KeyId=@KeyId AND Lang=@Lang;
  ELSE
    INSERT INTO L10nValues(KeyId,Lang,[Value]) VALUES(@KeyId,@Lang,@Value);
END
GO");
            // -------- L10nKeys --------
            migrationBuilder.Sql(@"
SET NOCOUNT ON;

SET IDENTITY_INSERT dbo.L10nKeys ON;

;WITH SrcKeys AS (
    SELECT * FROM (VALUES
        (1,  N'Save',               NULL, N'Save action'),
        (2,  N'Cancel',             NULL, N'Cancel action'),
        (3,  N'Delete',             NULL, N'Delete action'),
        (4,  N'Add',                NULL, N'Add action'),
        (5,  N'Name',               NULL, N'Entity name'),
        (6,  N'Description',        NULL, N'Entity description'),
        (7,  N'Required',           NULL, N'Field required error'),
        (8,  N'Success',            NULL, N'Success message'),
        (9,  N'Error',              NULL, N'Unexpected error'),
        (10, N'CurLanguage',        NULL, NULL),
        (11, N'Enterpassword',      NULL, NULL),
        (12, N'Price',              NULL, NULL),
        (13, N'Debt',               NULL, NULL),
        (14, N'MonthIncome',        NULL, NULL),
        (15, N'MonthDebt',          NULL, NULL),
        (16, N'TotalAmount',        NULL, NULL),
        (17, N'SearchBy',           NULL, NULL),
        (18, N'Search',             NULL, NULL),
        (19, N'ClientDetails',      NULL, NULL),
        (20, N'RegistrationDate',   NULL, NULL),
        (21, N'ScheduleDoubleVisit',NULL, NULL),
        (22, N'OngoingWorks',       NULL, NULL),
        (23, N'Clear',              NULL, NULL),
        (24, N'SaveDoubleVisit',    NULL, NULL),
        (25, N'Close',              NULL, NULL),
        (26, N'FirstName',          NULL, NULL),
        (27, N'LastName',           NULL, NULL),
        (28, N'MiddlName',          NULL, NULL),
        (29, N'Phone',              NULL, NULL),
        (30, N'PasswordRequest',    NULL, NULL),
        (31, N'AppTitle',           NULL, NULL),
        (32, N'Enter',              NULL, NULL),
        (33, N'ArmenianAM',         NULL, NULL),
        (34, N'RussianRU',          NULL, NULL),
        (35, N'EnglishEN',          NULL, NULL),
        (36, N'Clients',            NULL, NULL),
        (37, N'Messages',           NULL, NULL),
        (38, N'Senders',            NULL, NULL),
        (39, N'Technicians',        NULL, NULL),
        (40, N'Minimize',           NULL, NULL),
        (41, N'Exit',               NULL, NULL),
        (42, N'Materials',          NULL, NULL),
        (43, N'PaidAmount',         NULL, NULL),
        (44, N'TransactionDate',    NULL, NULL),
        (45, N'FullName',           NULL, NULL),
        (46, N'Edit',               NULL, NULL),
        (47, N'ViewAmount',         NULL, NULL),
        (48, N'PaidPerMonth',       NULL, NULL),
        (49, N'Patronymic',         NULL, NULL),
        (50, N'SetPrices',          NULL, NULL),
        (51, N'Type',               NULL, NULL),
        (52, N'Tier1',              NULL, NULL),
        (53, N'Tier2',              NULL, NULL),
        (54, N'Tier3',              NULL, NULL),
        (55, N'OK',                 NULL, NULL)
    ) AS v([Id],[Key],[Group],[Description])
)
INSERT INTO dbo.L10nKeys ([Id],[Key],[Group],[Description])
SELECT s.[Id], s.[Key], s.[Group], s.[Description]
FROM SrcKeys s
WHERE NOT EXISTS (SELECT 1 FROM dbo.L10nKeys k WHERE k.Id = s.Id);

SET IDENTITY_INSERT dbo.L10nKeys OFF;
");


            // -------- L10nValues --------
            migrationBuilder.Sql(@"
SET NOCOUNT ON;

;WITH SrcValues AS (
    SELECT * FROM (VALUES
        (1, N'en', N'Save'), (1, N'hy', N'Պահպանել'), (1, N'ru', N'Сохранить'),
        (2, N'en', N'Cancel'), (2, N'hy', N'Չեղարկել'), (2, N'ru', N'Отмена'),
        (3, N'en', N'Delete'), (3, N'hy', N'Ջնջել'), (3, N'ru', N'Удалить'),
        (4, N'en', N'Add'), (4, N'hy', N'Ավելացնել'), (4, N'ru', N'Добавить'),
        (5, N'en', N'Name'), (5, N'hy', N'Անվանում'), (5, N'ru', N'Название'),
        (6, N'en', N'Description'), (6, N'hy', N'Նկարագրություն'), (6, N'ru', N'Описание'),
        (7, N'en', N'This field is required'), (7, N'hy', N'Դաշտը պարտադիր է'), (7, N'ru', N'Это поле обязательно'),
        (8, N'en', N'Successfully saved'), (8, N'hy', N'Հաջողությամբ պահպանվեց'), (8, N'ru', N'Успешно сохранено'),
        (9, N'en', N'Unexpected error. Please try again.'), (9, N'hy', N'Անսպասելի սխալ'), (9, N'ru', N'Непредвиденная ошибка'),
        (10, N'en', N'Language'), (10, N'hy', N'Ընթացիկ լեզու'), (10, N'ru', N'Текущий язык'),
        (11, N'en', N'Enter password'), (11, N'hy', N'Մուտքագրեք գաղտնաբառը'), (11, N'ru', N'Введите пароль'),
        (12, N'en', N'Price'), (12, N'hy', N'Փող'), (12, N'ru', N'Деньги'),
        (13, N'en', N'Debt'), (13, N'hy', N'Հարկ/ապառք'), (13, N'ru', N'Долг'),
        (14, N'en', N'Income for this month'), (14, N'hy', N'Եկամուտ այս ամսվա համար'), (14, N'ru', N'Доход за этот месяц'),
        (15, N'en', N'Debt for this month'), (15, N'hy', N'Պարտք այս ամսվա համար'), (15, N'ru', N'за этот месяц'),
        (16, N'en', N'Total amount'), (16, N'hy', N'Ընդհանուր գումար'), (16, N'ru', N'Общая сумма'),
        (17, N'en', N'Search by'), (17, N'hy', N'Որոնել ըստ'), (17, N'ru', N'Искать по'),
        (18, N'en', N'Search'), (18, N'hy', N'Որոնել'), (18, N'ru', N'Поиск'),
        (19, N'en', N'Client details'), (19, N'hy', N'Հաճախորդի տվյալները'), (19, N'ru', N'Детали клиента'),
        (20, N'en', N'Registration date'), (20, N'hy', N'Գրանցման ամսաթիվ'), (20, N'ru', N'Дата регистрации'),
        (21, N'en', N'Schedule double visit'), (21, N'hy', N'Պլանավորել կրկնակի այցելություն'), (21, N'ru', N'Запланировать двойное посещение'),
        (22, N'en', N'Ongoing works'), (22, N'hy', N'Ընթացիկ աշխատանքներ'), (22, N'ru', N'Текущие работы'),
        (23, N'en', N'Clear'), (23, N'hy', N'Ջնջել'), (23, N'ru', N'Очистить'),
        (24, N'en', N'Save double visit'), (24, N'hy', N'Պահել կրկնակի այցելությունը'), (24, N'ru', N'Сохранить двойное посещение'),
        (25, N'en', N'Close'), (25, N'hy', N'Փակել'), (25, N'ru', N'Закрыть'),
        (26, N'en', N'First Name'), (26, N'hy', N'Ազգանուն'), (26, N'ru', N'Имя'),
        (27, N'en', N'Last Name'), (27, N'hy', N'Անուն'), (27, N'ru', N'Фамилия'),
        (28, N'en', N'Middle name'), (28, N'hy', N'Ազգանուն'), (28, N'ru', N'Отчество'),
        (29, N'en', N'Phone'), (29, N'hy', N'Հեռախոս'), (29, N'ru', N'Телефон'),
        (30, N'en', N'Password request'), (30, N'hy', N'Գաղտնաբառի հարցում'), (30, N'ru', N'Запрос пароля'),
        (31, N'en', N'Stom Organizer'), (31, N'hy', N'Ստոմ կազմակերպիչ'), (31, N'ru', N'Организатор STOM'),
        (32, N'en', N'Enter'), (32, N'hy', N'Մուտքագրել'), (32, N'ru', N'Ввод'),
        (33, N'en', N'Armenian (AM)'), (33, N'hy', N'Հայերեն (AM)'), (33, N'ru', N'Армянский (AM)'),
        (34, N'en', N'Russian (RU)'), (34, N'hy', N'Ռուսերեն (RU)'), (34, N'ru', N'Русский (RU)'),
        (35, N'en', N'English (EN)'), (35, N'hy', N'Անգլերեն (EN)'), (35, N'ru', N'Английский (EN)'),
        (36, N'en', N'Clients'), (36, N'hy', N'Հաճախորդներ'), (36, N'ru', N'Клиенты'),
        (37, N'en', N'Messages'), (37, N'hy', N'Հաղորդագրություններ'), (37, N'ru', N'Сообщения'),
        (38, N'en', N'Senders'), (38, N'hy', N'Ուղարկող'), (38, N'ru', N'Отправители'),
        (39, N'en', N'Technicians'), (39, N'hy', N'Տեխնիկներ'), (39, N'ru', N'Техники'),
        (40, N'en', N'Minimize'), (40, N'hy', N'Նվազեցնել'), (40, N'ru', N'Свернуть'),
        (41, N'en', N'Exit'), (41, N'hy', N'Exit'), (41, N'ru', N'Выход'),
        (42, N'en', N'Materials'), (42, N'hy', N'նյութերը։'), (42, N'ru', N'Материально-технические ресурсы'),
        (43, N'en', N'Paid amount'), (43, N'hy', N'Վճարված գումարը'), (43, N'ru', N'Выплаченная сумма'),
        (44, N'en', N'Transaction date'), (44, N'hy', N'Գործարքի ամսաթիվը'), (44, N'ru', N'Дата операции'),
        (45, N'en', N'Full name'), (45, N'hy', N'Ամբողջական անուն'), (45, N'ru', N'Полное имя'),
        (46, N'en', N'Edit'), (46, N'hy', N'Փոփոխել'), (46, N'ru', N'Правка'),
        (47, N'en', N'View amount'), (47, N'hy', N'Դիտել գումարը'), (47, N'ru', N'Посмотреть сумму'),
        (48, N'en', N'Paid amount / month'), (48, N'hy', N'Վճարված գումարը / ամիսը'), (48, N'ru', N'Оплаченная сумма / месяц'),
        (49, N'en', N'Patronymic'), (49, N'hy', N'Հայրանուն'), (49, N'ru', N'Отчевство'),
        (50, N'en', N'Set price list'), (50, N'hy', N'Սահմանել գնացուցակ'), (50, N'ru', N'Прейскурант'),
        (51, N'en', N'Type'), (51, N'hy', N'Տեսակ'), (51, N'ru', N'Тип'),
        (52, N'en', N'Tier 1'), (52, N'hy', N'Աստիճան 1'), (52, N'ru', N'Уровень 1'),
        (53, N'en', N'Tier 2'), (53, N'hy', N'Մակարդակ 2'), (53, N'ru', N'Уровень 2'),
        (54, N'en', N'Tier 3'), (54, N'hy', N'Մակարդակ 3'), (54, N'ru', N'Уровень 3'),
        (55, N'en', N'OK'), (55, N'hy', N'ԼԱՎ'), (55, N'ru', N'OK')
    ) AS v([KeyId],[Lang],[Value])
)
INSERT INTO dbo.L10nValues ([KeyId],[Lang],[Value])
SELECT s.[KeyId], s.[Lang], s.[Value]
FROM SrcValues s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.L10nValues lv
    WHERE lv.[KeyId] = s.[KeyId] AND lv.[Lang] = s.[Lang]
);
");


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
             
            migrationBuilder.Sql(@"
DELETE lv
FROM dbo.L10nValues lv
WHERE (lv.[KeyId] BETWEEN 1 AND 55)
  AND EXISTS (
        SELECT 1 FROM (VALUES
            (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
            (11),(12),(13),(14),(15),(16),(17),(18),(19),(20),
            (21),(22),(23),(24),(25),(26),(27),(28),(29),(30),
            (31),(32),(33),(34),(35),(36),(37),(38),(39),(40),
            (41),(42),(43),(44),(45),(46),(47),(48),(49),(50),
            (51),(52),(53),(54),(55)
        ) AS k([Id]) WHERE k.[Id] = lv.[KeyId]
  );
");

          
            migrationBuilder.Sql(@"
SET NOCOUNT ON;
SET IDENTITY_INSERT dbo.L10nKeys ON;

DELETE FROM dbo.L10nKeys
WHERE [Id] BETWEEN 1 AND 55;

SET IDENTITY_INSERT dbo.L10nKeys OFF;
");
        }
    }
}