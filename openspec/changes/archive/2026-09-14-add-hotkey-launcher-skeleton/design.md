## Context

См. `proposal.md` — Why. В `docs/vision.md` уже зафиксированы платформенные и
архитектурные решения: .NET 10 LTS, WPF, Windows-only, H.NotifyIcon, `RegisterHotKey`
через P/Invoke, ручной composition root, MVVM-lite, слои Core (без WPF) + тонкий UI.
Кода ещё нет — это первое изменение, поэтому здесь фиксируется структура решения.

Ограничения, формирующие подход:

- Core SHALL не зависеть от WPF, чтобы вся логика покрывалась тестами без UI.
- Пассивный режим попапа (клик по трею, глобальный mouse hook) в это изменение не входит.
- Обнаружение агентов (probe/detect) не входит, поэтому манифест для скелета
  ограничен теми полями, которые реально используются.

## Goals / Non-Goals

**Goals:**

- Тонкий end-to-end путь: трей -> hotkey -> попап -> запуск агента, реально запускаемый.
- Разложение по сборкам так, чтобы доменная логика тестировалась в изоляции от WPF.
- Проверяемый контракт `agent.json`, на который смогут опереться последующие изменения.

**Non-Goals:**

- Не вводим внешний DI-контейнер — только ручной composition root в `App.xaml.cs`.
- Не вводим абстракции «на будущее» — только швы, нужные скелету.
- Не реализуем `update`/`init` как UI-действия попапа — скелет запускает `run`.

## Decisions

### D1. Две сборки: `CLIHub.Core` + `CLIHub.App`

Core — net10.0 без WPF, вся логика и контракты. App — net10.0-windows/WPF, тонкий
UI и весь платформенный интероп.

- **Почему:** прямое следствие vision «Core чистый, всё остальное тонкое».
- **Альтернатива — третья сборка `CLIHub.Windows` для interop:** отклонена; на этом
  этапе даёт плюс проект и glue без практической пользы.
- **Альтернатива — interop внутри Core:** отклонена; Core потеряет чистоту и
  тестируемость без UI-рантайма.

### D2. Структура репозитория

```
CLIHub 2/                       репо-корень
|
+-- CLIHub.sln
+-- src/
|   +-- CLIHub.Core/            net10.0, без WPF
|   |     Models/  Abstractions/  Services/
|   +-- CLIHub.App/             net10.0-windows, WPF
|         App.xaml(.cs)  Views/  ViewModels/  Tray/  Interop/  Assets/
+-- tests/
|   +-- CLIHub.Core.Tests/      xUnit
+-- plugins/
|   +-- agents/
|       +-- <id>/  agent.json + ассеты
+-- docs/                       vision.md, ui.md
+-- openspec/                   config.yaml, specs/, changes/
```

Сервисы Core: `ConfigStore`, `PluginLoader`, `AgentDetector`, `LauncherCore`
(`AgentDetector` и `UpdateService` в этом изменении не реализуются, но структура
оставляет им место).

### D3. Ручной composition root

Все сервисы создаются и связываются в `App.xaml.cs`. Состояние приложения живёт до
завершения процесса; `Application.ShutdownMode` — `OnExplicitShutdown`, чтобы трей
жил без окон.

- **Почему:** vision фиксирует ручной DI; число сервисов пока невелико.
- **Альтернатива — `Microsoft.Extensions.DependencyInjection`:** отложена до роста
  числа сервисов (вне scope скелета).

### D4. Швы тестируемости в Core

`IFileSystem`, `IProcessRunner`, `IClock`, `IPathProvider` — интерфейсы в
`CLIHub.Core/Abstractions`, реализации — в App/тестах.

- **Почему:** `PluginLoader` и `ConfigStore` должны тестироваться на временных
  каталогах, а `LauncherCore` — без реального запуска процессов.
- **Trade-off:** `IFileSystem` — тонкая обёртка над `System.IO`; для скелета
  оправдана, но её стоит держать минимальной, чтобы не дублировать BCL.

### D5. Контракт `agent.json` (v1, для скелета)

```json
{
  "schemaVersion": 1,
  "id": "claude",
  "name": "Claude Code",
  "actions": {
    "run":     { "command": "claude" },
    "resume":  { "command": "claude --continue" },
    "init":    { "command": "claude init" },
    "update":  { "command": "claude update", "runtime": "ps" },
    "version": { "command": "claude --version" }
  }
}
```

- `id` авторитетен, имя папки — конвенция; неизвестные поля игнорируются.
- `command` — shell-агностичная строка (исполняемый файл и аргументы).
- `actions.<key>.runtime` — необязательный per-action override.
- Поля `detect` (host/project) и использование `version` как probe относятся к
  будущему изменению про детекцию и здесь не разбираются.

### D6. Корень плагинов

Плагины лежат в `plugins/agents/` в корне репо; `CLIHub.App.csproj` включает их как
content с копированием в output. `PluginLoader` ищет агентов в
`<AppContext.BaseDirectory>/plugins/agents/`.

- **Почему:** единый источник в репо и попадание в бандл Velopack одним механизмом.
- **Альтернатива — `src/CLIHub.App/plugins/`:** отклонена; прячет общий контент
  внутри UI-проекта.

### D7. Платформенный интероп

`CLIHub.App/Interop`: `RegisterHotKey` (user32, P/Invoke), `GetCursorPos`,
DPI-конвертация через `PresentationSource.CompositionTarget.TransformFromDevice`.
`UseWindowsForms` включается ради `Screen`/`Cursor`.

### D8. Жизненный цикл попапа

Один экземпляр окна на весь процесс; создаётся на старте скрытым. Показ — переключение
`ShowActivated` перед `Show()`; закрытие — `Hide()`, а не `Close()`.

### D9. Runtime и форма запуска

Выбор runtime: per-action override -> `config.runtime` -> встроенный дефолт. Построение
командной строки — по таблице из спеки `agent-launch`; при отсутствии `wt` — откат на
`ps`/`cmd` с предупреждением. Запуск — `Process.Start` с `WorkingDirectory` = папка
проекта, без ожидания завершения.

## Risks / Trade-offs

- [В одном процессе WPF и WinForms] -> `UseWindowsForms=true`; следить за коллизиями
  типов (`Screen`/`Cursor` из `System.Windows.Forms`), не смешивать моделями WPF.
- [Математика DPI/clamp хрупка] -> вынести расчёт позиции в чистую функцию, проверять
  вручную на экранах с масштабированием 100%/150%/200%.
- [Приложение без открытых окон может завершиться] -> `ShutdownMode=OnExplicitShutdown`
  и скрытое окно-синглтон на старте.
- [Hotkey может быть занят] -> регистрация может не удаться; показать предупреждение
  и работать без hotkey (см. спеку `global-hotkey`).
- [Зависимость от .NET Desktop Runtime] -> framework-dependent развёртывание, runtime
  как pre-requisite инсталлятора (см. `docs/vision.md`).

## Migration Plan

Не применимо: первый код в проекте, переносимых данных или состояния нет.

## Open Questions

- Где живёт `UpdateService` (Core за интерфейсом `IAppUpdater` или App) — решается в
  изменении про обновление, когда появится Velopack.
- Реализация `AgentDetector` и поля `detect` в манифесте — отдельное изменение.
- Резолв логотипов и UI-состав окна настроек — отдельные изменения.
