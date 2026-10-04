# CLIHub — предложения по оптимизации

Дата: 2026-10-04
Статусы исполнения проверены по коду: 2026-10-04 (после внедрения logo-cache, фоновой записи конфига, Core boundary refactor и runtime stabilization).
Область анализа: `src/CLIHub.Core` (сервисы, модели, интерфейсы), `src/CLIHub` (ViewModels, View-слой — кроме MainWindow, исключён из рассмотрения).

Легенда: ✅ реализовано · 🟡 частично · ❌ не реализовано

---

## Приоритет 1 — файловый I/O на UI-потоке (2/3)

**Логотипы сканируются диском при каждом запросе.**
`ProjectService.GetAllProjects/GetRecentProjects/GetFavorites` вызывают `RefreshLogos()`, а `GetCurrentProject()` — `ResolveLogo` (`src/CLIHub.Core/Services/ProjectService.cs:40-91`, `:197-214`). Это до 7×`File.Exists` на проект на каждый вызов. Цепочка при клике на проект:

```
SelectedProject.set → SetCurrentProject → Persist (синхронная запись JSON)
  → ProjectsChanged → RefreshProjects (логотипы заново) + RefreshAgents
```

Предложения:

- ✅ **Кэшировать резолв логотипов** — реализовано: `LogoCacheService` (`ILogoCacheService.GetOrResolve`, ключи `project:<id>`/`plugin:<id>`), кэширует и негативные результаты, персистентно хранится в `%APPDATA%\CLIHub\cache\logos.json`, атомарно сохраняется при `Dispose`, сброс по `Remove`/`InvalidateAll` (`ProjectService.RemoveProject` чистит запись). Покрыто `LogoCacheServiceTests`, `ProjectServiceTests.GetAllProjects_AfterServiceRestart_ReuseCachedLogoWithoutRescan`. Спека: `openspec/specs/logo-cache`.
- ✅ **Вынести запись конфига из UI-потока** — реализовано: `ConfigService.Save` сериализует на вызывающем потоке (защита от гонок по общему графу конфига) и передаёт готовый JSON фоновому воркеру (`EnsureWorker`/`_pendingJson` под `_gate`), есть `Flush()` для записи при выходе.
- ✅ **Не пересобирать списки на каждый Persist** — `RefreshProjects` и `RefreshAgents` синхронизируют строки по стабильным идентификаторам и сохраняют объекты и выбор при обновлении.

---

## Приоритет 2 — cache stampede в AgentVersionService (2/2)

`GetVersionAsync` (`src/CLIHub.Core/Services/AgentVersionService.cs:49-80`): на промахе кэша каждый вызывающий запускает свой процесс. `PopulateVersionsAsync` фан-аутится по всем агентам, а `RefreshAgents` вызывается после каждой команды, смены проекта и т.д. При быстрой смене проектов — дублирующиеся процессы `--version`.

- ✅ Кэшировать **in-flight `Task<string?>`** вместо результата — реализовано: параллельные запросы одного агента разделяют один probe task, TTL-кэш готового результата сохранён (`src/CLIHub.Core/Agents/AgentVersionService.cs`).
- ✅ `PopulateVersionsAsync` защищён generation + CTS: устаревший проход отменяется, а его результаты не применяются к текущему списку (`src/CLIHub/ViewModels/LaunchWindowViewModel.cs`, retained `MainWindow`).

---

## Приоритет 3 — корректность ProcessLauncher (3/3)

`WindowsCommandLineBuilder` (`src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs`) теперь является общей точкой построения команд:

- ✅ Interactive Windows Terminal, Command Prompt и PowerShell paths построены через общий builder; PowerShell использует encoded command.
- ✅ Executable paths, working directories, embedded quotes и cmd metacharacters покрыты regression tests.
- ✅ Captured execution продолжает поддерживать `.cmd`/`.bat` shims и process timeout; добавлены tests для shell shim paths.

---

## Приоритет 4 — UpdateService: необработанные исключения (1/1)

`CheckForUpdateCoreAsync` (`src/CLIHub.Core/Services/UpdateService.cs:211-248`) использует `Task.WhenAny(checkTask, Task.Delay(...))`: проигравший таск продолжает жить, и если `checkTask` упадёт после таймаута — exception станет unobserved. Плюс таймер `Task.Delay` не диспозится.

Фикс: `await checkTask.WaitAsync(timeoutCts.Token)` + отдельная observe-continuation, либо передавать `CancellationToken` в `manager.CheckForUpdatesAsync` (Velopack его поддерживает) — тогда таймаут реально отменяет сетевой вызов.

- ✅ Исправлено: timeout/cancellation теперь явно наблюдает late check task, очищает доступную версию и оставляет сервис готовым к следующей проверке. Velopack в используемой версии не предоставляет cancellation token для `CheckForUpdatesAsync`, поэтому применяется observation continuation (`src/CLIHub.Core/Updates/UpdateService.cs`).

---

## Приоритет 5 — архитектура (4/4)

- ✅ **Дублирование домена в IConfigService** — реализовано: интерфейс сужён до `Load`/`Save`/`Flush`; `GetCurrentProject`/`SetCurrentProject` живут в `ProjectService` (коммит `refactor(core): slim the core api surface`).
- ✅ **PluginManager.GetAllPlugins** — реализовано: интерфейс и реализация возвращают `IReadOnlyList<Plugin>` (внутренний `_plugins` отдаётся напрямую без копии — приемлемо, мутация снаружи типом не выражена).
- ✅ **`CreatePlaceholderLogo`** — реализовано иным путём: метода в `PluginManager` больше нет; побочные записи файлов в папку плагина выполняет `PluginSeeder`, лого резолвится через `ILogoCacheService` (`LoadPluginLogo`).
- ✅ **`JsonSerializerOptions` дублируются** — реализовано: общий `CoreJson.Options` (`src/CLIHub.Core/Services/CoreJson.cs`), используется в `ConfigService`, `LogoCacheService`, `PluginManager`.

---

## Мелочи (1/3)

- 🟡 `_ = PopulateVersionsAsync(...)` защищён; другие fire-and-forget команды (`ExecuteAsync`, update control и settings actions) ещё требуют отдельного общего error-boundary решения.
- ✅ `RefreshProjects/RefreshAgents`: синхронизация по стабильным идентификаторам сохраняет строки и ограничивает CollectionChanged membership/order changes.
- ✅ `App.OnStartup` каждый запуск перезаписывает Run-ключ реестра — оставлено (самолечение при смене пути); зафиксировано как осознанный трейд-офф.

---

## Общая оценка

Ядро (`CLIHub.Core`) чистое: тесты покрывают почти все сервисы, DI аккуратный, MVVM соблюдён. Самые ощутимые для пользователя пункты — приоритеты 1 и 2 (отзывчивость окна), 3 и 4 — надёжность.

### Сводка исполнения (по коду на 2026-10-04)

| Метрика | Значение |
|---|---|
| Всего пунктов | 16 |
| ✅ Реализовано | 14 (87.5%) |
| 🟡 Частично | 1 (М1) |
| ❌ Осталось | 1 (М1) |
| Содержательная работа | 1 пункт: М1 |

Готовность по приоритетам: П1 — 2/3, П2 — 2/2, П3 — 3/3, П4 — 1/1, П5 — 4/4, мелочи — 1/3.

Порядок внедрения оставшегося: общий error boundary для fire-and-forget операций.
