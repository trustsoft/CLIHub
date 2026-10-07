# CLIHub: архитектурные улучшения и порядок работ

Этот файл является стержнем последующих архитектурных изменений CLIHub. Он фиксирует текущую оценку, целевое направление, порядок рефакторинга и критерии завершения. Новые изменения должны сверяться с этим документом.

## Правила работы

- Работать по одному активному OpenSpec change.
- Перед реализацией фиксировать границы, зависимости и критерии готовности в change-артефактах.
- Сохранять пользовательское поведение, формат `config.json`, один atomic write path и Windows-only модель приложения.
- Не выполнять массовый rewrite или миграцию на сложную архитектуру без конкретной проблемы, которую это решает.
- После каждого change запускать `dotnet build CLIHub.sln -c Release`, `dotnet test CLIHub.sln -c Release` и `openspec validate`.
- После успешной реализации архивировать change и обновлять раздел прогресса этого файла.
- Не переписывать пользовательские незакоммиченные изменения в документации или UI-артефактах.

## Baseline

Состояние на 2026-10-07:

- `CLIHub.Core` не зависит от WPF и содержит основные модели, сервисы, persistence boundaries и Windows-инфраструктуру.
- `CLIHub` содержит WPF presentation layer, tray, hotkey, dialogs, launch/settings/release-notes windows и composition root.
- DI-регистрация сгруппирована по подсистемам.
- Конфигурация использует `ConfigurationSnapshot`, `IConfigurationRepository`, миграции, debounce и atomic write.
- Плагинная система разделена на reader, validator и `IPluginCatalog`, поддерживает deterministic loading и reload.
- Startup-сценарии уже частично вынесены в отдельные coordinators.
- Core-процессы разделены на interactive launch и captured output contracts.
- Проверка `dotnet build CLIHub.sln --no-restore -c Release`: 0 предупреждений, 0 ошибок.
- Проверка `dotnet test CLIHub.sln -c Release`: 437 успешных тестов, из них 376 Core и 61 application/UI.

### Уже выполнено и не должно планироваться повторно

Следующие направления уже реализованы и находятся в архиве OpenSpec:

- plugin initialization;
- startup preferences;
- hotkey startup registration;
- release-notes startup coordinator;
- startup update check;
- update download workflow;
- UI dialog services;
- application lifetime boundary;
- shared agent command workflow;
- tray menu separation;
- разделение Core и application/UI test projects;
- configuration concurrency tests;
- configuration snapshot и explicit store updates;
- schema version и config migrations;
- configuration repository;
- plugin descriptor reader и validator;
- deterministic plugin loading;
- plugin catalog boundary и reload;
- синхронизация документации, topical architecture docs и ADR.

## Главные текущие проблемы

### 1. Startup orchestration и ownership ресурсов

`App.OnStartup` всё ещё управляет single-instance, каталогами, logging, DI, плагинами, preferences, tray, hotkey, release notes и updates. Это усложняет тестирование порядка запуска и делает `App` главным application service.

Кроме того, `SingleInstanceGuard` создаётся напрямую в `App`, но одновременно зарегистрирован в DI. В результате есть две потенциальные модели владения одним ресурсом, хотя фактически используется только ручной экземпляр.

Направление:

```text
Program
  Velopack bootstrap

App
  WPF lifecycle adapter

ApplicationBootstrapper
  startup ordering, failure policy, cancellation, shutdown

Infrastructure services
  single instance, logging, persistence, tray, hotkey
```

### 2. Неконтролируемые фоновые операции

Startup update check, update download и часть команд ViewModel запускаются через fire-and-forget. У приложения нет общего cancellation lifecycle и согласованного ожидания фоновых операций при завершении.

Нужно определить:

- какие операции можно отменить;
- какие операции должны завершиться перед shutdown;
- как логируются исключения и отмена;
- кто владеет `CancellationTokenSource` приложения.

### 3. Слишком большой `LaunchWindowViewModel`

ViewModel содержит состояние окна, проекты, агентов, фильтрацию, версии, кэширование, команды, настройки, действия меню, notifications, dialogs, lifetime и update control. Конструктор принимает большое количество сервисов.

Направление:

