# Предложение 004: Модель процессного домена
**Создано:** 2026-10-29  
**Статус:** Одобрено (2026-10-29)  
**Трудозатраты:** Высокие (5-7 дней)  
**Риск:** Средний  
**Приоритет:** Фаза 2 (2 квартал 2027)  
**Зависимости:** Предложение 1 (Сквозные сервисы)  
**Владелец:** Не назначен

## Описание проблемы

В текущей архитектуре CLIHub функции работы с процессами разбросаны по нескольким сервисам без чётких границ ответственности:

### Текущая фрагментация

```
ProcessLauncher (Core)
  └─> Строит командные строки
  └─> Не выполняет запуск процессов

WindowsInteractiveProcessRunner (Windows)
  └─> Запускает терминалы
  └─> Определяет runtime (pwsh, bash, cmd)

WindowsProcessOutputRunner (Windows)
  └─> Захватывает вывод команд
  └─> Дублирует логику определения runtime

AgentProcessMatcher (Core)
  └─> Сопоставляет запущенные процессы
  └─> Парсит командные строки

WindowsAgentProcessInspector (Windows)
  └─> WMI-запросы для получения командных строк
  └─> Логика таймаутов и деградации

AgentCommandService (Core)
  └─> Оркестрирует выполнение
  └─> Зависит от нескольких runner'ов
```

### Проблемы

1. **Дублирование логики определения runtime** между interactive и output runner'ами
2. **Логика сопоставления процессов разделена** между matcher (Core) и inspector (Windows)
3. **Отсутствует единая модель процесса** — разные представления на разных уровнях
4. **Сложное тестирование** — нужно мокировать несколько сервисов для одного workflow
5. **Построение командной строки отделено** от контекста выполнения
6. **Нет явного жизненного цикла процесса** — запуск, мониторинг, сопоставление, завершение разрознены

## Предлагаемое решение

Ввести унифицированный **процессный домен** с явными value object'ами и единой границей оркестрации.

### Доменные модели

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Представляет исполняемую среду выполнения (терминал или интерпретатор).
/// </summary>
public record RuntimeExecutable
{
    public required RuntimeKind Kind { get; init; }
    public required string ExecutablePath { get; init; }
    public string? Version { get; init; }

    public static RuntimeExecutable Create(RuntimeKind kind, string path, string? version = null)
        => new() { Kind = kind, ExecutablePath = path, Version = version };
}

/// <summary>
/// Представляет запущенный процесс агента с контекстом выполнения.
/// </summary>
public record AgentProcess
{
    public required int ProcessId { get; init; }
    public required string CommandLine { get; init; }
    public required RuntimeExecutable? Runtime { get; init; }
    public required DateTime StartTime { get; init; }

    public bool IsRuntimeHosted => this.Runtime?.Kind is RuntimeKind.Node or RuntimeKind.Python;
}

/// <summary>
/// Запрос на запуск команды агента в конкретном контексте.
/// </summary>
public record LaunchRequest
{
    public required string WorkingDirectory { get; init; }
    public required AgentCommand Command { get; init; }
    public required RuntimePreference Preference { get; init; }
    public CancellationToken CancellationToken { get; init; }

    public static LaunchRequest Create(
        string workingDirectory,
        AgentCommand command,
        RuntimePreference preference,
        CancellationToken cancellationToken = default)
        => new()
        {
            WorkingDirectory = workingDirectory,
            Command = command,
            Preference = preference,
            CancellationToken = cancellationToken
        };
}

/// <summary>
/// Результат выполнения процесса.
/// </summary>
public record ProcessResult
{
    public required ProcessResultKind Kind { get; init; }
    public int? ExitCode { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public TimeSpan? Duration { get; init; }

    public bool IsSuccess => this.Kind == ProcessResultKind.Success;

    public static ProcessResult Success(int exitCode, string? output = null, TimeSpan? duration = null)
        => new()
        {
            Kind = ProcessResultKind.Success,
            ExitCode = exitCode,
            Output = output,
            Duration = duration
        };

    public static ProcessResult Failure(string error)
        => new() { Kind = ProcessResultKind.Failure, Error = error };

    public static ProcessResult Timeout()
        => new() { Kind = ProcessResultKind.Timeout };

    public static ProcessResult Cancelled()
        => new() { Kind = ProcessResultKind.Cancelled };
}

public enum ProcessResultKind
{
    Success,    // Успешное выполнение
    Failure,    // Ошибка выполнения
    Timeout,    // Превышение времени ожидания
    Cancelled   // Отменено пользователем
}

/// <summary>
/// Критерии поиска процессов агентов.
/// </summary>
public record ProcessMatchCriteria
{
    public required Plugin Plugin { get; init; }
    public required AgentProcessMatchDetail Detail { get; init; }
    public TimeSpan? Timeout { get; init; }

