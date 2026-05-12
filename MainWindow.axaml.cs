using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Midnight_Launcher;

public partial class MainWindow : Window
{
    private readonly MinecraftLauncher _launcher;
    private readonly string _accountsPath = "accounts.json";
    private readonly string _configPath = "config.json";
    private readonly string _logPath = "MidnightLauncherLogs.txt";
    public ObservableCollection<string> Accounts { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        
        Log("Application started.");
        
        var path = new MinecraftPath("./game");
        _launcher = new MinecraftLauncher(path);

        _launcher.FileProgressChanged += (s, e) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusTextBlock.Text = e.Name;
                if (e.TotalTasks > 0)
                {
                    DownloadProgressBar.Value = (double)e.ProgressedTasks / e.TotalTasks * 100;
                }
            });
        };

        AccountComboBox.ItemsSource = Accounts;
        AccountsListBox.ItemsSource = Accounts;
        
        LoadAccounts();
        LoadConfig();
        InitializeLauncher();

        AddAccountButton.Click += AddAccountButton_Click;
        PlayButton.Click += PlayButton_Click;
        RefreshVersionsButton.Click += async (s, e) => await LoadVersions();
        OpenFolderButton.Click += OpenFolderButton_Click;
        NavListBox.SelectionChanged += NavListBox_SelectionChanged;
        
        NavListBox.SelectedIndex = 0;
    }

    private void Log(string message)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(_logPath, logEntry);
        }
        catch { }
    }

    private void LogError(string message, Exception ex)
    {
        Log($"ERROR: {message} | {ex.Message}{Environment.NewLine}{ex.StackTrace}");
    }

    private void LoadConfig()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                // Future config like RAM could be loaded here
            }
            catch (Exception ex)
            {
                LogError("Failed to load config", ex);
            }
        }
    }

    private async void InitializeLauncher()
    {
        LoadingOverlay.IsVisible = true;
        LoadingStatus.Text = "Fetching versions from Mojang...";
        
        await LoadVersions();
        
        LoadingStatus.Text = "Loading accounts...";
        await Task.Delay(200); 
        
        LoadingOverlay.IsVisible = false;
        Log("Data loaded. Showing branding animation.");

        // Show Branding Animation
        BrandingOverlay.IsVisible = true;
        await Task.Delay(3100); // Slightly more than the 3s animation
        BrandingOverlay.IsVisible = false;

        Log("Launcher initialized.");
    }

    private async Task LoadVersions()
    {
        try
        {
            var versions = await _launcher.GetAllVersionsAsync();
            VersionComboBox.ItemsSource = versions.Select(v => v.Name);
            VersionComboBox.SelectedIndex = 0;
            Log("Versions loaded successfully.");
        }
        catch (Exception ex)
        {
            LoadingStatus.Text = $"Error: {ex.Message}";
            LogError("Failed to load versions", ex);
        }
    }

    private void LoadAccounts()
    {
        if (File.Exists(_accountsPath))
        {
            try
            {
                var json = File.ReadAllText(_accountsPath);
                var savedAccounts = JsonSerializer.Deserialize<List<string>>(json);
                if (savedAccounts != null)
                {
                    foreach (var acc in savedAccounts)
                        Accounts.Add(acc);
                }
                Log($"Loaded {Accounts.Count} accounts.");
            }
            catch (Exception ex)
            {
                LogError("Failed to load accounts", ex);
            }
        }

        if (Accounts.Count > 0)
        {
            AccountComboBox.SelectedIndex = 0;
            AccountsListBox.SelectedIndex = 0;
        }
    }

    private void SaveAccounts()
    {
        try
        {
            var json = JsonSerializer.Serialize(Accounts.ToList());
            File.WriteAllText(_accountsPath, json);
        }
        catch (Exception ex)
        {
            LogError("Failed to save accounts", ex);
        }
    }

    private void AddAccountButton_Click(object? sender, RoutedEventArgs e)
    {
        var username = NewUsernameTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(username) && !Accounts.Contains(username))
        {
            Accounts.Add(username);
            SaveAccounts();
            AccountComboBox.SelectedItem = username;
            NewUsernameTextBox.Text = "";
            Log($"Added new account: {username}");
        }
    }

    private void NavListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavListBox.SelectedItem is ListBoxItem item)
        {
            if (item == HomeNav)
            {
                ViewTitle.Text = "Home";
                HomeView.IsVisible = true;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = false;
            }
            else if (item == AccountsNav)
            {
                ViewTitle.Text = "Accounts";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = true;
                SettingsView.IsVisible = false;
            }
            else if (item == SettingsNav)
            {
                ViewTitle.Text = "Settings";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = true;
            }
        }
    }

    private void OpenFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = Path.GetFullPath("./game");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        Log($"Opening game folder: {path}");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Could not open folder: {ex.Message}";
            LogError("Failed to open game folder", ex);
        }
    }

    private async void PlayButton_Click(object? sender, RoutedEventArgs e)
    {
        var version = VersionComboBox.SelectedItem as string;
        var username = AccountComboBox.SelectedItem as string;

        if (string.IsNullOrEmpty(version))
        {
            StatusTextBlock.Text = "Please select a version.";
            return;
        }

        if (string.IsNullOrEmpty(username))
        {
            StatusTextBlock.Text = "Please select an account.";
            return;
        }

        PlayButton.IsEnabled = false;
        StatusTextBlock.Text = $"Starting {version}...";
        Log($"Attempting to launch {version} for user {username}");

        try
        {
            await _launcher.InstallAsync(version);

            var process = await _launcher.BuildProcessAsync(version, new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(username),
                MaximumRamMb = 4096
            });

            process.Start();
            StatusTextBlock.Text = "Game started!";
            Log("Game process started successfully.");
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            LogError("Failed to launch game", ex);
        }
        finally
        {
            PlayButton.IsEnabled = true;
        }
    }
}
