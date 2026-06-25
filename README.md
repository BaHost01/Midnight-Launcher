# 🌙 Midnight Launcher

Midnight Launcher is a lightweight, high-performance, and cross-platform Minecraft launcher built with **C#**, **.NET 9.0**, and **Avalonia UI**. It offers a modern interface, advanced security features, and seamless modding support.

## ✨ Key Features

- **🚀 Polished UI:** Sidebar navigation, translucent panels, and a focused dashboard.
- **🛠️ Launcher Shortcuts:** Quick links for Forge and Fabric installer resources.
- **🔄 Update Checks:** GitHub Releases detection with optional staged downloads.
- **📰 News & Updates:** Mojang news feed and integrated changelog browsing.
- **🧩 Mod Discovery:** Search and open Modrinth projects from inside the launcher.
- **⚙️ Deep Customization:** Flexible RAM allocation, custom game paths, and theme switching.

## 🚀 Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/BaHost01/Midnight-Launcher.git
   ```
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run the launcher:
   ```bash
   dotnet run
   ```

Launcher data is stored in the current user's app-data directory, not beside the executable.

## 🛠️ Development

- **Tech Stack:** Avalonia UI, CmlLib.Core, YamlDotNet, Newtonsoft.Json.
- **CI/CD:** Automated builds for Windows and Linux via GitHub Actions.
- **Installer:** Windows `.msi` support via WiX Toolset.

## 🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request. The experimental UI remains opt-in from Settings.

## 📜 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---
*Made with Love by CStudioss.*
