# Этап 2/4 — Kabdrakhmanova Zarina, IT-2511

Задача: Some/None; поиск игры и цены; преобразование None в Result; проверка названия тарифа.

## Новые файлы этого этапа

- `src/BoardGameRental.Core/Option.cs`
- `src/BoardGameRental.Core/RentalTariffOutcomes.cs`
- `tests/BoardGameRental.Tests/OptionTariffTests.cs`

## Обновляемые файлы

- `tests/BoardGameRental.Tests/Program.cs`: подключить тесты этого участника, сохранив предыдущие.
- `README.md`: состояние этапа, карта требований, ожидаемые проверки.
- `GITHUB_STEPS_RU.md`: порядок GitHub и ожидаемое число тестов этого этапа.
- `docs/TEST_RESULTS.txt`, `docs/EXPECTED_OUTPUT.txt`: фактические результаты проверки.
- `docs/STAGE_CHANGES.md`: этот список изменений.

## Проверка перед коммитом

```powershell
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
```

Ожидается **45/45 passed; 0 failed**.

## Коммит этого этапа

```powershell
git add .
git diff --cached --stat
git commit -m "Add explicit optional lookups and tariff outcomes"
git push -u origin HEAD
```

Сначала примените предыдущие этапы. Скопируйте содержимое BoardGameRental в корень
репозитория, сохранив существующую .git. Полная инструкция: GITHUB_STEPS_RU.md.
