# Solana Pump Tracker (WPF, .NET 8)

Мини-приложение для Windows, которое подключается к WebSocket и показывает новые токены в реальном времени.
Фильтрация токенов задается в конфиге (Blacklist/Whitelist, Twitter, Dev, Market Cap, Dev Migrations and other).

## Как запустить

**Требования:** Windows 10/11 x64, установлен .NET 8 SDK.

```bash
git clone https://github.com/stepamak/pump_tracker
cd pump_tracker
dotnet build
dotnet run --project SolanaPumpTracker.csproj
```

Опционально собрать .exe:
```bash
dotnet publish SolanaPumpTracker.csproj -c Release -r win-x64   -p:PublishSingleFile=true --self-contained true
```

Готовый файл будет в:
```
SolanaPumpTracker/bin/Release/net8.0-windows/win-x64/publish/
```

## Настройка

В приложении откройте **Config** и заполните:
- **Endpoint** (например, `ws://<host>:<port>/ws`)
- **API Key**

При необходимости включите/отключите фильтры.

## Как получить API-ключ и Websocket

Напишите автору в Telegram (дам потестить): **[@stp4lfe](https://t.me/stp4lfe)**
