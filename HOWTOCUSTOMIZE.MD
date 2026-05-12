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
By default, the launcher allocates 4GB of RAM. You can change this in `MainWindow.axaml.cs` inside the `PlayButton_Click` method:
```csharp
var process = await _launcher.BuildProcessAsync(version, new MLaunchOption
{
    Session = MSession.CreateOfflineSession(username),
    MaximumRamMb = 8192 // Change to your desired RAM in MB
});
```

### Game Path
The game files are stored in the `./game` directory. You can change this by modifying the `MinecraftPath` in `MainWindow.axaml.cs`:
```csharp
var path = new MinecraftPath("C:\\Your\\Custom\\Path");
```

## Adding Features
Midnight Launcher uses `CmlLib.Core`. You can refer to the [CmlLib.Core Documentation](https://github.com/CmlLib/CmlLib.Core/wiki) to add features like:
- Modpack support (Forge, Fabric, Quilt).
- Microsoft/Xbox Live Authentication.
- Custom JVM arguments.
- Server auto-connect.

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

