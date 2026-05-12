using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
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
    private readonly string _currentVersion = "v1.0.6";
    private readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();
    public ObservableCollection<string> Accounts { get; } = new();
    public ObservableCollection<NewsItem> News { get; } = new();
    public ObservableCollection<ModItem> Mods { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MidnightLauncher");
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
        NewsListBox.ItemsSource = News;
        ModsListBox.ItemsSource = Mods;
        
        LoadAccounts();
        LoadConfig();
        InitializeLauncher();
        CheckForUpdates();

        AddAccountButton.Click += AddAccountButton_Click;
        PlayButton.Click += PlayButton_Click;
        RefreshVersionsButton.Click += async (s, e) => await LoadVersions();
        OpenFolderButton.Click += OpenFolderButton_Click;
        SearchModsButton.Click += async (s, e) => await SearchMods();
        ExperimentalUiToggle.IsCheckedChanged += ExperimentalUiToggle_IsCheckedChanged;
        CloseNewsButton.Click += (s, e) => NewsArticleOverlay.IsVisible = false;
        NewsListBox.SelectionChanged += NewsListBox_SelectionChanged;
        NavListBox.SelectionChanged += NavListBox_SelectionChanged;
        
        NavListBox.SelectedIndex = 0;
    }

    private async void LoadNews()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://launchercontent.mojang.com/news.json");
            var data = Newtonsoft.Json.Linq.JObject.Parse(response);
            var entries = data["entries"];
            
            News.Clear();
            foreach (var entry in entries ?? Enumerable.Empty<Newtonsoft.Json.Linq.JToken>())
            {
                News.Add(new NewsItem
                {
                    Title = entry["title"]?.ToString() ?? "",
                    Date = entry["date"]?.ToString() ?? "",
                    Summary = entry["text"]?.ToString() ?? "",
                    ImageUrl = "https://launchercontent.mojang.com" + entry["playPageImage"]?["url"]?.ToString(),
                    Url = entry["readMoreLink"]?.ToString() ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            LogError("Failed to load news", ex);
        }
    }

    private void NewsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NewsListBox.SelectedItem is NewsItem item)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = item.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogError("Failed to open news URL", ex);
            }
            NewsListBox.SelectedItem = null;
        }
    }

    private async Task SearchMods()
    {
        var query = ModSearchTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        try
        {
            var response = await _httpClient.GetStringAsync($"https://api.modrinth.com/v2/search?query={query}");
            var data = Newtonsoft.Json.Linq.JObject.Parse(response);
            var hits = data["hits"];

            Mods.Clear();
            foreach (var hit in hits ?? Enumerable.Empty<Newtonsoft.Json.Linq.JToken>())
            {
                Mods.Add(new ModItem
                {
                    Title = hit["title"]?.ToString() ?? "",
                    Description = hit["description"]?.ToString() ?? "",
                    IconUrl = hit["icon_url"]?.ToString() ?? "",
                    Id = hit["project_id"]?.ToString() ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            LogError("Mod search failed", ex);
        }
    }

    private void ShowNotification(string title, string message, bool isError = false)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var notification = new Border
            {
                Background = new SolidColorBrush(Color.Parse(isError ? "#C62828" : "#2A2A2A")),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(8),
                BoxShadow = new BoxShadows(new BoxShadow { Blur = 10, Color = Color.FromArgb(128, 0, 0, 0) }),
                Child = new StackPanel
                {
                    Spacing = 5,
                    Children =
                    {
                        new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 14 },
                        new TextBlock { Text = message, FontSize = 12, TextWrapping = TextWrapping.Wrap, Opacity = 0.8 }
                    }
                }
            };

            NotificationArea.Children.Add(notification);

            // Auto-remove after 5 seconds
            Task.Delay(5000).ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => NotificationArea.Children.Remove(notification)));
        });
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

    private async void CheckForUpdates()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://api.github.com/repos/BaHost01/Midnight-Launcher/releases/latest");
            var release = Newtonsoft.Json.Linq.JToken.Parse(response);
            var latestVersion = release["tag_name"]?.ToString();

            if (!string.IsNullOrEmpty(latestVersion) && latestVersion != _currentVersion)
            {
                Log($"New update available: {latestVersion}");
                ShowNotification("Update Available", $"Midnight Launcher {latestVersion} is ready to install.");
                var asset = release["assets"]?.FirstOrDefault(a => a["name"]?.ToString().EndsWith(".zip") == true);
                if (asset != null)
                {
                    var downloadUrl = asset["browser_download_url"]?.ToString();
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        await DownloadUpdate(downloadUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogError("Update check failed", ex);
        }
    }

    private async Task DownloadUpdate(string url)
    {
        try
        {
            var cacheDir = Path.Combine(Environment.CurrentDirectory, "cache", "updates");
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            var zipPath = Path.Combine(cacheDir, "update.zip");
            var data = await _httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(zipPath, data);
            
            Log("Update downloaded to cache. Preparing updater script.");
            ShowNotification("Update Downloaded", "The update will be applied when the launcher closes.");
            PrepareUpdater();
        }
        catch (Exception ex)
        {
            LogError("Failed to download update", ex);
        }
    }

    private void PrepareUpdater()
    {
        var psScript = @"
Start-Sleep -Seconds 2
$zipPath = '.\cache\updates\update.zip'
$destPath = '.\'
Expand-Archive -Path $zipPath -DestinationPath $destPath -Force
Remove-Item $zipPath
Start-Process '.\Midnight-Launcher.exe'
";
        File.WriteAllText("updater.ps1", psScript);
        Log("Updater script prepared.");
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

    private void ExperimentalUiToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ExperimentalUiToggle.IsChecked == true)
        {
            try
            {
                var config = new { ExperimentalUi = true };
                File.WriteAllText(_configPath, Newtonsoft.Json.JsonConvert.SerializeObject(config));
                
                var exp = new ExperimentalMainWindow();
                exp.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                LogError("Failed to switch to experimental UI", ex);
            }
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
                NewsView.IsVisible = false;
                ModsView.IsVisible = false;
            }
            else if (item == AccountsNav)
            {
                ViewTitle.Text = "Accounts";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = true;
                SettingsView.IsVisible = false;
                NewsView.IsVisible = false;
                ModsView.IsVisible = false;
            }
            else if (item == NewsNav)
            {
                ViewTitle.Text = "News";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = false;
                NewsView.IsVisible = true;
                ModsView.IsVisible = false;
                if (News.Count == 0) LoadNews();
            }
            else if (item == ModsNav)
            {
                ViewTitle.Text = "Mods";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = false;
                NewsView.IsVisible = false;
                ModsView.IsVisible = true;
            }
            else if (item == SettingsNav)
            {
                ViewTitle.Text = "Settings";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = true;
                NewsView.IsVisible = false;
                ModsView.IsVisible = false;
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

            if (File.Exists("updater.ps1"))
            {
                Log("Update pending. Starting updater and exiting.");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-ExecutionPolicy Bypass -File updater.ps1",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                Environment.Exit(0);
            }
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

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (File.Exists("updater.ps1"))
        {
            Log("Update pending. Starting updater on close.");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-ExecutionPolicy Bypass -File updater.ps1",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        base.OnClosing(e);
    }
}

public class NewsItem
{
    public string Title { get; set; } = "";
    public string Date { get; set; } = "";
    public string Summary { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Url { get; set; } = "";
}

public class ModItem
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public string Id { get; set; } = "";
}
