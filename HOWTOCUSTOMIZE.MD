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
