# Этап 1/4 — Issen Daiana, IT-2511

Задача: Success/Failure, Map, Bind, Match; ошибки количества, остатка, отсутствующей игры; повторное использование фабрики задания 6.

## Новые файлы этого этапа

- `src/BoardGameRental.Core/Result.cs`
- `src/BoardGameRental.Core/ValidatedRentalOutcomes.cs`
- `tests/BoardGameRental.Tests/ResultFactoryTests.cs`

## Обновляемые файлы

- `tests/BoardGameRental.Tests/Program.cs`: подключить тесты этого участника, сохранив предыдущие.
- `README.md`: состояние этапа, карта требований, ожидаемые проверки.
- `GITHUB_STEPS_RU.md`: порядок GitHub и ожидаемое число тестов этого этапа.
- `docs/TEST_RESULTS.txt`, `docs/EXPECTED_OUTPUT.txt`: фактические результаты проверки.
- `docs/STAGE_CHANGES.md`: этот список изменений.
- `.vscode/settings.json`: прежний исправленный запуск .bat через PowerShell.
- `.gitignore`: исключить сборочные папки и ZIP.
- `docs/assignment6/`: сохранить прежнюю документацию задания 6 рядом с заданием 5.

## Проверка перед коммитом

```powershell
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
```

Ожидается **40/40 passed; 0 failed**.

## Коммит этого этапа

```powershell
git add .
git diff --cached --stat
git commit -m "Add Result composition and validated rental outcomes"
git push -u origin HEAD
```

Сначала примените предыдущие этапы. Скопируйте содержимое BoardGameRental в корень
репозитория, сохранив существующую .git. Полная инструкция: GITHUB_STEPS_RU.md.