```text
LaunchWindowViewModel
  observable state, selection, commands, presentation messages

ProjectPaneController
  project list, selection, favorite/remove/add

AgentPaneController
  available agents, filtering, versions, refresh

AgentCommandWorkflow
  validation, execution, unified result

WPF adapters
  dialogs, notifications, dispatcher, window lifetime
```

Цель не в механическом дроблении класса, а в том, чтобы workflow можно было тестировать без создания WPF-окна.

### 4. Сильная связь application-кода с конкретными WPF-объектами

`PreferenceApplier` напрямую зависит от `GlobalHotkeyService` и `LaunchWindowViewModel`. `TrayMenuBuilder` зависит от `LaunchWindow`, а `GlobalHotkeyService` создаётся вокруг конкретного WPF `Window`.

Уже существующие интерфейсы `IProjectDialogService`, `IUserNotificationService` и `IApplicationLifetime` нужно продолжить в том же направлении. Application-код должен зависеть от портов, а WPF-классы - реализовывать эти порты.

### 5. `CLIHub.Core` объединяет несколько архитектурных ролей

В одном проекте находятся domain models, application services, JSON persistence, filesystem, process execution, Windows Registry, named pipes и Velopack. Отсутствие WPF-зависимости полезно, но `Core` не является чистым domain/application слоем.

Целевое логическое разделение:

```text
Domain
  models, value objects, parsing, pure rules

Application
  use cases, workflows, application ports

Infrastructure
  JSON, filesystem, processes, Windows, Velopack

WPF
  views, ViewModels, tray, dialogs, composition root
```

Сначала границы следует установить namespace и dependency rules. Отдельные проекты `CLIHub.Application` и `CLIHub.Infrastructure` выделять только после стабилизации этих границ.

### 6. Слишком широкий update contract

`IUpdateService` одновременно предоставляет current version, check, download, apply/restart, mutable download state и event notifications.

Целевое разделение:

- `IAppVersionProvider`;
- `IUpdateChecker`;
- `IUpdateDownloader`;
- `IUpdateInstaller`;
- отдельный state/notification adapter для UI.

Это позволит тестировать update check отдельно от скачивания и перезапуска приложения.

### 7. Конфигурационная модель требует дальнейшего укрепления

`AppConfig`, `AppConfigDocument`, `ConfigurationSnapshot` и `ProjectState` дублируют части структуры и используют ручное копирование. Это повышает риск забыть новое поле в одном из mappings.

Следующее направление:

- оставить persistence DTO отдельно от runtime state;
- свести mappings в один явно тестируемый слой;
- постепенно заменить строковые runtime settings на типизированные значения;
- сохранить текущий `config.json` и обратную совместимость;
- не разрешать потребителям изменять общий конфигурационный граф обходным путём.

### 8. Legacy и compatibility code

- `MainWindow` больше не является активным окном, но остаётся рядом с рабочим UI.
- `PluginManager` является compatibility adapter над `PluginCatalog`.
- `IProcessLauncher` сохраняется как aggregate interface, хотя новые сервисы уже могут зависеть от узких контрактов.

После проверки references эти элементы нужно либо удалить, либо переместить в явно обозначенный `Legacy`/`Compatibility` слой с зафиксированным сроком удаления.

### 9. Недостаточная защита application/UI orchestration тестами

Core имеет хорошую тестовую базу. Прямых тестов недостаточно для `LaunchWindowViewModel`, `SettingsViewModel`, `PreferenceApplier`, `TrayIconController` и полного startup/shutdown flow.

Нужно добавлять тесты не ради покрытия строк, а для контрактов:

- порядок startup и shutdown;
- single ownership ресурсов;
- сохранение, отмена и валидация настроек;
- отмена фоновых операций;
- реакция UI на изменение проектов и плагинов;
- восстановление предыдущего hotkey после ошибки регистрации.

## Целевая архитектурная форма

```text
CLIHub WPF
  App / Program
  Views / ViewModels
  Tray / Hotkey / Dialog adapters
  Composition root
          |
          v
Application layer
  Startup and shutdown
  Project workflows
  Agent workflows
  Settings application
  Update workflows
  Application ports
          |
          v
Core contracts and rules
  Models
  Plugin rules
  Agent command rules
  Configuration state
          ^
          |
Infrastructure adapters
  JSON and filesystem
  Process execution
  Windows Registry / pipes / hotkey host
  Velopack
```

