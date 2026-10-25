# Порядок работы в GitHub

Текущий архив: **этап 3/4, Kurmashev Damir**. Ожидается 51 успешных тестов.

## Как устроены архивы

Четыре ZIP — последовательные полные снимки **одного проекта**, а не четыре разных проекта.
Архив 01 содержит Assignment 6 и первый модуль Assignment 7; 02 содержит 01 + второй модуль;
03 содержит 02 + третий модуль; 04 содержит все четыре модуля и полный запуск Assignment 7.
Не загружайте архив 04 до того, как участники завершили этапы 01, 02 и 03.

Во всех ZIP верхняя папка называется `BoardGameRental`. В репозиторий копируется её
**содержимое**: `src`, `tests`, `docs`, `.vscode`, README и файлы проекта. Сам ZIP в GitHub
загружать не нужно. В корне репозитория должен лежать `BoardGameRental.sln`, а не вложенная
папка `BoardGameRental/BoardGameRental`.

## Перед первым этапом

Используйте один общий репозиторий и одну согласованную ветку. Владелец даёт остальным
троим доступ на запись. У каждого свой аккаунт и своя локальная копия репозитория.
Нужны .NET 8 SDK и Git. Если репозиторий уже содержит Assignment 6, работайте с ним.
Если репозиторий новый и пустой, первый участник копирует в него содержимое архива 01.
Для скачивания репозитория используйте его настоящий адрес в команде `git clone`.

Проверьте, что имя и email Git настроены на **ваши собственные** данные:

```powershell
git config user.name
git config user.email
```

## Перед каждым из четырёх коммитов

1. Дождитесь, пока предыдущий участник отправит свой этап в общий репозиторий.
2. В своей папке репозитория выполните `git status`, затем `git pull --ff-only`.
   Для самого первого коммита пустого репозитория `git pull` пропускается.
3. Распакуйте **свой** ZIP отдельно. Скопируйте содержимое папки `BoardGameRental` в
   корень своей локальной копии репозитория, согласившись на замену файлов.
   Скрытую папку `.vscode` тоже скопируйте. Существующую `.git` оставьте на месте.
4. Откройте свои файлы из `docs/STAGE_CHANGES.md`, проверьте логику и свои тесты.
   При необходимости внесите изменения. Если предыдущий участник менял код сверх
   подготовленного снимка, перенесите эти изменения перед заменой соответствующих файлов.
5. Из папки с `BoardGameRental.sln` выполните команды ниже.
6. Убедитесь, что сборка и все тесты прошли. Просмотрите `git diff`, затем выполните
   команды коммита своего этапа. Дождитесь успешного `git push` и передайте очередь следующему.

```powershell
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
git diff
```

Если сборщик в ограниченной среде не запускает фоновые процессы, тот же проект проверяется так:

```powershell
dotnet build BoardGameRental.sln -m:1 -p:UseSharedCompilation=false -nr:false --disable-build-servers
dotnet run --project tests/BoardGameRental.Tests --no-build
```

`bin` и `obj` исключены через `.gitignore`. Архивы не содержат `.git`, историю коммитов
или локальные настройки аккаунта. Коммит своего этапа делает сам участник после проверки.

### Этап 1: Issen Daiana

Архив: `01_Issen_Daiana_Assignment7.zip`. После применения: **40/40 тестов**.

Задача: Success/Failure, Map, Bind, Match; ошибки количества, остатка, отсутствующей игры; повторное использование фабрики задания 6.
Тесты: `tests/BoardGameRental.Tests/ResultFactoryTests.cs`.
Объяснить на защите: Result и фабрика.

```powershell
git add .
git diff --cached --stat
git commit -m "Add Result composition and validated rental outcomes"
git push -u origin HEAD
```
### Этап 2: Kabdrakhmanova Zarina

Архив: `02_Kabdrakhmanova_Zarina_Assignment7.zip`. После применения: **45/45 тестов**.

Задача: Some/None; поиск игры и цены; преобразование None в Result; проверка названия тарифа.
Тесты: `tests/BoardGameRental.Tests/OptionTariffTests.cs`.
Объяснить на защите: Отсутствующие значения и тарифы.

```powershell
git add .
git diff --cached --stat
git commit -m "Add explicit optional lookups and tariff outcomes"
git push -u origin HEAD
```
### Этап 3: Kurmashev Damir

Архив: `03_Kurmashev_Damir_Assignment7.zip`. После применения: **51/51 тестов**.

Задача: Цепочка поиска, подготовки и подтверждения; Map/Bind; ошибки порядка операций; повторное подтверждение.
Тесты: `tests/BoardGameRental.Tests/ResultWorkflowTests.cs`.
Объяснить на защите: Переходы состояний и композиция.

```powershell
git add .
git diff --cached --stat
git commit -m "Compose safe booking transitions with Map and Bind"
git push -u origin HEAD
```
### Этап 4: Nurlanbek Marghulan

Архив: `04_Nurlanbek_Marghulan_Assignment7.zip`. После применения: **55/55 тестов**.

Задача: JSON и файловые ошибки; консольные команды; один обработчик результата; соединение всех модулей.
Тесты: `tests/BoardGameRental.Tests/BoundaryOutcomeTests.cs`.
Объяснить на защите: Граница ввода-вывода и запуск.

```powershell
git add .
git diff --cached --stat
git commit -m "Integrate JSON boundary handling and Assignment 7 console"
git push -u origin HEAD
```

## После четвёртого этапа

У всех участников выполните `git pull --ff-only`, затем:

```powershell
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
dotnet run --project src/BoardGameRental.App -- --demo7
```

Итог: **55/55 тестов**, доступны прежние задания 5–6 и новая цепочка задания 7.
Для сдачи используется итоговый проект из архива 04 или тот же проверенный снимок общего
репозитория без `bin`, `obj`, `.git` и IDE-кэша. Вклад каждого участника указан в README.
