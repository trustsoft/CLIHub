## 1. Scaffold решения

- [x] 1.1 Создать `CLIHub.sln` и проекты `src/CLIHub.Core` (net10.0, без WPF), `src/CLIHub.App` (net10.0-windows, `UseWPF` + `UseWindowsForms`), `tests/CLIHub.Core.Tests` (xUnit); проверить, что `dotnet build CLIHub.sln` проходит
- [x] 1.2 Добавить ссылку тестового проекта на `CLIHub.Core`; проверить, что `dotnet test` запускается и проходит на пустом наборе
- [x] 1.3 Создать `plugins/agents/<id>/agent.json` по контракту v1 (D5) и asset-заглушку; включить как content в `CLIHub.App.csproj` с копированием в output; проверить наличие файла в `bin/.../plugins/agents/<id>/`

## 2. Core: швы и загрузка

- [x] 2.1 Объявить в `CLIHub.Core/Abstractions` интерфейсы `IFileSystem`, `IProcessRunner`, `IClock`, `IPathProvider`; проверить компиляцию Core
- [x] 2.2 Добавить модели манифеста (`schemaVersion`, `id`, `name`, `actions` с `command` и опциональным `runtime`); проверить десериализацию тестового JSON
- [x] 2.3 Реализовать `PluginLoader`: читает `plugins/agents/*/agent.json`, `id` авторитетен, неизвестные поля игнорируются, битый манифест и неподдерживаемый `schemaVersion` пропускаются с предупреждением; проверить unit-тестами на временной папке (валид/битый/старая версия)
- [x] 2.4 Реализовать `ConfigStore`: чтение `config.json` (`runtime`, `hotkey`, `projects[]`) с дефолтами при отсутствии файла; проверить unit-тестами с подменёнными `IFileSystem`/`IPathProvider`

## 3. Core: запуск

- [x] 3.1 Реализовать разрешение runtime (`actions.<key>.runtime` -> `config.runtime` -> дефолт); проверить unit-тестами все три сценария из спеки `agent-launch`
- [x] 3.2 Реализовать построение командной строки для `cmd`/`ps`/`wt`; проверить unit-тестами точные строки и `WorkingDirectory`
- [x] 3.3 Реализовать `LauncherCore.Start` через `IProcessRunner`: запуск без ожидания, `cwd` = папка проекта, откат `wt` -> `ps`/`cmd` с предупреждением; проверить unit-тестом на fake `IProcessRunner` (команда и cwd) и ручным запуском

## 4. App: хост в трее

- [x] 4.1 Собрать composition root в `App.xaml.cs`, выставить `ShutdownMode=OnExplicitShutdown`; проверить, что приложение стартует и остаётся в процессе без окон
- [x] 4.2 Подключить H.NotifyIcon: иконка в трее и меню с пунктом «Выход»; проверить, что иконка видна, а «Выход» завершает процесс

## 5. Hotkey

- [x] 5.1 Добавить `RegisterHotKey`/`UnregisterHotKey` в `App/Interop` и регистрировать комбинацию из `config.json` (дефолт `Ctrl+Alt+Space`); проверить, что нажатие открывает попап
- [x] 5.2 Обработать занятую комбинацию: предупреждение и продолжение работы без hotkey; проверить вручную на занятой комбинации

## 6. Попап

- [x] 6.1 Создать `PopupWindow` как warm singleton на старте скрытым; закрытие через `Hide`, не `Close`; проверить, что повторный вызов показывает тот же экземпляр
- [x] 6.2 Реализовать позиционирование: `GetCursorPos` -> экран -> clamp в `WorkingArea` -> конвертация в DIP; проверить вручную на масштабировании 100%/150%/200%
- [x] 6.3 Реализовать закрытие по Esc и `Deactivated`, переключение `ShowActivated` перед `Show()`; проверить, что попап скрывается по Esc и при потере фокуса
- [x] 6.4 Показать в попапе агентов из `PluginLoader` для выбранного проекта; проверить, что агент из плагина появляется в списке
- [x] 6.5 Связать действие запуска в попапе с `LauncherCore`; проверить, что терминал открывается в папке проекта и остаётся открытым

## 7. Проверка интеграции

- [x] 7.1 Пройти end-to-end вручную: hotkey -> попап -> выбрать агента -> запуск -> терминал открыт в нужной папке; отдельно проверить на экране с масштабированием
- [x] 7.2 Прогнать `dotnet test` и `openspec validate add-hotkey-launcher-skeleton`; проверить, что оба проходят