Основные правила целевой формы:

1. Только composition root знает конкретные реализации всех слоёв.
2. ViewModel не создаёт application service и не обращается к `Application.Current` напрямую.
3. Application layer не зависит от WPF types.
4. Infrastructure не зависит от ViewModel и окон.
5. UI получает typed results и state notifications, а не управляет persistence и process details.
6. Каждый singleton имеет одного владельца и явный lifecycle.

## Порядок работ

### Этап 1. Lifecycle и startup ownership

#### 1. `unify-single-instance-ownership`

Убрать двойную регистрацию/создание `SingleInstanceGuard` и определить единый lifecycle.

Критерии:

- создаётся ровно один guard;
- второй экземпляр по-прежнему активирует первый;
- guard освобождается на каждом пути shutdown;
- есть composition test на registration и ownership.

#### 2. `extract-application-bootstrapper`

Вынести startup ordering из `App.OnStartup` в `ApplicationBootstrapper` или `StartupCoordinator`.

Критерии:

- `App` содержит только WPF-specific lifecycle glue;
- порядок действий зафиксирован тестом;
- failure policy явно разделена на fatal и best-effort операции;
- текущий пользовательский startup flow не меняется.

#### 3. `add-application-operation-lifetime`

Ввести общий cancellation/lifetime boundary для startup checks, update downloads и async command workflows.

Критерии:

- фоновые операции получают cancellation token;
- завершение приложения не оставляет неконтролируемые tasks;
- исключения не теряются в fire-and-forget вызовах;
- shutdown ожидает только операции, которые действительно требуют ожидания.

### Этап 2. Application/UI boundaries

#### 4. `split-launch-window-workflows`

Разделить `LaunchWindowViewModel` на state/presentation часть и отдельные project/agent workflows.

Критерии:

- application workflow тестируется без WPF window;
- ViewModel не владеет process, cache и persistence details;
- refresh, selection и command results имеют явные контракты;
- поведение selection identity и async version population сохраняется.

#### 5. `extract-settings-draft-and-application`

Вынести draft, validation и сохранение настроек из `SettingsViewModel` в типизированный settings application service.

Критерии:

- Cancel не изменяет сохранённые настройки;
- Save атомарно применяет валидный draft;
- частично применяемые системные настройки имеют rollback/error policy;
- добавлены прямые тесты ViewModel/application service.

#### 6. `decouple-runtime-preference-application`

Заменить зависимости `PreferenceApplier` на конкретные WPF classes узкими интерфейсами и state notifications.

Критерии:

- application code не зависит от `LaunchWindowViewModel`;
- runtime, hotkey, startup и display preferences применяются независимо;
- ошибка hotkey сохраняет предыдущую комбинацию;
- прямые unit-тесты покрывают все ветки applier.

#### 7. `separate-tray-actions-from-tray-host`

Оставить `TrayIconController` владельцем `TaskbarIcon`, а действия и menu state перенести в application-facing handlers.

Критерии:

- tray host не содержит бизнес-правил запуска агента;
- menu builder получает готовые состояния и commands;
- tray actions используют те же workflows, что launch window;
- refresh и disposal проверяются тестами.

### Этап 3. Contracts и infrastructure

#### 8. `split-update-service-contracts`

Разделить check, download, install/restart и current version contracts. Velopack оставить за infrastructure adapter.

Критерии:

- update check можно тестировать без download/apply;
- UI не знает деталей Velopack;
- state changes приходят через отдельный application/UI adapter;
- повторный download и failure paths сохраняют текущую семантику.

#### 9. `harden-process-boundaries`

Сохранить узкие process contracts и убрать новые зависимости от aggregate `IProcessLauncher`. Проверить cancellation, timeout и termination semantics.

Критерии:

- interactive и output runners остаются независимыми;
- cancellation не только прекращает ожидание, но и имеет определённое поведение для процесса;
- command-line building тестируется отдельно;
- runtime selection не смешан с UI/application logic.

