using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyOrganizer.Wpf.Migrations
{
    /// <inheritdoc />
    public partial class MigrstionforLang : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- Languages: провайдер-агностично, но идемпотентно ----------
            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Languages] WHERE [Code]='en') INSERT INTO [Languages]([Code],[Name]) VALUES ('en','English');
IF NOT EXISTS (SELECT 1 FROM [Languages] WHERE [Code]='hy') INSERT INTO [Languages]([Code],[Name]) VALUES ('hy','Armenian');
IF NOT EXISTS (SELECT 1 FROM [Languages] WHERE [Code]='ru') INSERT INTO [Languages]([Code],[Name]) VALUES ('ru','Russian');
");
            }
            else if (ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql(@"
INSERT OR IGNORE INTO Languages(Code,Name) VALUES ('en','English');
INSERT OR IGNORE INTO Languages(Code,Name) VALUES ('hy','Armenian');
INSERT OR IGNORE INTO Languages(Code,Name) VALUES ('ru','Russian');
");
            }

            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                // --------------------- SQL Server branch ---------------------
                // L10nKeys (ID=1..55) — вставляем только отсутствующие Id
                migrationBuilder.Sql(@"
SET IDENTITY_INSERT [L10nKeys] ON;

WITH seed(Id,[Key],[Group],[Description]) AS (
    SELECT * FROM (VALUES
(1,N'Save',NULL,N'Save action'),
(2,N'Cancel',NULL,N'Cancel action'),
(3,N'Delete',NULL,N'Delete action'),
(4,N'Add',NULL,N'Add action'),
(5,N'Name',NULL,N'Entity name'),
(6,N'Description',NULL,N'Entity description'),
(7,N'Required',NULL,N'Field required error'),
(8,N'Success',NULL,N'Success message'),
(9,N'Error',NULL,N'Unexpected error'),
(10,N'CurLanguage',NULL,NULL),
(11,N'Enterpassword',NULL,NULL),
(12,N'Price',NULL,NULL),
(13,N'Debt',NULL,NULL),
(14,N'MonthIncome',NULL,NULL),
(15,N'MonthDebt',NULL,NULL),
(16,N'TotalAmount',NULL,NULL),
(17,N'SearchBy',NULL,NULL),
(18,N'Search',NULL,NULL),
(19,N'ClientDetails',NULL,NULL),
(20,N'RegistrationDate',NULL,NULL),
(21,N'ScheduleDoubleVisit',NULL,NULL),
(22,N'OngoingWorks',NULL,NULL),
(23,N'Clear',NULL,NULL),
(24,N'SaveDoubleVisit',NULL,NULL),
(25,N'Close',NULL,NULL),
(26,N'FirstName',NULL,NULL),
(27,N'LastName',NULL,NULL),
(28,N'MiddlName',NULL,NULL),
(29,N'Phone',NULL,NULL),
(30,N'PasswordRequest',NULL,NULL),
(31,N'AppTitle',NULL,NULL),
(32,N'Enter',NULL,NULL),
(33,N'ArmenianAM',NULL,NULL),
(34,N'RussianRU',NULL,NULL),
(35,N'EnglishEN',NULL,NULL),
(36,N'Clients',NULL,NULL),
(37,N'Messages',NULL,NULL),
(38,N'Senders',NULL,NULL),
(39,N'Technicians',NULL,NULL),
(40,N'Minimize',NULL,NULL),
(41,N'Exit',NULL,NULL),
(42,N'Materials',NULL,NULL),
(43,N'PaidAmount',NULL,NULL),
(44,N'TransactionDate',NULL,NULL),
(45,N'FullName',NULL,NULL),
(46,N'Edit',NULL,NULL),
(47,N'ViewAmount',NULL,NULL),
(48,N'PaidPerMonth',NULL,NULL),
(49,N'Patronymic',NULL,NULL),
(50,N'SetPrices',NULL,NULL),
(51,N'Type',NULL,NULL),
(52,N'Tier1',NULL,NULL),
(53,N'Tier2',NULL,NULL),
(54,N'Tier3',NULL,NULL),
(55,N'OK',NULL,NULL),
(56,'Reminders',NULL,NULL),
(57,'Currency',NULL,NULL)
) AS t(Id,[Key],[Group],[Description])
)
INSERT INTO [L10nKeys](Id,[Key],[Group],[Description])
SELECT s.Id, s.[Key], s.[Group], s.[Description]
FROM seed s
WHERE NOT EXISTS (SELECT 1 FROM [L10nKeys] k WHERE k.Id = s.Id);

SET IDENTITY_INSERT [L10nKeys] OFF;
");

                // L10nValues — вставляем только отсутствующие пары (KeyId, Lang)
                migrationBuilder.Sql(@"
INSERT INTO [L10nValues] ([KeyId],[Lang],[Value])
SELECT v.[KeyId], v.[Lang], v.[Value]
FROM (VALUES
(1,'en',N'Save'),(1,'hy',N'Պահպանել'),(1,'ru',N'Сохранить'),
(2,'en',N'Cancel'),(2,'hy',N'Չեղարկել'),(2,'ru',N'Отмена'),
(3,'en',N'Delete'),(3,'hy',N'Ջնջել'),(3,'ru',N'Удалить'),
(4,'en',N'Add'),(4,'hy',N'Ավելացնել'),(4,'ru',N'Добавить'),
(5,'en',N'Name'),(5,'hy',N'Անվանում'),(5,'ru',N'Название'),
(6,'en',N'Description'),(6,'hy',N'Նկարագրություն'),(6,'ru',N'Описание'),
(7,'en',N'This field is required'),(7,'hy',N'Դաշտը պարտադիր է'),(7,'ru',N'Это поле обязательно'),
(8,'en',N'Successfully saved'),(8,'hy',N'Հաջողությամբ պահպանվեց'),(8,'ru',N'Успешно сохранено'),
(9,'en',N'Unexpected error. Please try again.'),(9,'hy',N'Անսպասելի սխալ'),(9,'ru',N'Непредвиденная ошибка'),
(10,'en',N'Language'),(10,'hy',N'Ընթացիկ լեզու'),(10,'ru',N'Текущий язык'),
(11,'en',N'Enter password'),(11,'hy',N'Մուտքագրեք գաղտնաբառը'),(11,'ru',N'Введите пароль'),
(12,'en',N'Price'),(12,'hy',N'Փող'),(12,'ru',N'Деньги'),
(13,'en',N'Debt'),(13,'hy',N'Հարկ/ապառք'),(13,'ru',N'Долг'),
(14,'en',N'Income for this month'),(14,'hy',N'Եկամուտ այս ամսվա համար'),(14,'ru',N'Доход за этот месяц'),
(15,'en',N'Debt for this month'),(15,'hy',N'Պարտք այս ամսվա համար'),(15,'ru',N'за этот месяц'),
(16,'en',N'Total amount'),(16,'hy',N'Ընդհանուր գումար'),(16,'ru',N'Общая сумма'),
(17,'en',N'Search by'),(17,'hy',N'Որոնել ըստ'),(17,'ru',N'Искать по'),
(18,'en',N'Search'),(18,'hy',N'Որոնել'),(18,'ru',N'Поиск'),
(19,'en',N'Client details'),(19,'hy',N'Հաճախորդի տվյալները'),(19,'ru',N'Детали клиента'),
(20,'en',N'Registration date'),(20,'hy',N'Գրանցման ամսաթիվ'),(20,'ru',N'Дата регистрации'),
(21,'en',N'Schedule double visit'),(21,'hy',N'Պլանավորել կրկնակի այցելություն'),(21,'ru',N'Запланировать двойное посещение'),
(22,'en',N'Ongoing works'),(22,'hy',N'Ընթացիկ աշխատանքներ'),(22,'ru',N'Текущие работы'),
(23,'en',N'Clear'),(23,'hy',N'Ջնջել'),(23,'ru',N'Очистить'),
(24,'en',N'Save double visit'),(24,'hy',N'Պահել կրկնակի այցելությունը'),(24,'ru',N'Сохранить двойное посещение'),
(25,'en',N'Close'),(25,'hy',N'Փակել'),(25,'ru',N'Закрыть'),
(26,'en',N'First Name'),(26,'hy',N'Ազգանուն'),(26,'ru',N'Имя'),
(27,'en',N'Last Name'),(27,'hy',N'Անուն'),(27,'ru',N'Фамилия'),
(28,'en',N'Middle name'),(28,'hy',N'Ազգանուն'),(28,'ru',N'Отчество'),
(29,'en',N'Phone'),(29,'hy',N'Հեռախոս'),(29,'ru',N'Телефон'),
(30,'en',N'Password request'),(30,'hy',N'Գաղտնաբառի հարցում'),(30,'ru',N'Запрос пароля'),
(31,'en',N'Stom Organizer'),(31,'hy',N'Ստոմ կազմակերպիչ'),(31,'ru',N'Организатор STOM'),
(32,'en',N'Enter'),(32,'hy',N'Մուտքագրել'),(32,'ru',N'Ввод'),
(33,'en',N'Armenian (AM)'),(33,'hy',N'Հայերեն (AM)'),(33,'ru',N'Армянский (AM)'),
(34,'en',N'Russian (RU)'),(34,'hy',N'Ռուսերեն (RU)'),(34,'ru',N'Русский (RU)'),
(35,'en',N'English (EN)'),(35,'hy',N'Անգլերեն (EN)'),(35,'ru',N'Английский (EN)'),
(36,'en',N'Clients'),(36,'hy',N'Հաճախորդներ'),(36,'ru',N'Клиенты'),
(37,'en',N'Messages'),(37,'hy',N'Հաղորդագրություններ'),(37,'ru',N'Сообщения'),
(38,'en',N'Senders'),(38,'hy',N'Ուղարկող'),(38,'ru',N'Отправители'),
(39,'en',N'Technicians'),(39,'hy',N'Տեխնիկներ'),(39,'ru',N'Техники'),
(40,'en',N'Minimize'),(40,'hy',N'Նվազեցնել'),(40,'ru',N'Свернуть'),
(41,'en',N'Exit'),(41,'hy',N'Exit'),(41,'ru',N'Выход'),
(42,'en',N'Materials'),(42,'hy',N'նյութերը։'),(42,'ru',N'Материально-технические ресурсы'),
(43,'en',N'Paid amount'),(43,'hy',N'Վճարված գումարը'),(43,'ru',N'Выплаченная сумма'),
(44,'en',N'Transaction date'),(44,'hy',N'Գործարքի ամսաթիվը'),(44,'ru',N'Дата операции'),
(45,'en',N'Full name'),(45,'hy',N'Ամբողջական անուն'),(45,'ru',N'Полное имя'),
(46,'en',N'Edit'),(46,'hy',N'Փոփոխել'),(46,'ru',N'Правка'),
(47,'en',N'View amount'),(47,'hy',N'Դիտել գումարը'),(47,'ru',N'Посмотреть сумму'),
(48,'en',N'Paid amount / month'),(48,'hy',N'Վճարված գումարը / ամիսը'),(48,'ru',N'Оплаченная сумма / месяц'),
(49,'en',N'Patronymic'),(49,'hy',N'Հայրանուն'),(49,'ru',N'Отчевство'),
(50,'en',N'Set price list'),(50,'hy',N'Սահմանել գնացուցակ'),(50,'ru',N'Прейскурант'),
(51,'en',N'Type'),(51,'hy',N'Տեսակ'),(51,'ru',N'Тип'),
(52,'en',N'Tier 1'),(52,'hy',N'Աստիճան 1'),(52,'ru',N'Уровень 1'),
(53,'en',N'Tier 2'),(53,'hy',N'Մակարդակ 2'),(53,'ru',N'Уровень 2'),
(54,'en',N'Tier 3'),(54,'hy',N'Մակարդակ 3'),(54,'ru',N'Уровень 3'),
(55,'en',N'OK'),(55,'hy',N'ԼԱՎ'),(55,'ru',N'OK'),
(56,'en',N'Reminders for today'),(56,'hy','Այսօրվա հիշեցումներ'),(56,'ru','Напоминания на сегодня'),
(57,'en','$'),(57,'hy','֏'),(57,'ru','₽')
) AS v([KeyId],[Lang],[Value])
WHERE NOT EXISTS (SELECT 1 FROM [L10nValues] x WHERE x.[KeyId]=v.[KeyId] AND x.[Lang]=v.[Lang]);
");

                // Stored procedure (создать, если нет)
                migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[usp_L10n_UpsertValue]', N'P') IS NULL
EXEC(N'
CREATE PROCEDURE [dbo].[usp_L10n_UpsertValue]
  @Key nvarchar(200), @Lang nvarchar(5), @Value nvarchar(2000)
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @KeyId int = (SELECT Id FROM L10nKeys WHERE [Key]=@Key);
  IF @KeyId IS NULL
  BEGIN
    INSERT INTO L10nKeys([Key]) VALUES (@Key);
    SET @KeyId = SCOPE_IDENTITY();
  END

  IF EXISTS (SELECT 1 FROM L10nValues WHERE KeyId=@KeyId AND Lang=@Lang)
    UPDATE L10nValues SET [Value]=@Value WHERE KeyId=@KeyId AND Lang=@Lang;
  ELSE
    INSERT INTO L10nValues(KeyId,Lang,[Value]) VALUES(@KeyId,@Lang,@Value);
END
');
");
            }
            else if (ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // --------------------- SQLite branch ---------------------
                // L10nKeys
                migrationBuilder.Sql(@"
INSERT OR IGNORE INTO L10nKeys(Id,[Key],[Group],[Description]) VALUES
(1,'Save',NULL,'Save action'),
(2,'Cancel',NULL,'Cancel action'),
(3,'Delete',NULL,'Delete action'),
(4,'Add',NULL,'Add action'),
(5,'Name',NULL,'Entity name'),
(6,'Description',NULL,'Entity description'),
(7,'Required',NULL,'Field required error'),
(8,'Success',NULL,'Success message'),
(9,'Error',NULL,'Unexpected error'),
(10,'CurLanguage',NULL,NULL),
(11,'Enterpassword',NULL,NULL),
(12,'Price',NULL,NULL),
(13,'Debt',NULL,NULL),
(14,'MonthIncome',NULL,NULL),
(15,'MonthDebt',NULL,NULL),
(16,'TotalAmount',NULL,NULL),
(17,'SearchBy',NULL,NULL),
(18,'Search',NULL,NULL),
(19,'ClientDetails',NULL,NULL),
(20,'RegistrationDate',NULL,NULL),
(21,'ScheduleDoubleVisit',NULL,NULL),
(22,'OngoingWorks',NULL,NULL),
(23,'Clear',NULL,NULL),
(24,'SaveDoubleVisit',NULL,NULL),
(25,'Close',NULL,NULL),
(26,'FirstName',NULL,NULL),
(27,'LastName',NULL,NULL),
(28,'MiddlName',NULL,NULL),
(29,'Phone',NULL,NULL),
(30,'PasswordRequest',NULL,NULL),
(31,'AppTitle',NULL,NULL),
(32,'Enter',NULL,NULL),
(33,'ArmenianAM',NULL,NULL),
(34,'RussianRU',NULL,NULL),
(35,'EnglishEN',NULL,NULL),
(36,'Clients',NULL,NULL),
(37,'Messages',NULL,NULL),
(38,'Senders',NULL,NULL),
(39,'Technicians',NULL,NULL),
(40,'Minimize',NULL,NULL),
(41,'Exit',NULL,NULL),
(42,'Materials',NULL,NULL),
(43,'PaidAmount',NULL,NULL),
(44,'TransactionDate',NULL,NULL),
(45,'FullName',NULL,NULL),
(46,'Edit',NULL,NULL),
(47,'ViewAmount',NULL,NULL),
(48,'PaidPerMonth',NULL,NULL),
(49,'Patronymic',NULL,NULL),
(50,'SetPrices',NULL,NULL),
(51,'Type',NULL,NULL),
(52,'Tier1',NULL,NULL),
(53,'Tier2',NULL,NULL),
(54,'Tier3',NULL,NULL),
(55,'OK',NULL,NULL),
(56,'Reminders',NULL,NULL),
(57,'Currency',NULL,NULL);
");

                // L10nValues
                migrationBuilder.Sql(@"
INSERT OR IGNORE INTO L10nValues(KeyId,Lang,Value) VALUES
(1,'en','Save'),(1,'hy','Պահպանել'),(1,'ru','Сохранить'),
(2,'en','Cancel'),(2,'hy','Չեղարկել'),(2,'ru','Отмена'),
(3,'en','Delete'),(3,'hy','Ջնջել'),(3,'ru','Удалить'),
(4,'en','Add'),(4,'hy','Ավելացնել'),(4,'ru','Добавить'),
(5,'en','Name'),(5,'hy','Անվանում'),(5,'ru','Название'),
(6,'en','Description'),(6,'hy','Նկարագրություն'),(6,'ru','Описание'),
(7,'en','This field is required'),(7,'hy','Դաշտը պարտադիր է'),(7,'ru','Это поле обязательно'),
(8,'en','Successfully saved'),(8,'hy','Հաջողությամբ պահպանվեց'),(8,'ru','Успешно сохранено'),
(9,'en','Unexpected error. Please try again.'),(9,'hy','Անսպասելի սխալ'),(9,'ru','Непредвиденная ошибка'),
(10,'en','Language'),(10,'hy','Ընթացիկ լեզու'),(10,'ru','Текущий язык'),
(11,'en','Enter password'),(11,'hy','Մուտքագրեք գաղտնաբառը'),(11,'ru','Введите пароль'),
(12,'en','Price'),(12,'hy','Փող'),(12,'ru','Деньги'),
(13,'en','Debt'),(13,'hy','Հարկ/ապառք'),(13,'ru','Долг'),
(14,'en','Income for this month'),(14,'hy','Եկամուտ այս ամսվա համար'),(14,'ru','Доход за этот месяц'),
(15,'en','Debt for this month'),(15,'hy','Պարտք այս ամսվա համար'),(15,'ru','за этот месяц'),
(16,'en','Total amount'),(16,'hy','Ընդհանուր գումար'),(16,'ru','Общая сумма'),
(17,'en','Search by'),(17,'hy','Որոնել ըստ'),(17,'ru','Искать по'),
(18,'en','Search'),(18,'hy','Որոնել'),(18,'ru','Поиск'),
(19,'en','Client details'),(19,'hy','Հաճախորդի տվյալները'),(19,'ru','Детали клиента'),
(20,'en','Registration date'),(20,'hy','Գրանցման ամսաթիվ'),(20,'ru','Дата регистрации'),
(21,'en','Schedule double visit'),(21,'hy','Պլանավորել կրկնակի այցելություն'),(21,'ru','Запланировать двойное посещение'),
(22,'en','Ongoing works'),(22,'hy','Ընթացիկ աշխատանքներ'),(22,'ru','Текущие работы'),
(23,'en','Clear'),(23,'hy','Ջնջել'),(23,'ru','Очистить'),
(24,'en','Save double visit'),(24,'hy','Պահել կրկնակի այցելությունը'),(24,'ru','Сохранить двойное посещение'),
(25,'en','Close'),(25,'hy','Փակել'),(25,'ru','Закрыть'),
(26,'en','First Name'),(26,'hy','Ազգանուն'),(26,'ru','Имя'),
(27,'en','Last Name'),(27,'hy','Անուն'),(27,'ru','Фамилия'),
(28,'en','Middle name'),(28,'hy','Ազգանուն'),(28,'ru','Отчество'),
(29,'en','Phone'),(29,'hy','Հեռախոս'),(29,'ru','Телефон'),
(30,'en','Password request'),(30,'hy','Գաղտնաբառի հարցում'),(30,'ru','Запрос пароля'),
(31,'en','Myorganizer.dental'),(31,'hy','Myorganizer.dental'),(31,'ru','Myorganizer.dental'),
(32,'en','Enter'),(32,'hy','Մուտքագրել'),(32,'ru','Ввод'),
(33,'en','Armenian (AM)'),(33,'hy','Հայերեն (AM)'),(33,'ru','Армянский (AM)'),
(34,'en','Russian (RU)'),(34,'hy','Ռուսերեն (RU)'),(34,'ru','Русский (RU)'),
(35,'en','English (EN)'),(35,'hy','Անգլերեն (EN)'),(35,'ru','Английский (EN)'),
(36,'en','Clients'),(36,'hy','Հաճախորդներ'),(36,'ru','Клиенты'),
(37,'en','Messages'),(37,'hy','Հաղորդագրություններ'),(37,'ru','Сообщения'),
(38,'en','Senders'),(38,'hy','Ուղարկող'),(38,'ru','Отправители'),
(39,'en','Technicians'),(39,'hy','Տեխնիկներ'),(39,'ru','Техники'),
(40,'en','Minimize'),(40,'hy','Նվազեցնել'),(40,'ru','Свернуть'),
(41,'en','Exit'),(41,'hy','Exit'),(41,'ru','Выход'),
(42,'en','Materials'),(42,'hy','նյութերը։'),(42,'ru','Материально-технические ресурсы'),
(43,'en','Paid amount'),(43,'hy','Վճարված գումարը'),(43,'ru','Выплаченная сумма'),
(44,'en','Transaction date'),(44,'hy','Գործարքի ամսաթիվը'),(44,'ru','Дата операции'),
(45,'en','Full name'),(45,'hy','Ամբողջական անուն'),(45,'ru','Полное имя'),
(46,'en','Edit'),(46,'hy','Փոփոխել'),(46,'ru','Правка'),
(47,'en','View amount'),(47,'hy','Դիտել գումարը'),(47,'ru','Посмотреть сумму'),
(48,'en','Paid amount / month'),(48,'hy','Վճարված գումարը / ամիսը'),(48,'ru','Оплаченная сумма / месяц'),
(49,'en','Patronymic'),(49,'hy','Հայրանուն'),(49,'ru','Отчевство'),
(50,'en','Set price list'),(50,'hy','Սահմանել գնացուցակ'),(50,'ru','Прейскурант'),
(51,'en','Type'),(51,'hy','Տեսակ'),(51,'ru','Тип'),
(52,'en','Tier 1'),(52,'hy','Աստիճան 1'),(52,'ru','Уровень 1'),
(53,'en','Tier 2'),(53,'hy','Մակարդակ 2'),(53,'ru','Уровень 2'),
(54,'en','Tier 3'),(54,'hy','Մակարդակ 3'),(54,'ru','Уровень 3'),
(55,'en','OK'),(55,'hy','ԼԱՎ'),(55,'ru','OK'),
(56,'en','Reminders for today'),(56,'hy','Այսօրվա հիշեցումներ'),(56,'ru','Напоминания на сегодня'),
(57,'en','$'),(57,'hy','֏'),(57,'ru','₽')
");
                // для SQLite хранимую процедуру не создаём
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[usp_L10n_UpsertValue]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_L10n_UpsertValue];

DELETE FROM [L10nValues] WHERE [KeyId] BETWEEN 1 AND 55;
DELETE FROM [L10nKeys]   WHERE [Id]    BETWEEN 1 AND 55;

DELETE FROM [Languages] WHERE [Code] IN ('en','hy','ru');
");
            }
            else if (ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql(@"
DELETE FROM L10nValues WHERE KeyId BETWEEN 1 AND 55;
DELETE FROM L10nKeys   WHERE Id    BETWEEN 1 AND 55;

DELETE FROM Languages WHERE Code IN ('en','hy','ru');
");
            }
        }

    }
}
