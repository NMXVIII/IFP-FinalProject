# BoardGameRental — Assignments 5, 6 и 7

Аренда настольных игр: подбор доступных игр по бюджету и безопасное подтверждение аренды.
Пользователи: клиент и сотрудник проката. Данные берутся из JSON-каталога.
Подтверждение работает с неизменяемым снимком: проект не списывает остатки и не выполняет оплату.

**Снимок 2/4: Kabdrakhmanova Zarina — Отсутствующие значения и тарифы.**
Включены все предыдущие этапы. Полный консольный сценарий Assignment 7 появляется в снимке 04.
Функциональность и все 34 теста Assignments 5–6 сохранены в каждом архиве.

## Установка и проверка

Нужен **.NET 8 SDK**; проверено с **8.0.425**, Windows, **2026-10-22**.
Внешние NuGet-пакеты не нужны. Команды выполняются из папки с `BoardGameRental.sln`.

```powershell
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
```

Ожидаемый результат этого снимка: **45/45 passed; 0 failed**.
Проверен архив после чистой распаковки: сборка без предупреждений и ошибок.
Точные команды и результаты проверки: `docs/TEST_RESULTS.txt`.
Тесты используют консольный runner с утверждениями: запускайте через `dotnet run`, а не `dotnet test`.
Код выхода тестов: 0 при успехе, 1 при любой ошибке.

В Windows можно запустить `test.bat` и `run.bat`. В VS Code открывайте всю папку проекта;
кнопка ▶ для `.bat` использует **PowerShell**. Перед командой стоит безопасный разделитель `;`,
чтобы потеря первого символа при отправке команды не превращала `cmd.exe` в `md.exe`.

## Команда и распределение задач

| Порядок | Участник | Группа | Задача Assignment 7 | Основные файлы и тесты | Статус в этом снимке |
|---|---|---|---|---|---|---|
| 1 | Issen Daiana | IT-2511 | Success/Failure, Map, Bind, Match; ошибки количества, остатка, отсутствующей игры; повторное использование фабрики задания 6 | `Result.cs; ValidatedRentalOutcomes.cs`; `ResultFactoryTests.cs` | Включён |
| 2 | Kabdrakhmanova Zarina | IT-2511 | Some/None; поиск игры и цены; преобразование None в Result; проверка названия тарифа | `Option.cs; RentalTariffOutcomes.cs`; `OptionTariffTests.cs` | Включён |
| 3 | Kurmashev Damir | IT-2511 | Цепочка поиска, подготовки и подтверждения; Map/Bind; ошибки порядка операций; повторное подтверждение | `RentalOutcomes.cs`; `ResultWorkflowTests.cs` | Следующий этап |
| 4 | Nurlanbek Marghulan | IT-2511 | JSON и файловые ошибки; консольные команды; один обработчик результата; соединение всех модулей | `CatalogBoundary.cs; ExceptionConsole.cs; Program.cs; demo7.bat`; `BoundaryOutcomeTests.cs` | Следующий этап |

Все основные файлы находятся в `src/BoardGameRental.Core/`, тесты —
в `tests/BoardGameRental.Tests/`. Консольные файлы четвёртого этапа находятся в
`src/BoardGameRental.App/`. Каждый участник отвечает также за объяснение своего модуля,
своих тестов и соответствующего раздела документации.

Порядок копирования файлов, коммитов и передачи работы: **[GITHUB_STEPS_RU.md](GITHUB_STEPS_RU.md)**.
Изменения именно этого этапа: **[docs/STAGE_CHANGES.md](docs/STAGE_CHANGES.md)**.

## Сохранённый Assignment 5

```powershell
dotnet run --project src/BoardGameRental.App
dotnet run --project src/BoardGameRental.App -- sample-data/games.json 3 3000 both
```

В `games.json` шесть записей. Для 3 дней и бюджета 3000 KZT:
Standard даёт ID 4, 2, 1 с ценами 0, 1500, 3000; Student — 4, 2, 1, 6 с ценами
0, 1200, 2400, 3000. ID 5 без остатка отсекается. ID 4 бесплатный, это допустимое значение.
Каждая рекомендация проверяется на бюджет отдельно. Некорректный каталог отклоняется
целиком до расчётов; `[]` является корректным пустым набором.

