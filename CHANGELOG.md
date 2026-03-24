# Changelog

All notable changes to NetHealth will be documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.2.1] - 2026-03-24

### Added
- Auto-detect default gateway from the system network stack
- Set host to `"auto"` on ping targets to use the detected gateway
- Hint label in target editor showing `("auto" = detect gateway)`

## [0.2.0] - 2026-03-24

### Changed
- Poll interval moved from global to per-target (default: 30 seconds)
- Config dialog is now resizable with proper anchored layout
- All numeric fields replaced with plain text inputs (no up/down spinners)
- Target list now shows poll interval and enabled columns
- Each target polls independently on its own async timer
- Double-click a target row to edit it

### Fixed
- Buttons no longer squished at small dialog sizes

## [0.1.0] - 2026-03-24

### Added
- System tray application with color-coded health icon (green/yellow/red/gray)
- Ping monitor — ICMP echo to configured hosts with latency threshold
- DNS monitor — domain name resolution checks with timeout
- HTTP monitor — HEAD requests to URLs with expected status code validation
- JSON-based configuration (`config/targets.json`)
- Configuration dialog accessible from tray context menu (Configure...)
- Target editor with dynamic fields per monitor type (ping/dns/http)
- Toast notifications on health state changes
- Double-click tray icon for detailed status popup
- Single-instance enforcement via mutex
- Self-contained single-file publish support (win-x64)
