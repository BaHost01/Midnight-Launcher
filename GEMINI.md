# Midnight Launcher

Midnight Launcher is a professional, cross-platform Minecraft launcher built with **Avalonia UI** and **.NET 9.0**. It features a modern dark-themed interface, advanced security, and modular service architecture.

## Project Overview

- **Tech Stack:** C#, .NET 9.0, Avalonia UI, CmlLib.Core.
- **Architecture:** Service-oriented design with dedicated logic for settings, security tokens, launcher state, and UI management.
- **UI Paradigm:** Sidebar-based navigation with a notification system, dashboard home view, and alternate experimental window.

## Key Services & Files

### Configuration & Data
- **`Settings.yaml`**: The primary configuration for core launcher requirements (API endpoints, versioning). Handled by `SettingsService.cs`.
- **`config.json`**: UI-specific and user settings (RAM, themes, experimental UI toggle). Managed by `ConfigService.cs` with per-user storage.
- **`accounts.json`**: Persistent storage for offline Minecraft sessions. Initialized automatically on first run.
- **`Tokens.json`**: Local security payload containing HWID and process metadata. Handled by `SecurityService.cs`.

### Core Features
- **Auto-Update:** Checks GitHub Releases and can stage downloads for later application on Windows.
- **News & Changelogs:** Native parsing of Mojang's `news.json` feed for both news and changelog views.
- **Modloaders:** Shortcut buttons for Forge and Fabric installer resources.
- **Notifications:** Custom-built transient UI overlays for real-time user feedback.

## Developer Guidelines

### UI Modifications
- **Aesthetic:** Modern "Glassmorphism" look using `AcrylicBlur` and semi-transparent layers.
- **Theming:** Supports "Midnight", "Dark", and "Light" variants via `Application.Current.RequestedThemeVariant`.
- **Experimental UI:** Use `ExperimentalMainWindow.axaml` for the alternate concept window. Stable modern UI is in `MainWindow.axaml`.

### Security & Reliability
- **SecurityService:** Should never be a single point of failure. Always wrap in try-catch and notify user of non-critical failures.
- **Logging:** Use `LoggingService` for file-backed logs stored in the user data directory.

## Building and Deployment
- **Build:** `dotnet build`
- **Release:** GitHub Actions (`release.yml`) handles automated MSI (WiX 4) and ZIP generation with SHA-256 checksums.