| Чистый этап | Вход -> выход | Правило и пример |
|---|---|---|
| SelectAvailableGames | игры -> доступные игры | Остаток > 0; Risk с остатком 0 исключается |
| CalculateQuotes | игры, дни, функция цены -> предложения | Вызывает переданный делегат: Uno за 3 дня = 1500 Standard либо 1200 Student |
| BuildRecommendations | предложения, бюджет -> упорядоченные рекомендации | Цена <= бюджета; сортировка по цене, затем ID; Carcassonne при 3000 проходит только по Student |

`RentalPipeline.Run` соединяет все три этапа. Политика выбирается за пределами чистого
ядра и передаётся аргументом. Этапы не меняют исходные коллекции, не читают Console/файлы
и не зависят от скрытых настроек. Исходная документация и тесты: `docs/assignment5/`.

## Сохранённый Assignment 6

```powershell
dotnet run --project src/BoardGameRental.App -- --demo
dotnet run --project src/BoardGameRental.App -- --book sample-data/games.json 2 3 1 3000 student
```

Uno подтверждается за 1200.00 KZT. `ValidatedRental` имеет приватный конструктор,
а `TryCreate` проверяет все правила до создания значения.
Состояния закрыты: Draft содержит исходный запрос, Ready и Confirmed — ValidatedRental.
Prepare принимает Draft; повтор Prepare отклоняется. Confirm требует Ready;
повтор Confirm для Confirmed возвращает тот же объект. Неуспешные старые операции
возвращают исходное состояние без изменения. Пример скрытой зависимости до исправления,
безопасный вызов и таблица переходов сохранены в `docs/assignment6/README.md`.

## Модель Assignment 7

Эта секция описывает общий итог четырёх этапов; таблица команды выше показывает,
какие модули уже включены в текущий архив.

`Result<T>` содержит только Success с непустым обязательным payload либо Failure с AppError.
Ошибка несёт Code и Message. Value доступен лишь в Success, Error — лишь в Failure.
`Option<T>` содержит Some(value) или None. Реальный поиск игры — `CatalogLookup.Find`.
Отсутствующий обязательный результат превращается через ToResult в GameNotFound.
Поиск цены возвращает Some(0) для бесплатной игры и None для отсутствующей.
Значения 0, false и пустая строка сами по себе не означают отсутствие.

`RentalState` обозначает этап аренды; `Result` — успех или отказ одной операции.
Ошибка валидации не является новым состоянием аренды. Старый WorkflowResult сохранён
для совместимости задания 6. Новая подготовка вызывает `ValidatedRentalOutcomes.Create`
и лишь после успеха создаёт Ready. Создание ValidatedRental по-прежнему защищено фабрикой.

Правила: существующая игра; положительный ID; непустые имя/категория; цена 0..1 000 000
с максимум двумя десятичными знаками; остаток >=0; дни 1..30; количество >0 и <= остатка;
бюджет 0..1 000 000 000 с максимум двумя знаками; тариф Standard/Student; итог <= бюджета.
Дубликаты ID отклоняются при загрузке. Student округляет стоимость одной копии до двух
знаков от нуля на середине, затем умножает на количество. Точные границы принимаются.

| Операция / правило | Успешный результат | Ошибка и её контекст |
|---|---|---|
| Найти обязательную игру | Найденная игра | GameNotFound: отсутствующий ID |
| Проверить количество | Положительное количество | InvalidQuantity: исходное количество |
| Проверить остаток | Количество не больше остатка | InsufficientStock: ID, запрошенное и доступное количество |
| Проверить прочие правила фабрики | ValidatedRental с рассчитанной ценой | ValidationRejected: причина, например превышение бюджета |
| Найти тариф | Существующий нормализованный тариф | UnknownTariff: исходное название; допустимы standard/student |
| Подготовить / подтвердить | Draft -> Ready -> Confirmed | InvalidState: текущее состояние и нужная предпосылка |
| Загрузить JSON | Полностью проверенный read-only каталог | FileMissing, MalformedJson, InvalidCatalog, AccessDenied, FileReadFailed, InvalidPath: операция и путь |

Ожидаемые предметные отказы возвращаются без throw/catch. Исключения неправильного
использования программного API (например, попытка создать Success с null) не маскируются
под бизнес-успех. Обработка файловых и JSON-исключений находится в CatalogBoundary.Load,
за пределами чистых функций. Возвращается либо весь корректный каталог, либо ошибка;
частичный каталог не публикуется. Скрытого fallback нет, файлы никогда не перезаписываются.

