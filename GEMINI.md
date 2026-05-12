# Midnight Launcher

Midnight Launcher is a professional, cross-platform Minecraft launcher built with **Avalonia UI** and **.NET 9.0**. It features a modern dark-themed interface with sidebar navigation and integrated asset management.

## Features
- **Modern UI:** Sidebar-based navigation for Home, Accounts, and Settings.
- **Loading Screen:** Integrated pulsing loading overlay for data initialization.
- **Custom Assets:** Custom-generated Midnight-themed icons and application icon.
- **Version Selector:** Access to all Minecraft versions from Mojang.
- **Account Management:** Multi-session support with persistent local storage.

## Project Structure
- **/Assets:** Contains SVG source icons and converted PNG/ICO assets.
- **MainWindow.axaml:** Main UI layout with sidebar and content views.
- **accounts.json:** Local storage for saved offline sessions.

## Building and Running
- **Build:** `dotnet build`
- **Run:** `dotnet run`
- **Icon Support:** The app icon is configured in the `.csproj` and will appear in the taskbar and executable properties on supported platforms.


