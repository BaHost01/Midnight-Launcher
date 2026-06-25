# How to Customize Midnight Launcher

This guide explains how you can modify and customize Midnight Launcher for your own needs.

## UI Theming
Midnight Launcher uses **Avalonia UI** with the **Fluent Theme**.
- To change the theme variant (e.g., force Light or Dark), modify `App.axaml`:
  ```xml
  RequestedThemeVariant="Dark"
  ```
- To customize colors, you can add `Style` definitions in `MainWindow.axaml` or `App.axaml`.

## Game Settings
### Memory Allocation
The launcher includes a slider in the **Settings** menu to adjust RAM allocation. These settings are persisted in the per-user launcher config file. You can also manually change the default in `ConfigService.cs`:
```csharp
public int SelectedRam { get; set; } = 4096;
```

### Game Path
The game files are stored in the launcher data directory by default. You can change this via the **Settings** menu in the UI or by modifying `LauncherConfig.GamePath` in `ConfigService.cs`.

## Configuration Files
Midnight Launcher uses four main files for persistence:
1. **`accounts.json`**: Stores your offline usernames.
2. **`Settings.yaml`**: Advanced launcher settings (update channels, API endpoints).
3. **`config.json`**: User preferences like RAM allocation, UI theme, and custom game paths.
4. **`Tokens.json`**: Local security tokens containing HWID and session metadata.

## Security Features
The launcher generates a unique **HWID** (Hardware ID) to secure your local tokens. If you copy the launcher to a different PC, the `Tokens.json` will need to be regenerated to ensure security.

## Antivirus & False Positives
Since Midnight Launcher is a specialized tool, it might occasionally be flagged by Antivirus (AV) software as a "False Positive."

### How we minimize flags:
1. **Metadata:** Every build includes Company (CStudioss) and Description metadata.
2. **Checksums:** Every GitHub Release includes a `checksums.txt` with SHA-256 hashes for integrity verification.
3. **Open Source:** The code is transparent and auditable.

### Proactive Whitelisting:
To permanently clear flags for your users, you should submit the `.exe` or `.msi` to major AV vendors:
- **Microsoft Security Intelligence:** [Submit Here](https://www.microsoft.com/en-us/wdsi/filesubmission) (Select "Developer" and "Incorrectly detected as malware").
- **VirusTotal:** Upload your file to [VirusTotal](https://www.virustotal.com/) to see which engines flag it and use their contact forms to report false positives.
- **Norton/Avast/etc:** Each major vendor has a "False Positive Submission" form on their website.
