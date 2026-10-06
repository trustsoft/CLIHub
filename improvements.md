# Главные архитектурные проблемы

Этот файл является стержнем для последующих архитектурных улучшений и рефакторинга CLIHub. Изменения должны выполняться через OpenSpec, небольшими атомарными change-ами, по одному активному change за раз.

Текущая архитектура уже имеет сильную основу:

- `CLIHub.Core` не зависит от WPF;
- подсистемы Core разделены на `Agents`, `Projects`, `Plugins`, `Configuration`, `Updates` и `Infrastructure`;
- DI-регистрация сгруппирована по подсистемам;
- интерактивный запуск процессов и захват вывода разделены контрактами;
- основные сервисы Core покрыты тестами;
- конфигурация сохраняется через один атомарный persistence path;
- OpenSpec используется как источник плана, критериев и истории изменений.

Основная дальнейшая работа нужна вокруг application/UI orchestration, владения конфигурационным состоянием, расширяемости plugin system и согласованности документации.

## 1. Слишком большой startup orchestration

`App.xaml.cs` одновременно отвечает за:

- single-instance запуск;
- подготовку каталогов;
- настройку логирования;
- сборку DI;
- seed и загрузку плагинов;
- применение preferences;
- tray icon;
- second-instance activation;
- launch window;
- регистрацию hotkey;
- release notes;
- update check;
- скачивание и применение обновлений;
- завершение приложения.

Это затрудняет тестирование жизненного цикла приложения и увеличивает риск побочных эффектов при добавлении новых startup-сценариев.

Целевое направление:

```text
App.xaml.cs
    тонкий WPF entry point

Application startup coordinators
    отдельные сценарии запуска и завершения
```

## 2. `LaunchWindowViewModel` имеет слишком много обязанностей

`LaunchWindowViewModel` управляет:

- проектами;
- агентами;
- фильтрацией;
- версиями;
- кешами обнаружения;
- запуском команд;
- настройками;
- отображением путей;
- действиями меню;
- update control;
- WPF dialogs и MessageBox.

Конструктор ViewModel принимает большое количество сервисов, а сам класс содержит как presentation state, так и application workflow.

Целевое направление:

```text
LaunchWindowViewModel
    состояние окна, selection, команды

Application workflows
    запуск агентов, проекты, обновления

UI adapters
    dialogs, notifications, dispatcher, lifetime
```

## 3. WPF-зависимости проникли в ViewModel и прикладные сценарии

В ViewModel используются `Application.Current`, `MessageBox` и `OpenFolderDialog`. Это делает ViewModel трудно тестируемой без WPF и смешивает UI integration с application logic.

Необходимо ввести узкие UI-контракты:

```text
IProjectDialogService
IUserNotificationService
IApplicationLifetime
```

Реализации этих контрактов остаются в WPF-проекте, а ViewModel зависит только от интерфейсов.

## 4. `TrayIconController` смешивает tray UI и прикладные действия

`TrayIconController` одновременно:

- создаёт `TaskbarIcon`;
- строит контекстное меню;
- выбирает проекты;
- запускает агентов;
- показывает notifications и MessageBox;
- управляет settings, release notes и exit;
- отображает состояние updates.

Целевое направление:

```text
TrayIconController
    lifecycle TaskbarIcon

TrayMenuBuilder
    построение меню и его состояний

TrayActionHandler / application workflows
    выполнение действий
```

## 5. Применение preferences связано с конкретными WPF-объектами

`PreferenceApplier` знает о `GlobalHotkeyService` и `LaunchWindowViewModel`. Это связывает применение настроек с конкретным окном и затрудняет расширение настроек.

Целевое направление:

```text
IPreferencesApplicationService
    применение runtime, hotkey, startup и window preferences

Низкоуровневые appliers
    независимые адаптеры отдельных настроек
```

Нужно сохранять частичный результат операций: например, предыдущий hotkey должен восстанавливаться при неудачной регистрации нового.

## 6. Конфигурация основана на общем изменяемом графе

`ConfigService.Load()` возвращает кешированный изменяемый `AppConfig`. Stores работают поверх этого общего объекта. Сейчас это работает, но создаёт скрытые зависимости:

- потребитель может изменить вложенный объект без явного `Save`;
- preferences и project state используют общий mutable graph;
- усложняется контроль порядка изменений при появлении фоновых операций;
- ownership состояния выражен интерфейсами, но не защищён моделью данных.