    public static ProcessMatchCriteria Create(
        Plugin plugin,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine,
        TimeSpan? timeout = null)
        => new() { Plugin = plugin, Detail = detail, Timeout = timeout };
}
```

### Унифицированный оркестратор

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Оркестрирует все операции с процессами: запуск, захват вывода, поиск запущенных агентов.
/// </summary>
public interface IProcessOrchestrator
{
    /// <summary>
    /// Запускает команду агента в интерактивном терминале.
    /// </summary>
    /// <param name="request">Запрос на запуск с рабочей директорией, командой и предпочтением runtime.</param>
    /// <returns>Результат процесса с исходом выполнения.</returns>
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request);

    /// <summary>
    /// Выполняет команду агента и захватывает её вывод.
    /// </summary>
    /// <param name="request">Запрос на запуск с рабочей директорией, командой и предпочтением runtime.</param>
    /// <returns>Результат процесса с кодом возврата и захваченным выводом.</returns>
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request);

    /// <summary>
    /// Находит все запущенные процессы, соответствующие указанному плагину.
    /// </summary>
    /// <param name="criteria">Критерии сопоставления с плагином, уровнем детализации и опциональным таймаутом.</param>
    /// <returns>Список соответствующих процессов агентов.</returns>
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria criteria);

    /// <summary>
    /// Определяет доступную среду выполнения для указанного предпочтения.
    /// </summary>
    /// <param name="preference">Предпочтение runtime (PowerShell, Git Bash и т.д.).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Определённая среда выполнения или null, если недоступна.</returns>
    Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimePreference preference,
        CancellationToken cancellationToken = default);
}
```

### Структура реализации

```csharp
namespace CLIHub.Core.Processes;

public class ProcessOrchestrator : IProcessOrchestrator
{
    private readonly IRuntimeResolver runtimeResolver;
    private readonly ICommandLineBuilder commandLineBuilder;
    private readonly IInteractiveProcessRunner interactiveRunner;
    private readonly IProcessOutputRunner outputRunner;
    private readonly IAgentProcessFinder processFinder;
    private readonly ILogger<ProcessOrchestrator> logger;

    public ProcessOrchestrator(
        IRuntimeResolver runtimeResolver,
        ICommandLineBuilder commandLineBuilder,
        IInteractiveProcessRunner interactiveRunner,
        IProcessOutputRunner outputRunner,
        IAgentProcessFinder processFinder,
        ILogger<ProcessOrchestrator> logger)
    {
        this.runtimeResolver = runtimeResolver;
        this.commandLineBuilder = commandLineBuilder;
        this.interactiveRunner = interactiveRunner;
        this.outputRunner = outputRunner;
        this.processFinder = processFinder;
        this.logger = logger;
    }

    public async Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request)
    {
        using var scope = this.logger.BeginScope("LaunchInteractive");

        // Определяем runtime
        var runtime = await this.runtimeResolver.ResolveAsync(
            request.Preference,
            request.CancellationToken);

        if (runtime is null)
        {
            this.logger.LogWarning(
                "Runtime {Preference} недоступен для команды {Command}",
                request.Preference,
                request.Command.Kind);
            return ProcessResult.Failure($"Runtime {request.Preference} недоступен");
        }

        // Строим командную строку
        var commandLine = this.commandLineBuilder.Build(
            runtime,
            request.Command,
            request.WorkingDirectory);

        // Запускаем
        try
        {
            var startTime = DateTime.UtcNow;
            await this.interactiveRunner.LaunchAsync(
                runtime.ExecutablePath,
                commandLine,
                request.WorkingDirectory,
                request.CancellationToken);

            var duration = DateTime.UtcNow - startTime;
            this.logger.LogInformation(
                "Запущена команда {Command} в {Runtime} (заняло {Duration}мс)",
                request.Command.Kind,
                runtime.Kind,
                duration.TotalMilliseconds);

            return ProcessResult.Success(0, duration: duration);
        }
        catch (OperationCanceledException)
        {
            return ProcessResult.Cancelled();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Не удалось запустить команду {Command}", request.Command.Kind);
            return ProcessResult.Failure(ex.Message);
        }
    }

    public async Task<ProcessResult> CaptureOutputAsync(LaunchRequest request)
    {
        using var scope = this.logger.BeginScope("CaptureOutput");

        // Аналогичная структура с LaunchInteractiveAsync, но использует outputRunner
        // Возвращает ProcessResult с захваченным выводом
        // Детали реализации следуют тому же паттерну
        throw new NotImplementedException("См. полную реализацию");
    }

    public async Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(
        ProcessMatchCriteria criteria)
    {
        using var scope = this.logger.BeginScope("FindRunningAgents");

        try
        {
            return await this.processFinder.FindAsync(criteria);
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Не удалось найти запущенные агенты для плагина {PluginId}",
                criteria.Plugin.Id);
            return Array.Empty<AgentProcess>();
        }
    }

    public async Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimePreference preference,
        CancellationToken cancellationToken = default)
    {
        return await this.runtimeResolver.ResolveAsync(preference, cancellationToken);
    }
}
```

### Вспомогательные интерфейсы

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Определяет исполняемые среды выполнения на основе предпочтений.
/// </summary>
public interface IRuntimeResolver
{
    Task<RuntimeExecutable?> ResolveAsync(
        RuntimePreference preference,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Строит командные строки для конкретных сред выполнения.
/// </summary>
public interface ICommandLineBuilder
{
    string Build(RuntimeExecutable runtime, AgentCommand command, string workingDirectory);
}

/// <summary>
/// Находит запущенные процессы агентов, соответствующие критериям.
/// </summary>
public interface IAgentProcessFinder
{
    Task<IReadOnlyList<AgentProcess>> FindAsync(ProcessMatchCriteria criteria);
}
```

## Стратегия миграции

### Фаза 1: Внедрение доменных моделей (2 дня)
1. Создать новое пространство имён `CLIHub.Core/Processes/`
2. Добавить value object'ы: `RuntimeExecutable`, `AgentProcess`, `LaunchRequest`, `ProcessResult`
3. Без breaking changes — новые типы сосуществуют со старыми

### Фаза 2: Реализация оркестратора (2 дня)
1. Создать `ProcessOrchestrator`, реализующий `IProcessOrchestrator`
2. Подключить существующие runner'ы/matcher'ы как зависимости
3. Зарегистрировать в DI параллельно с существующими сервисами
4. Написать комплексные unit-тесты

### Фаза 3: Миграция потребителей (2-3 дня)
1. Обновить `AgentCommandService` для использования `IProcessOrchestrator`
2. Обновить `LaunchCommandCoordinator` для использования оркестратора
3. Обновить `WindowActionCoordinator` для использования оркестратора
4. Обновить `AgentProcessMonitor` для использования `FindRunningAgentsAsync`
5. Запускать полный набор тестов после каждой миграции потребителя

### Фаза 4: Устаревание старых интерфейсов (опционально, позже)
1. Пометить старые интерфейсы как `[Obsolete]` после миграции всех потребителей
2. Удалить в следующей мажорной версии

## Преимущества

### 1. Единый источник истины
Все операции с процессами идут через один оркестратор — проще понимать, отлаживать и расширять.

### 2. Унифицированная модель процесса
`AgentProcess` и `RuntimeExecutable` используются на всех уровнях — нет несоответствия представлений.

### 3. Тестируемость
Мокировать `IProcessOrchestrator` вместо 5 разных сервисов. Тесты проще и более сфокусированы.

**До:**
```csharp
// Нужно мокировать несколько сервисов
var interactiveRunner = new Mock<IInteractiveProcessRunner>();
var outputRunner = new Mock<IProcessOutputRunner>();
var matcher = new Mock<IAgentProcessMatcher>();
var inspector = new Mock<IAgentProcessInspector>();
var launcher = new Mock<IProcessLauncher>();

var service = new AgentCommandService(
    interactiveRunner.Object,
    outputRunner.Object,
    launcher.Object,
    ...);
```

**После:**
```csharp
// Мокировать один оркестратор
var orchestrator = new Mock<IProcessOrchestrator>();
orchestrator
    .Setup(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()))
    .ReturnsAsync(ProcessResult.Success(0));

var service = new AgentCommandService(orchestrator.Object, ...);
```

### 4. Чёткие границы
- **Core** владеет: доменными моделями, интерфейсом оркестрации
- **Windows** владеет: платформо-специфичными runner'ами, WMI-запросами
- **UI** использует: оркестратор для всех операций с процессами

### 5. Расширяемость в будущем
Легко добавить новые возможности:
- Удалённое выполнение процессов (SSH, контейнеры)
- Песочница процессов (ограниченные права)
- Телеметрия процессов (метрики, трассировка)
- Пулинг процессов (переиспользование долгоживущих агентов)

## Примеры использования

### До (текущее состояние)

```csharp
public class AgentCommandService
{
    public async Task<AgentCommandResult> ExecuteAsync(...)
    {
        // Разбросанная логика по нескольким сервисам
        var commandLine = this.launcher.Build(...);
        var runtime = this.runtimeResolver.Resolve(...);

        if (command.Kind == AgentCommandKind.Interactive)
        {
            await this.interactiveRunner.LaunchAsync(...);
        }
        else
        {
            var output = await this.outputRunner.CaptureAsync(...);
        }

        // Проверка запущенных процессов
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
    private readonly IProcessOrchestrator processOrchestrator;

    public async Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin,
        AgentCommandKind commandKind,
        string projectPath,
        CancellationToken cancellationToken)
    {
        var command = plugin.Commands.Get(commandKind)
            ?? return AgentCommandResult.Failure("Команда не найдена");

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

public class AgentProcessMonitor
{
    private readonly IProcessOrchestrator processOrchestrator;

    public async Task<bool> IsAgentRunningAsync(Plugin plugin)
    {
        var criteria = ProcessMatchCriteria.Create(
            plugin,
            AgentProcessMatchDetail.CommandLine,
            timeout: TimeSpan.FromSeconds(2));

        var processes = await this.processOrchestrator.FindRunningAgentsAsync(criteria);
        return processes.Count > 0;
    }
}
```

## Стратегия тестирования

### Unit-тесты (Core)

```csharp
public class ProcessOrchestratorTests
{
    [Fact]
    public async Task LaunchInteractiveAsync_СКорректнымЗапросом_ВозвращаетУспех()
    {
        // Arrange
        var runtimeResolver = new Mock<IRuntimeResolver>();
        runtimeResolver
            .Setup(x => x.ResolveAsync(It.IsAny<RuntimePreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RuntimeExecutable.Create(RuntimeKind.PowerShell, "pwsh.exe"));

        var commandLineBuilder = new Mock<ICommandLineBuilder>();
        commandLineBuilder
            .Setup(x => x.Build(It.IsAny<RuntimeExecutable>(), It.IsAny<AgentCommand>(), It.IsAny<string>()))
            .Returns("тестовая команда");

        var interactiveRunner = new Mock<IInteractiveProcessRunner>();
        interactiveRunner
            .Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var orchestrator = new ProcessOrchestrator(
            runtimeResolver.Object,
            commandLineBuilder.Object,
            interactiveRunner.Object,
            Mock.Of<IProcessOutputRunner>(),
            Mock.Of<IAgentProcessFinder>(),
            NullLogger<ProcessOrchestrator>.Instance);

        var request = LaunchRequest.Create(
            "C:\\test",
            new AgentCommand { Kind = AgentCommandKind.Interactive },
            RuntimePreference.PowerShell);

        // Act
        var result = await orchestrator.LaunchInteractiveAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        interactiveRunner.Verify(x => x.LaunchAsync(
            "pwsh.exe",
            "тестовая команда",
            "C:\\test",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchInteractiveAsync_СНедоступнымRuntime_ВозвращаетОшибку()
    {
        // Arrange
        var runtimeResolver = new Mock<IRuntimeResolver>();
        runtimeResolver
            .Setup(x => x.ResolveAsync(It.IsAny<RuntimePreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuntimeExecutable?)null);

        var orchestrator = new ProcessOrchestrator(
            runtimeResolver.Object,
            Mock.Of<ICommandLineBuilder>(),
            Mock.Of<IInteractiveProcessRunner>(),
            Mock.Of<IProcessOutputRunner>(),
            Mock.Of<IAgentProcessFinder>(),
            NullLogger<ProcessOrchestrator>.Instance);

        var request = LaunchRequest.Create(
            "C:\\test",
            new AgentCommand { Kind = AgentCommandKind.Interactive },
            RuntimePreference.GitBash);

        // Act
        var result = await orchestrator.LaunchInteractiveAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("недоступен");
    }

    [Fact]
    public async Task FindRunningAgentsAsync_СКорректнымиКритериями_ВозвращаетПроцессы()
    {
        // Arrange
        var expectedProcesses = new[]
        {
            new AgentProcess
            {
                ProcessId = 1234,
                CommandLine = "node agent.js",
                Runtime = RuntimeExecutable.Create(RuntimeKind.Node, "node.exe"),
                StartTime = DateTime.UtcNow
            }
        };

        var processFinder = new Mock<IAgentProcessFinder>();
        processFinder
            .Setup(x => x.FindAsync(It.IsAny<ProcessMatchCriteria>()))
            .ReturnsAsync(expectedProcesses);

        var orchestrator = new ProcessOrchestrator(
            Mock.Of<IRuntimeResolver>(),
            Mock.Of<ICommandLineBuilder>(),
            Mock.Of<IInteractiveProcessRunner>(),
            Mock.Of<IProcessOutputRunner>(),
            processFinder.Object,
            NullLogger<ProcessOrchestrator>.Instance);

        var criteria = ProcessMatchCriteria.Create(
            new Plugin { Id = "test-agent" },
            AgentProcessMatchDetail.CommandLine);

        // Act
        var processes = await orchestrator.FindRunningAgentsAsync(criteria);

        // Assert
        processes.Should().HaveCount(1);
        processes[0].ProcessId.Should().Be(1234);
        processes[0].IsRuntimeHosted.Should().BeTrue();
    }
}
```

### Интеграционные тесты (Windows)

```csharp
public class ProcessOrchestratorIntegrationTests
{
    [SkippableFact]
    public async Task LaunchInteractiveAsync_СРеальнымPowerShell_Успешно()
    {
        Skip.If(!OperatingSystem.IsWindows(), "Тест только для Windows");

        // Arrange
        var services = new ServiceCollection();
        services.AddCLIHubCoreServices();
        services.AddWindowsInfrastructure();
        var provider = services.BuildServiceProvider();

        var orchestrator = provider.GetRequiredService<IProcessOrchestrator>();

        var request = LaunchRequest.Create(
            Environment.CurrentDirectory,
            new AgentCommand
            {
                Kind = AgentCommandKind.Interactive,
                Executable = "echo",
                Arguments = "test"
            },
            RuntimePreference.PowerShell);

        // Act
        var result = await orchestrator.LaunchInteractiveAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Duration.Should().NotBeNull();
    }
}
```

## Снижение рисков

### Риск 1: Поломка существующих потребителей
**Снижение:**
- Внедрить оркестратор параллельно с существующими сервисами (без удаления)
- Мигрировать по одному потребителю за раз
- Запускать полный набор тестов после каждой миграции
- Сохранять старые интерфейсы до миграции всех потребителей

### Риск 2: Регрессия производительности
**Снижение:**
- Бенчмарки до/после с BenchmarkDotNet
- Профилирование путей WMI-запросов (AgentProcessFinder)
- Кеширование результатов определения runtime (уже кешируется в текущей реализации)
- Мониторинг латентности запуска в продакшене

### Риск 3: Платформо-специфичные крайние случаи
**Снижение:**
- Комплексные интеграционные тесты для Windows
- Тесты с реальными процессами для всех runtime (pwsh, bash, cmd, Windows Terminal)
- Покрытие тестами путей с таймаутами и деградацией
- Бета-тестирование с различными конфигурациями терминалов

## Рассмотренные альтернативные подходы

### Альтернатива 1: Сохранить текущую структуру, добавить фасад
**Плюсы:** Минимальные изменения  
**Минусы:** Не решает фрагментацию, добавляет слой перенаправления

### Альтернатива 2: Переместить всё в слой Windows
**Плюсы:** Упрощает Core  
**Минусы:** Нарушает ADR-0001 (границы Core/UI), теряется кроссплатформенный потенциал

### Альтернатива 3: Несколько оркестраторов (LaunchOrchestrator, MatchOrchestrator)
**Плюсы:** Меньшие интерфейсы  
**Минусы:** Всё ещё фрагментировано, неясно какой оркестратор использовать

**Решение:** Предложение 4 (единый унифицированный оркестратор) — лучший баланс простоты и мощности.

## Критерии успеха

✅ Все 1,221 тестов всё ещё проходят  
✅ Ноль новых предупреждений сборки  
✅ Нет регрессии производительности (время запуска в пределах 5% от базовой линии)  
✅ Все потребители мигрированы на `IProcessOrchestrator`  
✅ Интеграционные тесты для всех runtime (pwsh, bash, cmd, Windows Terminal)  
✅ ADR-0012 (Унификация процессного домена) принят и опубликован

## Связанные документы

- **Анализ:** `docs/architecture-analysis-2026-10-29.md` (Раздел 3.4)
- **Визуализация:** `docs/architecture-visualization.md` (раздел Data Flow)
- **ADR:** ADR-0001 (границы Core/UI), ADR-0006 (библиотека платформы Windows)
- **Текущая реализация:**
  - `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`
  - `src/CLIHub.Core.Windows/WindowsInteractiveProcessRunner.cs`
  - `src/CLIHub.Core.Windows/WindowsProcessOutputRunner.cs`
  - `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs`
  - `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs`

---

**Статус:** Готово к рассмотрению  
**Владелец:** Не назначен  
**Целевой срок:** 2 квартал 2027 (Фаза 2)
