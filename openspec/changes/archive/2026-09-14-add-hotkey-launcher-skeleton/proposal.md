## Why

Видение CLIHub уже детально зафиксировано в `docs/vision.md`, но не проверено кодом.
Самые хрупкие решения — не логика Core, а интероп: глобальный hotkey, жизненный цикл
окна-попапа, DPI-конвертация, clamp по `WorkingArea`. Дизайн-документ их подтвердить
не может, только запущенное приложение. Нужен walking skeleton, который проходит весь
путь end-to-end и снижает этот риск до того, как начнётся разработка «в ширину».

## What Changes

- Появляется решение (.NET 10, WPF) со слоями Core (без WPF) и тонким UI.
- Минимальный хост в трее: иконка и пункт «Выход» (без passive-попапа).
- Глобальный hotkey (по умолчанию `Ctrl+Alt+Space`) открывает попап.
- Попап в hotkey-режиме: warm singleton, показ у курсора, clamp, DPI, закрытие
  по Esc/потере фокуса, `Hide`, а не `Close`.
- `PluginLoader` читает реальные манифесты `plugins/agents/<id>/agent.json`.
- `LauncherCore` запускает агента в папке проекта в runtime cmd/ps/wt
  (fire-and-forget, окно остаётся открытым).
- **Не входит в это изменение:** passive-режим попапа по клику на трей, probe/detection
  (`AgentDetector`), обновление CLIHub (Velopack), окно настроек, CRUD проектов,
  резолв логотипов.

## Capabilities

### New Capabilities
- `global-hotkey`: регистрация глобального hotkey и реакция на него.
- `popup`: поверхность попапа — показ по hotkey, позиционирование у курсора,
  clamp/DPI, закрытие, warm singleton; плюс минимальный хост в трее.
- `agent-plugins`: загрузка манифестов агентов `plugins/agents/<id>/agent.json`
  и контракт манифеста.
- `agent-launch`: запуск агента в папке проекта в выбранном runtime.

### Modified Capabilities

Отсутствуют — это первое изменение, durable-спек пока нет.

## Impact

- Новые проекты: `CLIHub.Core` (net10.0, без WPF), `CLIHub.App` (net10.0-windows,
  WPF), `CLIHub.Core.Tests`.
- Новый контракт: формат `agent.json` и папка `plugins/agents/`.
- Чтение `%AppData%\CLIHub\config.json` (только чтение, подмножество полей).
- Внешние зависимости: H.NotifyIcon (трей), Velopack не затрагивается,
  `RegisterHotKey`/`GetCursorPos` через P/Invoke.
- Макет `CLIHub 2 UI` не меняется (производный артефакт, односторонняя синхронизация).