Целевое направление:

```text
ConfigurationSnapshot
    snapshot состояния

IConfigurationRepository
    явные операции чтения, изменения и сохранения

ConfigurationWriter
    debounce, atomic write и flush
```

При этом должен сохраниться один файл `config.json` и один atomic write path.

## 7. Отсутствует явная версия схемы конфигурации

Обратная совместимость сейчас основана в основном на default-значениях JSON. Это недостаточно для будущих изменений смысла полей и удаления старых свойств.

Нужно добавить:

```text
schemaVersion
IConfigMigration
ConfigMigrationRunner
```

Legacy-переход `terminalExecutable -> defaultRuntime` должен находиться в migration layer, а не в обычной загрузке модели.

## 8. Plugin loading зависит от неявного порядка каталогов

`PluginManager` читает каталоги через `Directory.GetDirectories`, а duplicate ID разрешается фактически порядком файловой системы. Это не является устойчивой политикой приоритета.

Нужно явно определить:

- детерминированную сортировку;
- поведение duplicate ID;
- приоритет seeded и user plugins;
- provenance plugin.

Также следует отделить:

```text
PluginDescriptorReader
PluginDescriptorValidator
PluginCatalog
Agent command/detection/version services
```

## 9. Реализация процессов всё ещё объединяет несколько механизмов

Контракты уже разделены, но `ProcessLauncher` продолжает объединять:

- interactive launch;
- captured output;
- runtime selection;
- command-line building;
- timeout и termination;
- logging.

Целевое направление:

```text
WindowsInteractiveProcessRunner
WindowsOutputProcessRunner
WindowsCommandLineBuilder
RuntimeSelection
```

Compatibility aggregate interface можно сохранить временно, но новые сервисы должны зависеть от узких контрактов.

## 10. Тестовая структура расходится с документацией

Документация описывает `CLIHub.Tests` как Core-only, однако фактический test project ссылается также на WPF-проект.

Целевая структура:

```text
tests/CLIHub.Core.Tests
    ссылка только на CLIHub.Core

tests/CLIHub.Tests
    WPF/UI-specific tests
```

Либо WPF-ссылка должна быть удалена, если UI-тесты не планируются. Архитектурное правило и фактическая структура должны совпадать.

## 11. Legacy `MainWindow` находится рядом с активным UI

`MainWindow` сохранён как legacy, но его код содержит старый UI workflow и диалоги. Это создаёт риск случайного использования или исправления неактивного пути.

Нужно либо удалить окно после проверки истории, либо переместить его в явно обозначенный `Legacy` каталог.

## 12. Документация требует синхронизации

Нужно привести в соответствие с кодом:

- development version в `docs/repo-structure.md`;
- release instructions в `docs/releasing.md`;
- описание test project dependencies;
- описание активных и legacy окон;
- список фактически зарегистрированных сервисов.

Архитектурную документацию желательно разделить на документы по слоям, оставив `docs/architecture.md` индексом.

# План OpenSpec changes

Каждый пункт ниже является отдельным атомарным change. В каждый момент времени реализуется только один активный change.

Для каждого change используется стандартный цикл:

```text
openspec new change "<change-name>"
создание proposal.md, spec.md, design.md и tasks.md
openspec validate "<change-name>"
реализация через отдельный apply-запрос
dotnet build CLIHub.sln -c Release
dotnet test CLIHub.sln -c Release
openspec archive "<change-name>"
```

## Этап 1. UI orchestration

### 1. `document-application-startup-contract`

Документационный change без изменения runtime-кода.

Зафиксировать:

- порядок startup operations;
- startup failure policy;
- порядок shutdown operations;
- будущие coordinator boundaries.

Зависимости: нет.

### 2. `extract-plugin-initialization`

Вынести seed и load plugins из `App.xaml.cs` в `PluginInitializationService`.

Требования:

- порядок `SeedIfEmpty -> LoadPlugins` сохраняется;
- ошибка seed не блокирует запуск;
- добавлены unit/composition tests.

Зависимость: `document-application-startup-contract`.

### 3. `extract-startup-preferences`

Вынести применение `DefaultRuntime` и `StartWithWindows` в `StartupPreferencesApplier`.

Зависимости: нет.

### 4. `extract-hotkey-startup-registration`

