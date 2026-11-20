# DoomPcSpeaker

Консольный плеер мелодий для **hardware PC speaker** (пищалка на материнке) через `inpoutx64.dll` (i8254 PIT, порты `0x43/0x42/0x61`).

## Требования

- Windows x64
- .NET 9 SDK
- `inpoutx64.dll` рядом с exe
- Запуск от администратора

## Сборка и запуск

```powershell
dotnet build .\DoomPcSpeaker.sln -c Release
# или
.\RebuildSolution.bat

.\src\DoomPcSpeaker\bin\Release\net9.0\DoomPcSpeaker.exe
```

## Использование

```text
DoomPcSpeaker.exe                 интерактивный режим
DoomPcSpeaker.exe doom            проиграть трек
DoomPcSpeaker.exe 7,3,14          очередь треков
DoomPcSpeaker.exe random          случайный трек
DoomPcSpeaker.exe --list          список треков
DoomPcSpeaker.exe --stop          заглушить спикер
DoomPcSpeaker.exe --help          справка
```

В интерактивном режиме: ввод по id/key/имени, очередь через `,`/`;`/пробел, команды `list`, `help`, `cls`, `stop`, выход `0`/`q`, прерывание `Ctrl+C`.

## Структура

- `src/DoomPcSpeaker/` — исходники проекта
- `res/` — ресурсы
- `_dependencies/` — локальные зависимости (в git не попадают)
- `DoomPcSpeaker.sln`, `RebuildSolution.bat`
