# Changelog

All notable changes to NetHealth will be documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.3.1] - 2026-03-24

### Added
- Application icon (`.ico`) for exe and taskbar display
- GitHub Actions release workflow: push a `v*` tag to auto-publish a GitHub Release with the single-file exe

### Changed
- Version bumped to 0.3.0 in project file (was still 0.1.0)

## [0.3.0] - 2026-03-24

### Added
- Live-updating status dialog (refreshes every 2 seconds while open)
- Rolling statistics per target: last latency, 5-minute average, last failure time
- IP/host/URL shown in brackets next to each target name in status dialog
- Local time displayed alongside UTC for all timestamps
- Selectable text in status dialog (read-only TextBox)
- Auto-sizing status dialog: fits content exactly, scrollbar only above 900px
- Single-instance status window: double-click activates existing window
- "Pin to Taskbar..." tray menu item opens Windows Settings for tray icon pinning

### Changed
- Status dialog replaces static MessageBox with proper live Form
- Config dialog buttons widened 15% (90→104px) with arrow labels (Move ˄ / Move ˅)
- Config dialog widened to accommodate larger buttons
- Target edit dialog reduced 20% in height with repositioned buttons
- DNS resolution detail now line-wraps after arrow for long IPv6 addresses
- OK button in status dialog properly closes the non-modal window

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
