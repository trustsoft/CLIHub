# Roadmap — новое окно настроек CLIHub

- **Дата:** 2026-10-10
- **Статус:** черновик (согласован на уровне декомпозиции)
- **Источник:** дизайн-сессия по Stitch-прототипам окна «CLIHub Settings» (проект `16339686008017084388`)
- **Назначение:** навигационный документ. Задаёт целевое состояние и **последовательность OpenSpec-ченджей**. Не является частью контракта OpenSpec и не валидируется `openspec validate`.

## 1. Цель

Перевести окно Settings из одной прокручиваемой страницы в окно с **боковой навигацией**: 3 группы / 8 страниц. Новые настройки и фичи добавляются вертикальными срезами, каждый — отдельным OpenSpec-ченджем.

```
GENERAL
  - General & Startup
  - Hotkeys & Launchers
ENGINES & REPOS
  - Projects & Paths
  - CLI Agents (Claude/Cline)
  - Terminal Profiles
SYSTEM
  - Appearance
  - Telemetry & Logs
  - Updates
```

## 2. Матрица решений (результат дизайн-сессии)

Легенда: ✅ оставляем · 🟡 оставляем, реализацию откладываем · ⏳ позже · ❌ отклонено · 🔄 открыто.

### 2.1 General & Startup
| Настройка | Решение |
|---|---|
| Launch on Windows Startup | ✅ оставить (текущий «Start with Windows») |
| Start Minimized to System Tray | ✅ ввести, **заменяет** «Show window on startup» |
| Close Button Behavior | ❌ |
| Default Projects Root Directory | ⏳ |
| System Notifications | ✅ |

### 2.2 Hotkeys & Launchers
| Настройка | Решение |
|---|---|
| Quick Launcher Hotkey | ✅ это и есть текущая глобальная клавиша |
| Direct Agent Run Hotkey | ⏳ |
| Quick Focus Terminal Hotkey | ⏳ |
| Frameless Quick Launcher Mode | ❌ (chrome уже рисованный) |
| Dismiss on Focus Loss / Esc | 🔄 переосмыслить с учётом pinning окна запуска |
| Popup Screen Position | ✅ |

### 2.3 Projects & Paths
| Настройка | Решение |
|---|---|
| Watched Repository Directories | ✅ |
| Repository Scan Depth | ✅ |
| Ignored Directory Patterns | ✅ |
| Auto-detect OpenSpec & AI Agent Configs | 🟡 **только AI-агенты** (OpenSpec не помечать) |
| Background Watcher Status | ✅ только индикатор |

### 2.4 CLI Agents
| Настройка | Решение |
|---|---|
| Карточка агента | ✅ имя+логотип, статус установки, установленная версия, последняя версия+маркер, путь, набор команд, маркеры детекта, дефолтная модель, кнопки действий |
| Действия с агентом | ✅ Test/Probe, Update, Edit Config (Register/Remove — нет) |
| Default Agent for Quick Launcher | ✅ |
| Pass Current Project Context Automatically | ⏳ |
| Метрики | 🔄 кол-во поддерживаемых, кол-во установленных, маркер «все последней версии» |

### 2.5 Terminal Profiles
| Настройка | Решение |
|---|---|
| Primary Terminal Host | ✅ **расширить** список хостов (не только cmd/ps/wt) |
| Default Shell Profile | ✅ отдельная настройка |
| Launch Terminal in Split Pane | ❌ |
| Font / Size / Palette | ❌ |

### 2.6 Appearance
| Настройка | Решение |
|---|---|
| Application Theme | ✅ Dark / Light / System |
| Windows 11 Mica Material | ❌ |
| Window Opacity & Blur | ✅ |
| Accent Color | ✅ |
| Compact Interface Density | 🟡 да, реализацию отложить |
| Window Corner Radius | ❌ |

### 2.7 Telemetry & Logs
| Настройка | Решение |
|---|---|
| Daemon Logging Level | ✅ |
| Active Log File Location | ✅ индикатор (+ Copy) |
| Log File Rotation & Size Limit | ✅ |
| Local IPC Named Pipe | ❌ |
| Anonymous Usage Diagnostics | ❌ |
| Open Log Folder / Export Diagnostics | ✅ только Open Log Folder |

### 2.8 Updates
| Настройка | Решение |
|---|---|
| Update Channel | ❌ (один стабильный канал) |
| Auto-download in Background | ✅ |
| Уведомления о версиях | ✅ две настройки: стабильные и pre-release |
| Version Status | 🔄 упростить |

## 3. Gap-анализ

**Есть сейчас (спеки):** `preferences-ui`, `settings-theme`, `launch-window-theme`, `hotkey-support`, `agent-commands`, `agent-detection`, `agent-version`, `agent-ui`, `agent-availability-display`, `logo-cache`, `plugin-seeding`, `project-management`, `configuration-snapshot`, `app-lifecycle`, `logging`, `update-checking`, `release-notes`, `release-notes-display`, `main-window-layout`, `shell-coordinator`.

