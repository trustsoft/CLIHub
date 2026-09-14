# AGENTS.md

CLIHub — Windows-компаньон (system-tray + hotkey-попап) для запуска и обновления
AI Agents CLI в папке проекта. .NET 10, WPF + WinForms, Windows-only.

Перед работой над поведением читай `docs/vision.md` (источник истины, все
зафиксированные решения) и `docs/ui.md` (состав UI).

## Команды

- Сборка: `dotnet build CLIHub.sln`
- Все тесты: `dotnet test CLIHub.sln`
- Один тест: `dotnet test tests/CLIHub.Core.Tests --filter FullyQualifiedName~PluginLoaderTests`
- Запуск приложения: `dotnet run --project src/CLIHub.App`
- OpenSpec: `openspec list`, `openspec status --change "<name>" --json`, `openspec validate --specs`

## Workflow — use OpenSpec, not ad-hoc code
Work flows through the OpenSpec commands and their matching skills in `.opencode/`:
- `/opsx-explore` — think through capabilities before formalizing them
- `/opsx-propose` — formalize a feature into a change (proposal + spec deltas + tasks)
- `/opsx-apply` — implement an approved change
- `/opsx-archive` — archive a completed change

Do not write application code without an approved change/proposal.

## Artifact conventions
- Write all OpenSpec artifacts (proposals, specs, design, tasks) in **English**. Authoritative source: `openspec/config.yaml`.
- The `context` block in `openspec/config.yaml` is injected into every `openspec instructions` call — keep it compressed and do not add un-reviewed capability sketches there.

## Структура и границы

- `src/CLIHub.Core` — net10.0, БЕЗ WPF. Вся логика и контракты, покрывается тестами.
- `src/CLIHub.App` — net10.0-windows, WPF + WinForms. Тонкий UI и платформенный
  интероп (P/Invoke, tray, hotkey). Composition root — `App.xaml.cs`.
- `tests/CLIHub.Core.Tests` — xUnit, ссылается только на Core; UI и интероп
  проверяются вручную.
- `plugins/agents/<id>/agent.json` — встроенные плагины. `id` из JSON авторитетен,
  имя папки — конвенция. Копируются в output через `Content`-glob в `CLIHub.App.csproj`.

## Подводные камни

- `CLIHub.sln` — классический формат. .NET 10 по умолчанию создаёт `.slnx`; solution
  создавай через `dotnet new sln --format sln`.
- WindowsDesktop SDK при `UseWindowsForms`: `System.Windows.Forms` и `System.Drawing`
  неявно подключаются (ломают однозначность типов WPF), а `System.IO` — НЕТ. В
  `CLIHub.App.csproj` это исправлено блоком `<Using Remove/Include>`; не удаляй его.
- Корневого `.gitignore` нет: `bin/`/`obj/` не игнорируются. Не коммить артефакты
  сборки, стейджить файлы выборочно.
- Конфиг приложения — `%AppData%\CLIHub\config.json` (не в репо). Дефолтный hotkey
  `Ctrl+Alt+Space`; запись в конфиг пока не реализована.
- При запуске окна нет: приложение живёт в трее (иконка + «Выход»), попап открывается
  по hotkey. `dotnet run` не завершается сам — запускай фоном, иначе терминал занят.
- `.opencode/` и `.kilocode/` содержат OpenSpec-воркфлоу (skills/commands); их
  `node_modules` игнорируется через `.opencode/.gitignore`.

## OpenSpec

- Схема `spec-driven`: proposal -> specs -> design -> tasks.
- Не создавай папки изменений вручную — только `openspec new change "<name>"`.
- Delta-спеки сливаются в `openspec/specs/<capability-path>/spec.md`; main-спеки не
  содержат заголовков `## ADDED/MODIFIED/REMOVED/RENAMED Requirements`.
