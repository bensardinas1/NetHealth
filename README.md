# NetHealth

A lightweight Windows 11 system tray widget that monitors the health and accessibility of network resources.

## Features

- **System Tray Widget** — lives in the notification area with minimal resource footprint
- **Ping Monitor** — ICMP ping to configured hosts (gateway, DNS servers, custom targets)
- **DNS Resolution Check** — verifies DNS is resolving correctly
- **HTTP Endpoint Health** — HEAD requests to configured URLs with status tracking
- **Visual Status** — tray icon changes color (green/yellow/red) based on overall health
- **Toast Notifications** — Windows notifications on state changes
- **Configurable** — JSON-based configuration for targets, intervals, and thresholds

## Requirements

- Windows 10/11
- No runtime dependencies (Native AOT compiled)

## Building

```bash
dotnet build
```

## Publishing (single-file native executable)

```bash
dotnet publish -c Release
```

## Configuration

Edit `config/targets.json` to define your monitored targets:

```json
{
  "pollIntervalSeconds": 30,
  "targets": [
    {
      "name": "Gateway",
      "type": "ping",
      "host": "192.168.1.1",
      "timeoutMs": 2000,
      "thresholdMs": 100
    },
    {
      "name": "Google DNS",
      "type": "dns",
      "host": "dns.google",
      "resolve": "google.com",
      "timeoutMs": 3000
    },
    {
      "name": "GitHub",
      "type": "http",
      "url": "https://github.com",
      "timeoutMs": 5000,
      "expectedStatusCode": 200
    }
  ]
}
```

### Target Types

| Type | Description | Key Fields |
|------|-------------|------------|
| `ping` | ICMP echo request | `host`, `timeoutMs`, `thresholdMs` |
| `dns` | DNS name resolution | `host` (DNS server), `resolve` (domain to resolve), `timeoutMs` |
| `http` | HTTP HEAD request | `url`, `timeoutMs`, `expectedStatusCode` |

## Architecture

- **.NET 8 Native AOT** — compiles to a single native executable, no runtime needed
- **WinForms** — lightest managed UI framework, used only for tray icon and overlay
- **Async monitors** — each check runs asynchronously on a timer, no blocking
- **~8-12 MB RAM** baseline footprint

## License

MIT License — see [LICENSE](LICENSE) for details.
