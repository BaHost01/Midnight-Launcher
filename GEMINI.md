# Midnight Launcher

Midnight Launcher is a professional, cross-platform Minecraft launcher built with **Avalonia UI** and **.NET 9.0**. It features a modern dark-themed interface, advanced security, and modular service architecture.

## Project Overview

- **Tech Stack:** C#, .NET 9.0, Avalonia UI, CmlLib.Core.
- **Architecture:** Service-oriented design with dedicated logic for Settings (YAML), Security (JWT/Tokens), and UI management.
- **UI Paradigm:** Sidebar-based navigation with a custom notification system and an integrated branding sequence.

## Key Services & Files

### Configuration & Data
- **`Settings.yaml`**: The primary configuration for core launcher requirements (API endpoints, versioning). Handled by `SettingsService.cs`.
- **`config.json`**: UI-specific and user settings (RAM, Themes, Experimental UI toggle).
- **`accounts.json`**: Persistent storage for offline Minecraft sessions.
- **`Tokens.json`**: Encrypted security payload containing HWID and process metadata. Handled by `SecurityService.cs`.

### Core Features
- **Auto-Update:** Checks GitHub Releases on startup, downloads updates to `cache/updates/`, and uses `updater.ps1` for delayed replacement.
- **News & Changelogs:** Native parsing of Mojang's `news.json` with a robust fallback mechanism.
- **Modloaders:** UI skeleton for Forge and Fabric integration via `CmlLib.Core.Installer`.
- **Notifications:** Custom-built transient UI overlays for real-time user feedback.

## Developer Guidelines

### UI Modifications
- **Namespace:** Use `using:WebView.Avalonia` for web content (though currently opening in system browser for stability).
- **Theming:** Default theme is "Midnight". Transitions are handled via `MainWindow.axaml.cs`.
- **Experimental UI:** Scrapped concepts live in `ExperimentalMainWindow.axaml`. Do not use for stable production features.

### Security & Reliability
- **SecurityService:** Should never be a single point of failure. Always wrap in try-catch and notify user of non-critical failures.
- **Logging:** Use the internal `Log()` and `LogError()` methods which write to `MidnightLauncherLogs.txt`.

## Building and Deployment
- **Build:** `dotnet build`
- **Release:** GitHub Actions (`release.yml`) handles automated MSI (WiX 4) and ZIP generation with SHA-256 checksums.