Вынести загрузку, parsing, fallback и регистрацию global hotkey в `HotkeyStartupRegistrar`.

Зависимости: нет.

### 5. `extract-release-notes-startup`

Вынести `ShowReleaseNotesOnce` в `ReleaseNotesStartupCoordinator`.

Покрыть сценарии новой версии, отсутствующих notes, уже показанной версии и ошибки сервиса.

Зависимости: нет.

### 6. `extract-update-startup-check`

Вынести startup update check в `UpdateStartupCoordinator`.

Покрыть enabled, disabled, update available, up-to-date и failed scenarios.

Зависимость: желательно после `extract-startup-preferences`.

### 7. `extract-update-download-workflow`

Вынести download, notifications, menu refresh, delay и apply/restart в `UpdateDownloadCoordinator`.

Зависимость: `extract-update-startup-check`.

### 8. `introduce-ui-dialog-services`

Добавить `IProjectDialogService` и `IUserNotificationService`, убрать прямые WPF dialogs из `LaunchWindowViewModel`.

Зависимости: нет.

### 9. `introduce-application-lifetime-service`

Добавить `IApplicationLifetime` и убрать прямые вызовы `Application.Current.Shutdown()` из ViewModel и tray.

Зависимости: нет.

### 10. `extract-agent-launch-workflow`

Объединить запуск агента из launch window и tray через `IAgentLaunchWorkflow`.

Workflow отвечает за проверку проекта, вызов command service и единый результат.

Зависимость: желательно после `introduce-ui-dialog-services`.

### 11. `separate-tray-menu-building`

Разделить TaskbarIcon lifecycle и построение tray menu через `TrayMenuBuilder`.

Зависимость: `extract-agent-launch-workflow`.

### 12. `split-core-and-ui-test-projects`

Разделить Core tests и WPF/UI tests по разным test projects и обновить solution/CI.

Зависимости: после UI boundary changes.

## Этап 2. Configuration

### 13. `add-configuration-concurrency-tests`

Добавить тесты для нескольких Save, Flush во время записи, worker restart, ошибок записи и сохранения последнего JSON.

Зависимости: нет.

### 14. `introduce-configuration-snapshot`

Добавить `ConfigurationSnapshot` и уменьшить использование общего mutable configuration graph.

Зависимость: `add-configuration-concurrency-tests`.

### 15. `make-preferences-and-project-updates-explicit`

Перевести stores на явные операции Load/Save/Update. Изменения должны проходить через store, а не через случайную мутацию общего объекта.

Зависимость: `introduce-configuration-snapshot`.

### 16. `add-config-schema-version`

Добавить `schemaVersion`, определить текущую версию и правила обработки старого/неизвестного формата.

Зависимость: `introduce-configuration-snapshot`.

### 17. `add-config-migration-runner`

Добавить `IConfigMigration` и `ConfigMigrationRunner`.

Зависимость: `add-config-schema-version`.

### 18. `migrate-legacy-terminal-preference`

Перенести `terminalExecutable -> defaultRuntime` в migration layer.

Зависимость: `add-config-migration-runner`.

### 19. `introduce-configuration-repository`

Ввести единый `IConfigurationRepository`, владеющий snapshot, изменениями, debounce, atomic write и flush.

Зависимости: changes 14–18.

## Этап 3. Plugin system

### 20. `extract-plugin-descriptor-reader`

Вынести поиск, чтение и JSON deserialization `plugin.json` в `IPluginDescriptorReader`.

Зависимости: нет.

### 21. `extract-plugin-descriptor-validator`

Вынести validation rules в отдельный validator и структурированный `PluginValidationResult`.

Зависимость: желательно после `extract-plugin-descriptor-reader`.

### 22. `make-plugin-loading-deterministic`

Определить сортировку директорий, duplicate ID policy и детерминированное логирование.

Зависимость: `extract-plugin-descriptor-validator`.

### 23. `introduce-plugin-catalog-boundary`

Разделить `IPluginCatalog` и agent behavior services. `IPluginManager` можно временно оставить compatibility adapter.

Зависимости: changes 20–22.

### 24. `add-plugin-catalog-reload`

Добавить ручной reload каталога и `PluginsChanged`. File watcher в этот change не входит.

Зависимость: `introduce-plugin-catalog-boundary`.

## Этап 4. Documentation and legacy cleanup

### 25. `synchronize-repository-documentation`

