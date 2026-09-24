# TrayAlarmWatcher

Трей-застосунок для Windows, який стежить за повітряною тривогою в обраному регіоні України через [API ukrainealarm.com](https://api.ukrainealarm.com) і показує поточний статус іконкою в системному треї.

## Можливості

- **Іконка в треї** з чотирма станами: 🔴 тривога (рівень API `Red`), 🟡 підвищена небезпека (рівень `Yellow`), 🟢 спокійно, ⚪ невідомо/помилка мережі
- **Контекстне меню**: поточний статус і час останньої перевірки, "Оновити зараз", вибір регіону, "Запускати з Windows" (checkbox), "Вийти"
- **Вибір будь-якого регіону вручну** — каскадне меню "Обрати район/місто": область → район → місто/громада, з позначкою поточного вибору. Можна обрати як цілий район, так і конкретне місто/громаду
- **Автоматичне опитування API кожні 60 секунд**
- **Спливаючі сповіщення** при зміні статусу — "Оголошено повітряну тривогу" / "Відбій повітряної тривоги" (лише для переходів у/з повної тривоги; перехід у "підвищену небезпеку" — без сповіщення, тільки зміна іконки). "Відбій" спрацює навіть якщо тривога спершу знизилась до "підвищеної небезпеки", а вже потім до спокою
- **Стійкість до збоїв**: retry з backoff (30с → 60с → 120с) при мережевих помилках, окрема обробка HTTP 429 (rate limit) з урахуванням `Retry-After`
- **Логування помилок** у `%AppData%\TrayAlarmWatcher\log.txt`
- Якщо `regionId` ще не задано, застосунок при старті покаже підказку обрати регіон через меню (нічого не вгадує й не підставляє за замовчуванням)
- **Автозапуск з Windows** через ключ реєстру `HKCU\...\Run` (вмикається/вимикається прямо з меню)
- Публікується як **один самодостатній `.exe`** (self-contained single-file), без встановлення .NET SDK на цільовій машині

## Конфігурація

Застосунок читає налаштування з `%AppData%\TrayAlarmWatcher\config.json`:

```json
{
  "apiKey": "YOUR_API_KEY",
  "regionId": "75"
}
```

- `apiKey` — ключ доступу до api.ukrainealarm.com (заголовок `Authorization`, без префікса `Bearer`)
- `regionId` — ID області/району/громади зі списку [ukrainealarm.com](https://api.ukrainealarm.com). Можна не вказувати: якщо поле порожнє, застосунок при старті попросить обрати регіон через меню трея "Обрати район/місто" — воно само збереже вибір сюди. Змінити на інший регіон можна в будь-який момент так само

Дивись [config.example.json](config.example.json) як шаблон. Файл `config.json` навмисно не потрапляє в git ([.gitignore](.gitignore)) — він містить секретний ключ.

## Вимоги

- .NET 8 SDK (для розробки/збірки)
- .NET 8 Desktop Runtime (для запуску framework-dependent білду; не потрібен для self-contained publish)
- Windows 10/11 (x64)

## Збірка та запуск (розробка)

```bash
dotnet build TrayAlarmWatcher/TrayAlarmWatcher.csproj -c Debug
dotnet run --project TrayAlarmWatcher/TrayAlarmWatcher.csproj
```

## Публікація одного `.exe`

```bash
dotnet publish TrayAlarmWatcher/TrayAlarmWatcher.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:DebugType=none -o dist
```

Результат — один файл `dist/TrayAlarmWatcher.exe` (~70 МБ, включає .NET runtime), який можна запускати на будь-якій Windows-машині без попереднього встановлення .NET.

## Структура проєкту

```
TrayAlarmWatcher/
├── Api/                    # HTTP-клієнти до ukrainealarm.com (regions, alerts), моделі відповідей, RegionLookupService
├── Configuration/          # AppConfig — читання/запис config.json
├── Models/                 # AlarmStatus, AlarmStatusSnapshot
├── Services/                # AlarmStatusChecker (retry/backoff), AutoStartManager, FileLogger
├── Program.cs               # Точка входу (без консольного вікна)
├── TrayApplicationContext.cs # Головна логіка: ApplicationContext, NotifyIcon, меню, таймер опитування
└── TrayIconFactory.cs        # Генерація іконок трею (GDI+, без файлів .ico)
```

## Логи та діагностика

- `%AppData%\TrayAlarmWatcher\log.txt` — помилки HTTP-запитів і невдалі спроби
- `%AppData%\TrayAlarmWatcher\regions-log.json` — повний список regions з API (створюється один раз, при першому запуску) — корисно для діагностики або щоб знайти `regionId` вручну, якщо меню вибору регіону з якоїсь причини недоступне