#### 10. `harden-configuration-boundary`

Разделить внутренние state, persistence DTO, mapping и writer implementation без изменения внешнего формата файла.

Критерии:

- один владелец mutable configuration state;
- mapping покрывает все поля;
- concurrent updates и Flush сохраняют latest valid snapshot;
- schema migrations и atomic write остаются совместимыми.

#### 11. `introduce-typed-runtime-settings`

Оставить строки только на JSON boundary и использовать typed values внутри application/infrastructure contracts.

Критерии:

- неизвестные значения безопасно переходят в defaults;
- миграции старых значений остаются обратимо тестируемыми;
- runtime services не сравнивают строковые tokens вручную.

### Этап 4. Plugin и legacy cleanup

#### 12. `define-plugin-reload-consumer`

Определить реального consumer для `PluginsChanged`, обновить agent list и связанные caches после reload. File watcher в этот change не включать.

Критерии:

- reload приводит к предсказуемому обновлению UI;
- исчезнувшие plugins удаляются из selection safely;
- version/detection caches не возвращают данные для устаревшего descriptor.

#### 13. `retire-compatibility-adapters`

Удалить или изолировать `PluginManager`, aggregate process interface и прочие compatibility boundaries после проверки references.

Критерии:

- production code использует canonical contracts;
- legacy API имеет явный owner и removal decision;
- тесты не поддерживают устаревший путь без необходимости.

#### 14. `remove-or-isolate-legacy-main-window`

После проверки истории и references удалить `MainWindow` либо переместить его в `src/CLIHub/Legacy/`.

Критерии:

- активный UI не имеет двусмысленного legacy entry point;
- документация и solution отражают только поддерживаемый путь;
- старый workflow не участвует в production composition.

#### 15. `add-architecture-enforcement`

Зафиксировать dependency rules в CI и тестах.

Проверять:

- Core не ссылается на WPF;
- application namespaces не ссылаются на `System.Windows`;
- composition root является единственным местом concrete wiring;
- все test projects запускаются в CI;
- legacy references не появляются снова.

## Definition of Done для каждого этапа

Change считается завершённым только когда:

1. Поведение описано в OpenSpec и не противоречит этому файлу.
2. Архитектурная граница отражена в коде, DI и тестах.
3. Есть тесты для нового контракта и failure paths.
4. `dotnet build CLIHub.sln -c Release` проходит без предупреждений.
5. `dotnet test CLIHub.sln -c Release` проходит.
6. Документация и ADR обновлены, если изменилось архитектурное решение.
7. Change архивирован, а прогресс ниже обновлён.

## Отслеживание прогресса