Исправить версии, test dependencies, список активных окон и фактическую структуру solution.

Зависимость: желательно после `split-core-and-ui-test-projects`.

### 26. `isolate-legacy-main-window`

Отложено на отдалённое будущее. Переместить `MainWindow` в `src/CLIHub/Legacy/` либо удалить после отдельной проверки истории и references, когда legacy cleanup снова станет приоритетом.

Зависимости: нет.

### 27. `split-architecture-documentation`

Разделить архитектурную документацию на документы по startup, configuration, plugins, process execution и UI boundaries.

Зависимости: после соответствующих implementation changes.

### 28. `add-architecture-decision-records`

Добавить ADR для Core/UI boundaries, single config document, startup orchestration, plugin precedence и UI dialog boundaries.

Зависимости: после соответствующих changes.

### 29. `add-architecture-ci-checks`

Добавить CI-проверки ссылок проектов, Core/WPF boundaries, test project structure, legacy references и запуска всех test projects.

Зависимости: после `split-core-and-ui-test-projects` и `isolate-legacy-main-window`.

### 30. `add-plugin-origin-metadata`

Добавить внутреннее происхождение plugin: `Seeded`, `User`, `Custom` после появления реального catalog/reload consumer.

Зависимость: `add-plugin-catalog-reload`.

# Рекомендуемая последовательность

```text
1.  document-application-startup-contract
2.  extract-plugin-initialization
3.  extract-startup-preferences
4.  extract-hotkey-startup-registration
5.  extract-release-notes-startup
6.  extract-update-startup-check
7.  extract-update-download-workflow
8.  introduce-ui-dialog-services
9.  introduce-application-lifetime-service
10. extract-agent-launch-workflow
11. separate-tray-menu-building
12. split-core-and-ui-test-projects
13. add-configuration-concurrency-tests
14. introduce-configuration-snapshot
15. make-preferences-and-project-updates-explicit
16. add-config-schema-version
17. add-config-migration-runner
18. migrate-legacy-terminal-preference
19. introduce-configuration-repository
20. extract-plugin-descriptor-reader
21. extract-plugin-descriptor-validator
22. make-plugin-loading-deterministic
23. introduce-plugin-catalog-boundary
24. add-plugin-catalog-reload
25. synchronize-repository-documentation
26. isolate-legacy-main-window
27. split-architecture-documentation
28. add-architecture-decision-records
29. add-architecture-ci-checks
30. add-plugin-origin-metadata
```

# Приоритеты

## Высокий приоритет

- startup orchestration;
- WPF dialogs в ViewModel;
- общий agent launch workflow;
- разделение Core/UI test projects;
- синхронизация документации;
- concurrency tests для configuration persistence.

## Средний приоритет

- configuration snapshot;
- schema version и migrations;
- tray menu separation;
- plugin reader и validator;
- deterministic duplicate policy;
- изоляция `MainWindow` (отдалённое будущее).

## Низкий приоритет

- универсальная TTL cache abstraction;
- plugin file watcher;
- полная immutable-модель конфигурации;
- дальнейшее разбиение `LaunchWindowViewModel` на controllers.

# Следующий change

Первым рекомендуется создать:

```text
document-application-startup-contract
```

Он не изменяет runtime-код и фиксирует baseline для последующих рефакторингов.

Первым implementation change после него должен стать:

```text
extract-plugin-initialization
```

Он небольшой, изолированный и сохраняет текущее пользовательское поведение.

# Отслеживание прогресса (обновляется по мере реализации)

Обновлять эту таблицу после каждого OpenSpec change. Change считается выполненным после реализации, успешных проверок и архивирования через OpenSpec.