**Основные разрывы:**
1. Окно Settings — одна страница без навигации (нужен шелл с sidebar).
2. Нет сканирования/наблюдения репозиториев (Projects & Paths).
3. Управление агентами не вынесено в Settings (нет карточек/действий/дефолтного агента).
4. Нет отдельного выбора shell и расширенного списка terminal hosts (есть только `DefaultRuntime` cmd/ps/wt).
5. Только тёмная тема; нет Light/System, opacity/blur, accent color.
6. Логи: нет UI-управления уровнем/ротацией.
7. Updates: нет auto-download и раздельных уведомлений stable/pre-release.
8. `config.json`: потребуется ряд новых полей и (при необходимости) бамп схемы + миграции.

## 4. Декомпозиция в OpenSpec-ченджи

Каждый чендж — самостоятельный срез: своя дельта спеки, свои тесты, отдельный archive.

### Фаза 0 — фундамент (первым, блокирующий)
| ID | Scope | Дельта спеки | Зависит |
|---|---|---|---|
| `settings-window-sidebar-shell` | Settings → шелл с боковой навигацией (3 группы / 8 страниц). Страницы-заглушки для будущих настроек. Сохранить Save/Cancel и откат. | `preferences-ui`, `settings-theme` | — |

### Фаза 1 — настройки по страницам
| ID | Scope | Дельта спеки | Зависит |
|---|---|---|---|
| `settings-general-startup` | Start minimized to tray (заменяет Show window on startup); System notifications. | `preferences-ui`, `app-lifecycle` | Фаза 0 |
| `settings-hotkeys-popup` | Popup screen position; переосмысление dismiss-on-focus-loss vs pinning. | `hotkey-support`, `preferences-ui` | Фаза 0 |
| `settings-terminal-profiles` | Расширенный список terminal hosts; отдельный default shell. | `preferences-ui`, `configuration-snapshot` | Фаза 0 |
| `settings-appearance-theme` | Dark/Light/System; window opacity/blur; accent color. Сквозной по всем окнам. | `launch-window-theme`, `settings-theme` | Фаза 0 |
| `settings-logging` | Logging level; log file location (индикатор); rotation/size; Open log folder. | `logging` | Фаза 0 |
| `settings-updates` | Auto-download в фоне; уведомления stable + pre-release; упрощённый version status. | `update-checking` | Фаза 0 |

### Фаза 2 — крупные фичи
| ID | Scope | Дельта спеки | Зависит |
|---|---|---|---|
| `settings-projects-scanning` | Watched directories; scan depth; ignored patterns; auto-detect AI-агентов; watcher-индикатор. | новый `project-scanning` (+ `project-management`) | Фаза 1 (`settings-terminal-profiles` не обязателен) |
| `settings-agents-management` | Карточки агентов и действия (Test/Probe, Update, Edit Config); default agent; новые метрики. | `agent-ui`, `agent-detection`, `agent-version`, `agent-commands`, `preferences-ui` | `settings-projects-scanning` (для auto-detect) |

### Later (вне этого цикла, отдельными ченджами)
`settings-compact-density` · `settings-agent-context-passing` · `settings-projects-root` · `settings-direct-run-hotkey` · `settings-focus-terminal-hotkey`.

### Сквозное
- **`add-settings-schema-fields`** (либо в каждом чендже, где появляются поля): бамп схемы `config.json`, поля в `PreferencesStore`, миграции.

## 5. Порядок и зависимости

```text
settings-window-sidebar-shell
        │
        ├── settings-general-startup
        ├── settings-hotkeys-popup
        ├── settings-terminal-profiles ──┐
        ├── settings-appearance-theme    │
        ├── settings-logging             │
        └── settings-updates             │
                                         ▼
                            settings-projects-scanning
                                         │
                                         ▼
                            settings-agents-management
```

Фаза 1 — срезы независимы и могут идти в любом порядке. Фаза 2 — строго после соответствующих зависимостей.

## 6. Эволюция `config.json`

- Новые поля добавляются **аддитивно**; там, где возможно — с дефолтами, совместимыми с прежним поведением.
- Бамп `schemaVersion` — только если меняется форма/семантика существующих полей (например, замена `ShowWindowOnStartup` на `StartMinimized`). Каждый такой бамп сопровождается миграцией.
- Переименования/удаления полей — с обратно-совместимой миграцией.

## 7. Out of scope

Всё из раздела ❌ (Close Button Behavior, Frameless Mode, Split Pane, Font/Size/Palette, Mica, Corner Radius, IPC Pipe Status, Anonymous Diagnostics, Update Channel), а также всё из ⏳/🟡 до отдельного решения.

## 8. Процесс работы по каждому ченджу

1. `openspec propose` → черновик change (`proposal.md` + `specs/` дельта) → `openspec validate`.
2. Непосредственно перед реализацией — детальный implementation-ready план (gitnexus-plan) под **этот** чендж.
3. Реализация + тесты (`dotnet build` / `dotnet test`).
4. `openspec archive` после верификации.

Deep-планы не делаем заранее на всю Фазу 2 — только по факту взятия ченджа в работу.

## 9. Открытые вопросы

- **Dismiss on Focus Loss / Esc** — как совместить с существующим pinning окна запуска (🔁 пересмотреть в `settings-hotkeys-popup`).
- **Auto-detect** — уточнить маркеры именно AI-агентов (без OpenSpec) в `settings-projects-scanning`.
- **Метрики агентов** — финальные названия и источники данных в `settings-agents-management`.
