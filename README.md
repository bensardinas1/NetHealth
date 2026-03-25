# NetHealth

A lightweight Windows 11 system tray widget that monitors the health and accessibility of network resources.

## Features

- **System Tray Widget** — lives in the notification area with minimal resource footprint
- **Ping Monitor** — ICMP ping to configured hosts (gateway, DNS servers, custom targets)
- **DNS Resolution Check** — verifies DNS is resolving correctly
- **HTTP Endpoint Health** — HEAD requests to configured URLs with status tracking
- **Visual Status** — tray icon changes color (green/yellow/red) based on overall health
- **Live Status Dialog** — auto-refreshing status window (2s interval) with selectable text
- **Rolling Statistics** — last ping, 5-minute rolling average, last failure timestamp per target
- **Toast Notifications** — Windows notifications on state changes
- **Auto Gateway Detection** — set host to `"auto"` to detect your default gateway automatically
- **Pin to Taskbar** — right-click menu opens Windows Settings to pin the tray icon
- **Configurable** — JSON-based configuration or GUI dialog for targets, intervals, and thresholds

## Requirements

- Windows 10/11
- No runtime dependencies (self-contained single-file executable)

## Building

```bash
dotnet build
```

## Publishing (single-file executable)

```bash
dotnet publish -c Release -r win-x64
```

## Tray Context Menu

| Item | Action |
|------|--------|
| Status | Shows current overall state and target count |
| Check Now | Restarts all monitors immediately |
| Configure... | Opens the configuration dialog |
| Pin to Taskbar... | Opens Windows Settings to pin the tray icon |
| Open Config Folder | Opens the config directory in Explorer |
| Exit | Closes the application |

## Status Dialog

Double-click the tray icon to open the live status window. It shows:

- **Target name** with IP/host/URL in brackets
- **Detail** — ping threshold, DNS resolution result, HTTP status code
- **Last latency** and **5-minute rolling average**
- **Last failure** timestamp (local + UTC) or "Never"
- **Timestamps** in both local and UTC time

The dialog auto-sizes to content (max 900px), text is selectable, and it refreshes every 2 seconds while open. Only one instance can be open at a time.

## Tray Icon Colors

| Color | Meaning |
|-------|---------|
| Green | All targets healthy |
| Yellow | One or more targets degraded (slow but responding) |
| Red | One or more targets unhealthy (unreachable/failed) |
| Gray | Unknown (initializing) |

## Configuration

Edit `config/targets.json` to define your monitored targets, or use the **Configure...** option from the tray context menu.

```json
{
  "showOverlay": true,
  "notifyOnChange": true,
  "targets": [
    {
      "name": "Gateway",
      "type": "ping",
      "host": "auto",
      "timeoutMs": 2000,
      "thresholdMs": 100,
      "pollIntervalSeconds": 30,
      "enabled": true
    },
    {
      "name": "Google DNS",
      "type": "dns",
      "host": "dns.google",
      "resolve": "google.com",
      "timeoutMs": 3000,
      "pollIntervalSeconds": 30,
      "enabled": true
    },
    {
      "name": "GitHub",
      "type": "http",
      "url": "https://github.com",
      "timeoutMs": 5000,
      "expectedStatusCode": 200,
      "pollIntervalSeconds": 60,
      "enabled": true
    }
  ]
}
```

### Global Settings

| Setting | Type | Default | Purpose |
|---------|------|---------|---------|
| `showOverlay` | bool | true | Show status overlay |
| `notifyOnChange` | bool | true | Toast notifications on health state changes |

### Per-Target Settings

Each target has its own poll interval and can be individually enabled/disabled.

| Setting | Type | Default | Purpose |
|---------|------|---------|---------|
| `name` | string | — | Display name |
| `type` | string | — | `ping`, `dns`, or `http` |
| `enabled` | bool | true | Whether this target is monitored |
| `pollIntervalSeconds` | int | 30 | How often this target is checked |
| `timeoutMs` | int | 2000 | Max wait before marking unhealthy |

### Type-Specific Fields

| Type | Description | Key Fields |
|------|-------------|------------|
| `ping` | ICMP echo request | `host` (`"auto"` = detect gateway), `thresholdMs` (latency above = degraded) |
| `dns` | DNS name resolution | `host` (DNS server), `resolve` (domain to resolve) |
| `http` | HTTP HEAD request | `url`, `expectedStatusCode` |

## Architecture

- **.NET 8 Self-Contained** — publishes as a single executable, no runtime needed
- **WinForms** — lightest managed UI framework, used only for tray icon and dialogs
- **Per-target async polling** — each target runs on its own async timer independently
- **Live status dialog** — auto-refreshing, auto-sizing, single-instance, selectable text
- **Rolling stats** — per-target 5-minute sliding window with failure tracking
- **Resizable config dialog** — GUI editor for all settings, no JSON editing required
- **~8-12 MB RAM** baseline footprint

## License

MIT License — see [LICENSE](LICENSE) for details.
