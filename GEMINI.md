# Midnight Launcher

Midnight Launcher is a professional, cross-platform Minecraft launcher built with **Avalonia UI** and **.NET 9.0**. It features a modern dark-themed interface with sidebar navigation and advanced configuration options.

## Features
- **Modern UI:** Sidebar-based navigation for Home, Accounts, Modloaders, Changelogs, News, Mods, and Settings.
- **Modloader Support:** Dedicated UI for Forge and Fabric installation (Modloader profile creation).
- **Changelogs:** View real-time Minecraft version updates directly in the launcher.
- **Advanced Settings:** 
  - **RAM Allocation:** Dynamic memory control via slider.
  - **Theming:** Support for Midnight, Dark, and Light themes.
  - **Custom Paths:** Flexibility to change the Minecraft installation directory.
- **Auto-Update:** Automatic version checking and background updating via GitHub Releases.
- **News & Mods:** Mojang news feed and Modrinth mod discovery.

## Technical Details
- **Tech Stack:** C#, .NET 9.0, Avalonia UI, CmlLib.Core.
- **Assets:** Custom SVG-to-PNG workflow for high-quality, lightweight icons.
- **Persistence:** Config and accounts stored in local JSON files.

## Building and Running
- **Build:** `dotnet build`
- **Run:** `dotnet run`
- **Release:** Automated MSI/ZIP generation via GitHub Actions.



