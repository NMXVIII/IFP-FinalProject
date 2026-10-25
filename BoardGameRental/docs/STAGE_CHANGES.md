# Этап 3/4 — Kurmashev Damir, IT-2511

Задача: Цепочка поиска, подготовки и подтверждения; Map/Bind; ошибки порядка операций; повторное подтверждение.

## Новые файлы этого этапа

- `src/BoardGameRental.Core/RentalOutcomes.cs`
- `tests/BoardGameRental.Tests/ResultWorkflowTests.cs`

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

Ожидается **51/51 passed; 0 failed**.

## Коммит этого этапа

```powershell
git add .
git diff --cached --stat
git commit -m "Compose safe booking transitions with Map and Bind"
git push -u origin HEAD
```

Сначала примените предыдущие этапы. Скопируйте содержимое BoardGameRental в корень
репозитория, сохранив существующую .git. Полная инструкция: GITHUB_STEPS_RU.md.