| №   | Change                                          | Состояние | Результат / проверка                                                                                                                                                                                                                                                                                                                         |
| ---: | ----------------------------------------------- | --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | `document-application-startup-contract`         | Completed | Startup/shutdown sequence and failure policy added to `docs/architecture.md`; all 5 OpenSpec tasks complete, validation passed, and change archived at `openspec/changes/archive/2026-10-05-document-application-startup-contract`.                                                                                                          |
| 2   | `extract-plugin-initialization`                 | Completed | Added `IPluginInitializationService`, preserved `SeedIfEmpty -> LoadPlugins` ordering, added focused tests, passed OpenSpec validation, Release build, and 340 tests. Archived at `openspec/changes/archive/2026-10-05-extract-plugin-initialization`.                                                                                       |
| 3   | `extract-startup-preferences`                   | Completed | Added `IStartupPreferencesApplier`, preserved runtime-before-startup ordering and the shared preferences snapshot, added focused tests, passed OpenSpec validation, Release build, and 342 tests. Archived at `openspec/changes/archive/2026-10-05-extract-startup-preferences`.                                                             |
| 4   | `extract-hotkey-startup-registration`           | Completed | Added `IHotkeyStartupRegistrar` and the global registration seam, preserved valid parsing and default fallback, added focused tests, passed OpenSpec validation, Release build, and 345 tests. Archived at `openspec/changes/archive/2026-10-05-extract-hotkey-startup-registration`.                                                        |
| 5   | `extract-release-notes-startup`                 | Completed | Added `IReleaseNotesStartupCoordinator`, preserved Show/RecordOnly/Skip behavior and warning-only failures, added focused tests, passed OpenSpec validation, Release build, and 350 tests. Archived at `openspec/changes/archive/2026-10-05-extract-release-notes-startup`.                                                                  |
| 6   | `extract-update-startup-check`                  | Completed | Added `IUpdateStartupCoordinator`, preserved disabled/non-blocking/available-version behavior and dispatcher callback, added focused tests, passed OpenSpec validation, Release build, and 357 tests. Archived at `openspec/changes/archive/2026-10-05-extract-update-startup-check`.                                                        |
| 7   | `extract-update-download-workflow`              | Completed | Added shared `IUpdateDownloadCoordinator` and dispatcher-aware notifier, routed tray and What's New through one workflow, preserved result handling/delay/apply behavior, added focused tests, passed OpenSpec validation, Release build, and 364 tests. Archived at `openspec/changes/archive/2026-10-05-extract-update-download-workflow`. |
| 8   | `introduce-ui-dialog-services`                  | Completed | Added project dialog and user notification services, removed direct WPF dialog calls from `LaunchWindowViewModel`, preserved modal behavior, added focused tests, passed OpenSpec validation, Release build, and 365 tests. Archived at `openspec/changes/archive/2026-10-05-introduce-ui-dialog-services`.                                  |
| 9   | `introduce-application-lifetime-service`        | Completed | Added `IApplicationLifetime` and WPF adapter, replaced direct shutdown calls in the active ViewModel and tray, added focused tests, passed OpenSpec validation, Release build, and 366 tests. Archived at `openspec/changes/archive/2026-10-05-introduce-application-lifetime-service`.                                                      |
| 10  | `extract-agent-launch-workflow`                 | Completed | Added shared `IAgentCommandWorkflow`, routed active launch-window and tray agent commands through it, preserved result presentation and Core validation, added focused tests, passed OpenSpec validation, Release build, and 367 tests. Archived at `openspec/changes/archive/2026-10-05-extract-agent-launch-workflow`.                     |
| 11  | `separate-tray-menu-building`                   | Completed | Added singleton `TrayMenuBuilder`, separated menu composition from tray icon lifecycle, passed Release build, 368 tests, and OpenSpec validation. Archived at `openspec/changes/archive/2026-10-05-separate-tray-menu-building`.                                                                                                             |
| 12  | `split-core-and-ui-test-projects`               | Completed | Split Core-only and WPF/application tests into separate projects, updated solution, CI, and repository documentation, passed Release build and 368 tests (332 Core, 36 UI). Archived at `openspec/changes/archive/2026-10-05-split-core-and-ui-test-projects`.                                                                               |
| 13  | `add-configuration-concurrency-tests`           | Completed | Added configuration concurrency, flush, worker-exit, and write-failure coverage with an internal test hook, passed Release build, 372 tests, and OpenSpec validation. Archived at `openspec/changes/archive/2026-10-05-add-configuration-concurrency-tests`.                                                                                 |
| 14  | `introduce-configuration-snapshot`              | Completed | Added detached `ConfigurationSnapshot`, migrated config service, stores, and test fakes, passed Release build and 374 tests; synced the new durable spec. Archived at `openspec/changes/archive/2026-10-05-introduce-configuration-snapshot`.                                                                                                |
| 15  | `make-preferences-and-project-updates-explicit` | Completed | Replaced store Save methods with explicit Update callbacks, migrated callers, added callback-failure tests, passed Release build, and synced the durable spec. Archived at `openspec/changes/archive/2026-10-05-make-preferences-and-project-updates-explicit`.                                                                              |
| 16  | `add-config-schema-version`                     | Completed | Added schema version 1 and legacy version 0 handling, future/invalid-version load fallback, passed Release build and 380 tests; synced the durable spec. Archived at `openspec/changes/archive/2026-10-06-add-config-schema-version`.                                                                                                        |
| 17  | `add-config-migration-runner`                   | Completed | Added migration contracts, runner, config-service integration, and Core DI registration, passed Release build and 385 tests (349 Core, 36 UI); synced the durable spec. Archived at `openspec/changes/archive/2026-10-06-add-config-migration-runner`.                                                                                       |
| 18  | `migrate-legacy-terminal-preference`            | Completed | Added schema 0 → 1 terminal preference migration, mapped legacy executable values to runtime tokens, passed Release build and 395 tests (359 Core, 36 UI); synced the durable spec. Archived at `openspec/changes/archive/2026-10-06-migrate-legacy-terminal-preference`.                                                                    |
| 19  | `introduce-configuration-repository`            | Completed | Replaced `IConfigService`/`ConfigService` with `IConfigurationRepository`, serialized latest-snapshot updates, preserved migration and atomic persistence behavior, passed Release build and 397 tests (361 Core, 36 UI); synced the durable spec. Archived at `openspec/changes/archive/2026-10-06-introduce-configuration-repository`.     |
| 20  | `extract-plugin-descriptor-reader`               | Completed | Added `IPluginDescriptorReader` and descriptor read results, moved `plugin.json` lookup/read/deserialization out of `PluginManager`, preserved validation and loading behavior, passed Release build and 401 tests (365 Core, 36 UI). Archived at `openspec/changes/archive/2026-10-06-extract-plugin-descriptor-reader`. |
| 21  | `extract-plugin-descriptor-validator`            | Completed | Added `IPluginDescriptorValidator` and structured validation results, preserved ID/name/launch rules and warning behavior, passed Release build and 406 tests (370 Core, 36 UI). Archived at `openspec/changes/archive/2026-10-06-extract-plugin-descriptor-validator`. |
| 22  | `make-plugin-loading-deterministic`              | Completed | Added deterministic plugin directory ordering, stable duplicate-ID selection, and winner/skipped-directory diagnostics, passed Release build and 408 tests (372 Core, 36 UI), and synced the durable spec. Archived at `openspec/changes/archive/2026-10-06-make-plugin-loading-deterministic`. |
| 23  | `introduce-plugin-catalog-boundary`              | Completed | Added `IPluginCatalog`/`PluginCatalog`, migrated startup and UI consumers, preserved `IPluginManager` as a shared-instance compatibility adapter, passed Release build and 409 tests (373 Core, 36 UI). Archived at `openspec/changes/archive/2026-10-06-introduce-plugin-catalog-boundary`. |
| 24  | `add-plugin-catalog-reload`                     | Completed | Added synchronous catalog reload and `PluginsChanged` notification, preserved initial-load behavior, passed Release build and 411 tests (375 Core, 36 UI), and synced the durable spec. Archived at `openspec/changes/archive/2026-10-06-add-plugin-catalog-reload`. |
| 25  | `synchronize-repository-documentation`          | Completed | Synchronized README, agent guidance, architecture, repository structure, and release runbook with the `0.9.0` solution state, current plugin/configuration boundaries, test projects, and DI registrations; passed Release build and 411 tests (375 Core, 36 UI). Archived at `openspec/changes/archive/2026-10-06-synchronize-repository-documentation`. |
| 27  | `split-architecture-documentation`              | Completed | Split `docs/architecture.md` into topical documents under `docs/architecture/` (`startup`, `configuration`, `plugins`, `processes`, `ui`) with the entry point keeping overview content and one-line links; updated glossary and README links; passed OpenSpec validation, Release build, and 411 tests (375 Core, 36 UI). Archived at `openspec/changes/archive/2026-10-06-split-architecture-documentation`. |


Остальные changes выполняются последовательно согласно разделу «Рекомендуемая последовательность» и добавляются в таблицу по мере перехода в работу. Завершено: 26 из 30 (позиция 26 `isolate-legacy-main-window` отложена на отдалённое будущее и в таблицу не входит). Активного change сейчас нет. Следующий change: `add-architecture-decision-records`.