Цепочка `RentalOutcomes.Book`:

```text
Find -> ToResult -> Map(Draft) -> Bind(Prepare) -> Bind(Confirm) -> Map(строка результата)
```

Map принимает `T -> U` и оборачивает новое значение в Result. Bind принимает
`T -> Result<U>` и возвращает Result следующей операции без дополнительного вложения.
Оба метода при Failure сохраняют ошибку и не вызывают переданную функцию.
Итоговая консольная цепочка: Load -> Bind(Parse tariff -> Bind(Book)) -> один Print/Match.

## Карта требований Assignment 7

| Требование | Этап / реализация | Проверки |
|---|---|---|
| Task 1: явные предметные исходы | 1: Result; ValidatedRentalOutcomes.Create; 3: RentalOutcomes | A7ValidatedFactorySuccess/Failures; A7BookingDomainFailures |
| Task 2: отсутствующие значения | 2: Option; CatalogLookup; RentalTariffOutcomes | A7LookupPresentAbsent; A7PrimitiveValuesPresent; A7MissingBecomesFailure |
| Task 3: Map и Bind в реальной цепочке | 1: Result.Map/Bind; 3: RentalOutcomes.Book; 4: ExceptionConsole.Run | A7MapSuccess; A7BindSuccess; A7CompleteBookingChain |
| Пропуск зависимых функций | 1 и 3: передача Failure | A7FailureSkipsFunctions; A7MissingLookupSkipsPreparation — счётчик равен 0 |
| Task 4: файловая граница | 4: CatalogBoundary.Load | A7MissingFilePreservesState; A7MalformedFilePreservesState; A7InvalidCatalogAndEmptyFile |
| Task 5: интеграция и сохранение 5–6 | 4: ExceptionConsole.Run/Demo; старые команды | A7CompleteFileToBookingChain; все 34 старых теста; --demo7 |
| Устойчивые переходы и нулевая цена | 3: Prepare/Confirm | A7StatesAndRepeatedConfirmation; A7BookingPreservesInput; A7FreeBooking |

## Тесты текущего снимка

34 теста прежних заданий плюс 11 новых. Каждый тест использует проверки значений,
при несовпадении завершается ошибкой. Полный фактический список: `docs/TEST_RESULTS.txt`.
В новых файловых тестах используются уникальные временные папки с очисткой в finally;
входные fixtures находятся в `sample-data/`.

| Участник | Тест | Ожидаемая проверка |
|---|---|---|
| Issen Daiana | A7MapSuccess | 7 передаётся в Map, результат 21 |
| Issen Daiana | A7BindSuccess | Bind получает 7 и возвращает тот же Result следующей операции |
| Issen Daiana | A7FailureSkipsFunctions | После Failure счётчик Map/Bind равен 0, объект ошибки сохранён |
| Issen Daiana | A7RejectsNullPayload | Success с null не создаётся |
| Issen Daiana | A7ValidatedFactorySuccess | Uno: 3 дня, 1 копия, student => 1200 KZT; вход не изменён |
| Issen Daiana | A7ValidatedFactoryFailures | GameNotFound, InvalidQuantity, InsufficientStock, ValidationRejected; правила дней, бюджета и тарифа действуют |
| Kabdrakhmanova Zarina | A7LookupPresentAbsent | Uno находится, ID 999 возвращает None |
| Kabdrakhmanova Zarina | A7PrimitiveValuesPresent | Цена 0, false и пустая строка являются присутствующими значениями |
| Kabdrakhmanova Zarina | A7MissingBecomesFailure | None становится GameNotFound, Some становится Success |
| Kabdrakhmanova Zarina | A7TariffLookupSuccess | STANDARD с пробелами и Student нормализуются; цена student 1200 |
| Kabdrakhmanova Zarina | A7TariffLookupFailure | null, пустая строка, both и vip дают UnknownTariff |

## Состояние разработки

В этом архиве приложения заданий 5–6 работают как раньше. Новые модули текущего этапа
проверяются автоматическими тестами. Команды `--demo7` и `--book7`, файловая граница
и полный показ ошибок добавляются четвёртым участником в архиве 04.
