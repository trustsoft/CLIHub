# Предложение 4: Модель процессного домена — Краткая сводка

## 🎯 Суть проблемы

Работа с процессами в CLIHub разбросана по **5 разным сервисам**:
- `ProcessLauncher` — строит командные строки
- `WindowsInteractiveProcessRunner` — запускает терминалы
- `WindowsProcessOutputRunner` — захватывает вывод
- `AgentProcessMatcher` — ищет запущенные процессы
- `WindowsAgentProcessInspector` — получает информацию через WMI

**Результат:** дублирование логики, сложное тестирование, неясные границы ответственности.

## 💡 Предлагаемое решение

Создать **единый оркестратор процессов** с чистой доменной моделью:

```csharp
public interface IProcessOrchestrator
{
    // Запуск в интерактивном терминале
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request);
    
    // Выполнение с захватом вывода
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request);
    
    // Поиск запущенных агентов
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria criteria);
    
    // Определение доступного runtime
    Task<RuntimeExecutable?> ResolveRuntimeAsync(RuntimePreference preference, ...);
}
```

### Доменные модели

```csharp
// Среда выполнения (терминал/интерпретатор)
public record RuntimeExecutable
{
    public RuntimeKind Kind { get; init; }        // PowerShell, Bash, Node...
    public string ExecutablePath { get; init; }   // C:\...\pwsh.exe
    public string? Version { get; init; }         // 7.4.0
}

// Запущенный процесс агента
public record AgentProcess
{
    public int ProcessId { get; init; }
    public string CommandLine { get; init; }
    public RuntimeExecutable? Runtime { get; init; }
    public DateTime StartTime { get; init; }
}

// Запрос на запуск
public record LaunchRequest
{
    public string WorkingDirectory { get; init; }
    public AgentCommand Command { get; init; }
    public RuntimePreference Preference { get; init; }
}

// Результат выполнения
public record ProcessResult
{
    public ProcessResultKind Kind { get; init; }  // Success, Failure, Timeout, Cancelled
    public int? ExitCode { get; init; }
    public string? Output { get; init; }
    public TimeSpan? Duration { get; init; }
}
```

## 📊 Сравнение: До vs. После

### До (текущее состояние)

```csharp
public class AgentCommandService
{
    // 5 зависимостей для работы с процессами
    private readonly IProcessLauncher launcher;
    private readonly IInteractiveProcessRunner interactiveRunner;
    private readonly IProcessOutputRunner outputRunner;
    private readonly IAgentProcessMatcher matcher;
    private readonly IAgentProcessInspector inspector;

    public async Task<AgentCommandResult> ExecuteAsync(...)
    {
        // Разбросанная логика
        var commandLine = this.launcher.Build(...);
        var runtime = this.runtimeResolver.Resolve(...);
        
        if (command.Kind == AgentCommandKind.Interactive)
            await this.interactiveRunner.LaunchAsync(...);
        else
            var output = await this.outputRunner.CaptureAsync(...);
            
        var matcher = new AgentProcessMatcher(...);
        var processes = await this.inspector.GetRunningAgentsAsync(...);
        var matches = processes.Where(p => matcher.IsMatch(p, plugin));
    }
}
```

### После (предлагаемое)

```csharp
public class AgentCommandService
{
    // 1 зависимость для всех операций с процессами
    private readonly IProcessOrchestrator processOrchestrator;

    public async Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin,
        AgentCommandKind commandKind,
        string projectPath,
        CancellationToken cancellationToken)
    {
        var command = plugin.Commands.Get(commandKind);
        
        var request = LaunchRequest.Create(
            projectPath,
            command,
            this.preferencesStore.GetRuntimePreference(),
            cancellationToken);

        var result = commandKind == AgentCommandKind.Interactive
            ? await this.processOrchestrator.LaunchInteractiveAsync(request)
            : await this.processOrchestrator.CaptureOutputAsync(request);

        return result.IsSuccess
            ? AgentCommandResult.Success(result.Output)
            : AgentCommandResult.Failure(result.Error);
    }
}
```

## ✅ Преимущества

| Аспект | Улучшение |
|--------|-----------|
| **Зависимости** | 5 интерфейсов → 1 оркестратор |
| **Тестирование** | 5 моков → 1 мок |
| **Дублирование** | Логика runtime в 2 местах → в 1 месте |
| **Понятность** | Размыты границы → чёткая ответственность |
| **Расширяемость** | Сложно добавлять → легко добавлять (SSH, Docker, песочница) |

## 📅 План миграции (5-7 дней)

### Фаза 1: Доменные модели (2 дня)
- Создать `CLIHub.Core/Processes/` namespace
- Добавить record'ы: `RuntimeExecutable`, `AgentProcess`, `LaunchRequest`, `ProcessResult`
- **Без breaking changes** — новые типы сосуществуют со старыми

### Фаза 2: Оркестратор (2 дня)
- Реализовать `ProcessOrchestrator`
- Подключить существующие runner'ы как зависимости
- Зарегистрировать в DI
- Написать unit-тесты

### Фаза 3: Миграция потребителей (2-3 дня)
- Обновить `AgentCommandService`
- Обновить `LaunchCommandCoordinator`
- Обновить `WindowActionCoordinator`
- Обновить `AgentProcessMonitor`
- После каждого шага — полный прогон тестов

### Фаза 4: Устаревание старых API (опционально)
- Пометить старые интерфейсы `[Obsolete]`
- Удалить в следующей мажорной версии

## ⚠️ Риски и снижение

### Риск 1: Поломка существующего кода
**Снижение:**
- Постепенная миграция (оба API работают параллельно)
- Полный прогон тестов после каждого изменения
- Один потребитель за раз

### Риск 2: Регрессия производительности
**Снижение:**
- Бенчмарки до/после (BenchmarkDotNet)
- Профилирование WMI-запросов
- Кеширование результатов определения runtime

### Риск 3: Платформо-специфичные баги
**Снижение:**
- Интеграционные тесты для всех runtime (pwsh, bash, cmd, Windows Terminal)
- Тесты с реальными процессами
- Бета-тестирование

## 🎯 Критерии успеха

- ✅ Все 1,221 теста проходят
- ✅ 0 новых предупреждений сборки
- ✅ Производительность ±5% от базовой линии
- ✅ Все потребители мигрированы
- ✅ ADR-0012 принят

## 📚 Связанные документы

- **Полное описание (English):** `docs/proposals/proposal-004-process-domain-model.md`
- **Полное описание (Русский):** `docs/proposals/proposal-004-process-domain-model-ru.md`
- **Анализ архитектуры:** `docs/architecture-analysis-2026-10-29.md` (раздел 3.4)
- **Визуализация:** `docs/architecture-visualization.md`

## 💬 Обсуждение

**Приоритет:** Средний (Фаза 2, Q2 2027)  
**Трудозатраты:** Высокие (5-7 дней)  
**Риск:** Средний  
**ROI:** Средний  

**Зависимости:** Рекомендуется выполнить после Предложения 1 (Сквозные сервисы) для переиспользования `ILoggingContextProvider`.

---

**Вопросы для обсуждения:**

1. Нужен ли отдельный интерфейс для мониторинга процессов (`IProcessMonitor`)?
2. Должен ли оркестратор публиковать доменные события (из Предложения 2)?
3. Стоит ли добавить timeout'ы на уровне `LaunchRequest`?
4. Нужна ли поддержка batch-запуска (несколько команд одновременно)?
