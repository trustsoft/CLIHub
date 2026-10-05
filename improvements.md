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

### 23. `add-plugin-origin-metadata`

Добавить внутреннее происхождение plugin: `Seeded`, `User`, `Custom`.

Зависимость: `make-plugin-loading-deterministic`.

### 24. `introduce-plugin-catalog-boundary`

Разделить `IPluginCatalog` и agent behavior services. `IPluginManager` можно временно оставить compatibility adapter.

Зависимости: changes 20–23.

### 25. `add-plugin-catalog-reload`

Добавить ручной reload каталога и `PluginsChanged`. File watcher в этот change не входит.

Зависимость: `introduce-plugin-catalog-boundary`.

## Этап 4. Documentation and legacy cleanup

### 26. `synchronize-repository-documentation`

Исправить версии, test dependencies, список активных окон и фактическую структуру solution.

Зависимость: желательно после `split-core-and-ui-test-projects`.

### 27. `isolate-legacy-main-window`

Переместить `MainWindow` в `src/CLIHub/Legacy/` либо удалить после проверки истории и references.

Зависимости: нет.

### 28. `split-architecture-documentation`

Разделить архитектурную документацию на документы по startup, configuration, plugins, process execution и UI boundaries.

Зависимости: после соответствующих implementation changes.

### 29. `add-architecture-decision-records`

Добавить ADR для Core/UI boundaries, single config document, startup orchestration, plugin precedence и UI dialog boundaries.

Зависимости: после соответствующих changes.

### 30. `add-architecture-ci-checks`

Добавить CI-проверки ссылок проектов, Core/WPF boundaries, test project structure, legacy references и запуска всех test projects.

Зависимости: после `split-core-and-ui-test-projects` и `isolate-legacy-main-window`.

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
23. add-plugin-origin-metadata
24. introduce-plugin-catalog-boundary
25. add-plugin-catalog-reload
26. synchronize-repository-documentation
27. isolate-legacy-main-window
28. split-architecture-documentation
29. add-architecture-decision-records
30. add-architecture-ci-checks
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
- изоляция `MainWindow`.

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

# Отслеживание прогресса

Обновлять эту таблицу после каждого OpenSpec change. Change считается выполненным после реализации, успешных проверок и архивирования через OpenSpec.

| № | Change | Состояние | Результат / проверка |
|---:|---|---|---|
| 1 | `document-application-startup-contract` | Completed | Startup/shutdown sequence and failure policy added to `docs/architecture.md`; all 5 OpenSpec tasks complete, validation passed, and change archived at `openspec/changes/archive/2026-10-05-document-application-startup-contract`. |
| 2 | `extract-plugin-initialization` | Completed | Added `IPluginInitializationService`, preserved `SeedIfEmpty -> LoadPlugins` ordering, added focused tests, passed OpenSpec validation, Release build, and 340 tests. Archived at `openspec/changes/archive/2026-10-05-extract-plugin-initialization`. |
| 3 | `extract-startup-preferences` | Completed | Added `IStartupPreferencesApplier`, preserved runtime-before-startup ordering and the shared preferences snapshot, added focused tests, passed OpenSpec validation, Release build, and 342 tests. Archived at `openspec/changes/archive/2026-10-05-extract-startup-preferences`. |
| 4 | `extract-hotkey-startup-registration` | Completed | Added `IHotkeyStartupRegistrar` and the global registration seam, preserved valid parsing and default fallback, added focused tests, passed OpenSpec validation, Release build, and 345 tests. Archived at `openspec/changes/archive/2026-10-05-extract-hotkey-startup-registration`. |
| 5 | `extract-release-notes-startup` | Completed | Added `IReleaseNotesStartupCoordinator`, preserved Show/RecordOnly/Skip behavior and warning-only failures, added focused tests, passed OpenSpec validation, Release build, and 350 tests. Archived at `openspec/changes/archive/2026-10-05-extract-release-notes-startup`. |

Остальные changes выполняются последовательно согласно разделу «Рекомендуемая последовательность» и добавляются в таблицу по мере перехода в работу. Завершено: 5 из 30. Активного change сейчас нет. Следующий change: `extract-update-startup-check`.