| № | Change | Состояние | Комментарий |
|---:|---|---|---|
| 1 | `unify-single-instance-ownership` | Completed | Guard зарегистрирован и разрешается как один DI singleton; provider владеет его disposal. Change archived at `openspec/changes/archive/2026-10-07-unify-single-instance-ownership`. |
| 2 | `extract-application-bootstrapper` | Completed | Startup orchestration вынесена в `ApplicationBootstrapper`; добавлены application-facing UI/lifecycle ports и regression tests. Change archived at `openspec/changes/archive/2026-10-07-extract-application-bootstrapper`. |
| 3 | `add-application-operation-lifetime` | Completed | Общий application operation lifetime, cancellation tokens и bounded shutdown для startup/update/command workflows. Change archived at `openspec/changes/archive/2026-10-07-add-application-operation-lifetime`. |
| 4 | `split-launch-window-workflows` | Completed | Project и agent workflows вынесены в pane controllers; ViewModel оставлена presentation facade. Change archived at `openspec/changes/archive/2026-10-07-split-launch-window-workflows`. |
| 5 | `extract-settings-draft-and-application` | Completed | Typed draft, validation, ordered application и rollback policy вынесены в Settings application service. Change archived at `openspec/changes/archive/2026-10-07-extract-settings-draft-and-application`. |
| 6 | `decouple-runtime-preference-application` | Completed | Убраны concrete WPF dependencies из applier через узкие runtime/display/hotkey ports; добавлены прямые tests. Change archived at `openspec/changes/archive/2026-10-08-decouple-runtime-preference-application`. |
| 7 | `separate-tray-actions-from-tray-host` | Completed | Tray host оставлен владельцем TaskbarIcon, application state/actions вынесены в TrayActions и narrow host ports. Change archived at `openspec/changes/archive/2026-10-08-separate-tray-actions-from-tray-host`. |
| 8 | `split-update-service-contracts` | Completed | Check, version, state, download и installer contracts разделены; Velopack adapter сохранён, consumers используют narrow ports. Change archived at `openspec/changes/archive/2026-10-08-split-update-service-contracts`. |
| 9 | `harden-process-boundaries` | Completed | Cancellation/timeout termination semantics уточнены, interactive/output contracts сохранены, command builder покрыт отдельными tests. Change archived at `openspec/changes/archive/2026-10-08-harden-process-boundaries`. |
| 10 | `harden-configuration-boundary` | Completed | Snapshot стал единственным mutable state owner, добавлены explicit DTO mapping и full-field round-trip tests; migrations/writer сохранены. Change archived at `openspec/changes/archive/2026-10-08-harden-configuration-boundary`. |
| 11 | `introduce-typed-runtime-settings` | Completed | `RuntimeKind` используется внутри preferences/runtime contracts, tokens оставлены только в DTO/migration boundary; unknown values безопасно defaulted. Change archived at `openspec/changes/archive/2026-10-08-introduce-typed-runtime-settings`. |
| 12 | `define-plugin-reload-consumer` | Pending | Реакция UI и cache invalidation после reload. |
| 13 | `retire-compatibility-adapters` | Pending | PluginManager и прочие переходные API. |
| 14 | `remove-or-isolate-legacy-main-window` | Pending | После проверки references. |
| 15 | `add-architecture-enforcement` | Pending | CI и dependency rules. |

## Следующая работа

Следующий implementation change: `define-plugin-reload-consumer`.

`unify-single-instance-ownership` завершён: references на `SingleInstanceGuard` проверены, ownership передан DI provider, добавлены regression tests на registration и disposal, OpenSpec change архивирован.

`extract-application-bootstrapper` завершён: startup ordering и failure policy вынесены из `App.OnStartup` в application bootstrapper, WPF wiring изолирован через startup UI/context ports, lazy factories предотвращают создание UI до single-instance проверки, добавлены orchestration и composition tests.

`add-application-operation-lifetime` завершён: один application CTS и tracked-operation registry используются для startup update checks, update downloads, agent commands, version population и settings/update controls; `App.OnExit` выполняет cancellation и bounded await перед DI disposal, добавлены cancellation/timeout tests.

`split-launch-window-workflows` завершён: project и agent pane operations вынесены в WPF-independent controllers, сохранены selection identity, filtering, refresh и cancellable version population, добавлены focused controller tests.

`extract-settings-draft-and-application` завершён: typed Settings input/draft, validation, ordered system application, atomic persistence update и rollback policy вынесены в `SettingsApplicationService`; `SettingsViewModel` оставлена binding/presentation facade, добавлены service и ViewModel tests. `decouple-runtime-preference-application` завершён: `PreferenceApplier` больше не зависит от concrete WPF targets, а runtime/display/hotkey boundaries покрыты узкими портами и tests.

`separate-tray-actions-from-tray-host` завершён: `TrayIconController` оставлен владельцем `TaskbarIcon`, tray state/actions вынесены в `TrayActions`, menu builder принимает prepared state и commands, а update notifier работает через `ITrayHost`.

`split-update-service-contracts` завершён: update consumers используют отдельные version/check/state/download/install ports, а все aliases разрешаются на один Velopack-backed update adapter.

`harden-process-boundaries` завершён: output process cancellation теперь явно завершает process tree и отличается от timeout, а command-line builder тестируется напрямую.

`harden-configuration-boundary` завершён: `ConfigurationSnapshot` стал единственным mutable state owner, persistence DTO mapping покрывает все поля, а concurrent/Flush/migration/atomic-write semantics сохранены.

`introduce-typed-runtime-settings` завершён: runtime preferences используют `RuntimeKind`, JSON tokens конвертируются только на persistence boundary, а unknown/legacy values безопасно мигрируются.
