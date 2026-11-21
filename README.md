# DoomPcSpeaker

Консольный плеер мелодий для **hardware PC speaker** (пищалка на материнке) через `inpoutx64.dll` (i8254 PIT, порты `0x43/0x42/0x61`).

## Требования

- Windows x64
- .NET 9 SDK
- `inpoutx64.dll` рядом с exe
- Запуск от администратора

## Сборка и запуск

```powershell
dotnet build .\DoomPcSpeaker.sln -c Debug
dotnet build .\DoomPcSpeaker.sln -c Release
# или
.\RebuildSolution.bat

.\src\DoomPcSpeaker\bin\Release\net9.0\DoomPcSpeaker.exe
```

Проверка логики без обращения к железному спикеру:

```powershell
dotnet test .\DoomPcSpeaker.sln -c Debug
```

## Использование

```text
DoomPcSpeaker.exe                 интерактивный режим
DoomPcSpeaker.exe doom            проиграть трек
DoomPcSpeaker.exe tetris,mario    очередь треков
DoomPcSpeaker.exe random          случайный трек
DoomPcSpeaker.exe --list          список треков
DoomPcSpeaker.exe --stop          заглушить спикер
DoomPcSpeaker.exe --help          справка
```

В интерактивном режиме: ввод по ключу трека, очередь через `,`/`;`/пробел, команды `list/l`, `help/h`, `clear/c`, `stop/s`, `quit/q`, `random/r`, прерывание `Ctrl+C`.

## Структура

- `src/DoomPcSpeaker/` — исходники проекта:
  - `Program.cs` — точка входа и завершение работы;
  - `App.cs` — режимы CLI и интерактивной оболочки;
  - `Commands.cs` — команды, флаги и разбор запросов;
  - `Playback.cs` — очередь воспроизведения и отмена по Ctrl+C;
  - `Melodies.cs` — загрузка, каталог и разбор `.dpsm`;
  - `PcSpeaker.cs` — порты i8254 и проверка доступности драйвера;
  - `Ui.cs` — отрисовка интерфейса;
  - `Terminal.cs` — состояние консоли и ввод с подсказками;
- `tests/DoomPcSpeaker.Tests/` — регрессионные тесты разбора команд и мелодий (NUnit);
- `res/melodies/` — мелодии в формате `.dpsm` (копируются в output при сборке);
- `res/libs/inpout/` — сторонняя библиотека InpOut (DLL копируется в output при сборке);
- `_dependencies/` — локальные рабочие материалы (в git не попадают);
- `DoomPcSpeaker.sln`, `RebuildSolution.bat`.
