using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using LazyCache;
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
    private readonly IAppCache _cache = new CachingService();
    private readonly string _accountsPath = "accounts.json";
    private readonly string _configPath = "config.json";
    private readonly string _currentVersion = "v1.1.5";
    private readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();
    // private CmlLib.Core.Version.Changelogs? _changelogs;
    public ObservableCollection<string> Accounts { get; } = new();
    public ObservableCollection<NewsItem> News { get; } = new();
    public ObservableCollection<ModItem> Mods { get; } = new();
    public ObservableCollection<string> ChangelogVersions { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        // Load YAML Settings and Generate Encrypted Tokens
        try
        {
            var yamlSettings = SettingsService.Load();
            SecurityService.GenerateTokens(_currentVersion);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to initialize Security tokens", ex);
            ShowNotification("Security Warning", "Could not generate session tokens. Security features may be limited.", true);
        }
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MidnightLauncher");
        LoggingService.Info("Application started.");
        
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
        ChangelogVersionListBox.ItemsSource = ChangelogVersions;
        
        LoadAccounts();
        LoadConfig();
        InitializeLauncher();
        CheckForUpdates();

        AddAccountButton.Click += AddAccountButton_Click;
        PlayButton.Click += PlayButton_Click;
        RefreshVersionsButton.Click += async (s, e) => await LoadVersions();
        OpenFolderButton.Click += OpenFolderButton_Click;
        ChangeFolderButton.Click += ChangeFolderButton_Click;
        SearchModsButton.Click += async (s, e) => await SearchMods();
        InstallForgeButton.Click += async (s, e) => await InstallForge();
        InstallFabricButton.Click += async (s, e) => await InstallFabric();
        RamSlider.PropertyChanged += (s, e) => { if (e.Property.Name == "Value") RamValueText.Text = $"{(int)RamSlider.Value} MB"; };
        ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
        ExperimentalUiToggle.IsCheckedChanged += ExperimentalUiToggle_IsCheckedChanged;
        CloseNewsButton.Click += (s, e) => NewsArticleOverlay.IsVisible = false;
        NewsListBox.SelectionChanged += NewsListBox_SelectionChanged;
        ChangelogVersionListBox.SelectionChanged += ChangelogVersionListBox_SelectionChanged;
        NavListBox.SelectionChanged += NavListBox_SelectionChanged;
        
        NavListBox.SelectedIndex = 0;
    }

    private async void LoadNews()
    {
        try
        {
            var newsItems = await _cache.GetOrAddAsync<List<NewsItem>>("mojang_news", async () =>
            {
                var response = await _httpClient.GetStringAsync("https://launchercontent.mojang.com/news.json");
                var data = Newtonsoft.Json.Linq.JObject.Parse(response);
                var entries = data["entries"];
                
                var list = new List<NewsItem>();
                foreach (var entry in entries ?? Enumerable.Empty<Newtonsoft.Json.Linq.JToken>())
                {
                    list.Add(new NewsItem
                    {
                        Title = entry["title"]?.ToString() ?? "",
                        Date = entry["date"]?.ToString() ?? "",
                        Summary = entry["text"]?.ToString() ?? "",
                        ImageUrl = "https://launchercontent.mojang.com" + entry["playPageImage"]?["url"]?.ToString(),
                        Url = entry["readMoreLink"]?.ToString() ?? ""
                    });
                }
                return list;
            }, TimeSpan.FromMinutes(30));

            News.Clear();
            foreach (var item in newsItems) News.Add(item);
            LoggingService.Info("News loaded successfully (cached).");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load news", ex);
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
                LoggingService.Error("Failed to open news URL", ex);
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
            LoggingService.Error("Mod search failed", ex);
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

    private async void LoadChangelogs()
    {
        try
        {
            // Fallback: Using Mojang News JSON as requested
            LoggingService.Info("Fetching changelogs fallback from Mojang News.");
            var response = await _httpClient.GetStringAsync("https://launchercontent.mojang.com/news.json");
            var data = Newtonsoft.Json.Linq.JObject.Parse(response);
            var entries = data["entries"];

            ChangelogVersions.Clear();
            foreach (var entry in entries ?? Enumerable.Empty<Newtonsoft.Json.Linq.JToken>())
            {
                var title = entry["title"]?.ToString() ?? "Update";
                ChangelogVersions.Add(title);
            }

            if (ChangelogVersions.Count == 0)
            {
                ChangelogContentText.Text = "No changelogs found in fallback feed.";
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load changelogs fallback", ex);
            ChangelogContentText.Text = "Failed to load changelogs from all sources.";
        }
    }

    private async void ChangelogVersionListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ChangelogVersionListBox.SelectedItem is string title)
        {
            try
            {
                // Find the news entry that matches this title
                var response = await _httpClient.GetStringAsync("https://launchercontent.mojang.com/news.json");
                var data = Newtonsoft.Json.Linq.JObject.Parse(response);
                var entry = data["entries"]?.FirstOrDefault(en => en["title"]?.ToString() == title);
                
                if (entry != null)
                {
                    var text = entry["text"]?.ToString() ?? "No content available.";
                    var date = entry["date"]?.ToString() ?? "Unknown Date";
                    ChangelogContentText.Text = $"Date: {date}\n\n{text}";
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to fetch changelog detail", ex);
            }
        }
    }

    private async Task InstallForge()
    {
        var version = ModloaderVersionComboBox.SelectedItem as string;
        if (string.IsNullOrEmpty(version)) { ShowNotification("Error", "Select a version first", true); return; }

        ShowNotification("Forge", $"Forge installation for {version} is coming soon!");
        /*
        try
        {
            var forge = new CmlLib.Core.Installer.Forge.MForge(_launcher);
            ShowNotification("Forge", "Forge integration initialized. Select the forge version in Home after it finishes.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Forge installation failed", ex);
            ShowNotification("Error", "Forge installation failed", true);
        }
        */
        await Task.CompletedTask;
    }

    private async Task InstallFabric()
    {
        ShowNotification("Fabric", "Fabric installation is coming soon!");
        await Task.CompletedTask;
    }

    private void ThemeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Content?.ToString();
            LoggingService.Info($"Switching theme to {theme}");
            // Theme logic: In a real app, we'd update App.Current.RequestedThemeVariant
        }
    }

    private async void ChangeFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Game Folder",
            AllowMultiple = false
        });

        if (folders != null && folders.Count > 0)
        {
            var result = folders[0].Path.LocalPath;
            LoggingService.Info($"Changing game folder to {result}");
            ShowNotification("Path Changed", "Please restart the launcher to apply the new path.");
        }
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
                LoggingService.Error("Failed to load config", ex);
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
                LoggingService.Info($"New update available: {latestVersion}");
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
            LoggingService.Error("Update check failed", ex);
        }
    }

    private async Task DownloadUpdate(string url)
    {
        try
        {
            var cacheDir = Path.Combine(Environment.CurrentDirectory, "cache", "updates");
            if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);
            Directory.CreateDirectory(cacheDir);

            var zipPath = Path.Combine(cacheDir, "update.zip");
            
            LoggingService.Info($"Downloading update from {url}");
            var data = await _httpClient.GetByteArrayAsync(url);
            
            if (data == null || data.Length < 1000) 
                throw new Exception("Downloaded update file is too small or invalid.");

            await File.WriteAllBytesAsync(zipPath, data);
            
            LoggingService.Info("Update downloaded and verified. Preparing updater script.");
            ShowNotification("Update Ready", "The launcher will update automatically on next restart.");
            PrepareUpdater();
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to download update", ex);
            ShowNotification("Update Failed", "Could not download the latest update.", true);
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
        LoggingService.Info("Updater script prepared.");
    }

    private async void InitializeLauncher()
    {
        try
        {
            LoadingOverlay.IsVisible = true;
            LoadingStatus.Text = "Fetching versions from Mojang...";

            await LoadVersions();

            LoadingStatus.Text = "Loading accounts...";
            await Task.Delay(200);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Initialization error", ex);
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }

        try
        {
            LoggingService.Info("Data loaded. Showing branding animation.");

            // Show Branding Animation
            BrandingOverlay.IsVisible = true;
            await Task.Delay(3100); // Slightly more than the 3s animation
        }
        catch (Exception ex)
        {
            LoggingService.Error("Branding animation error", ex);
        }
        finally
        {
            BrandingOverlay.IsVisible = false;
            LoggingService.Info("Launcher initialized.");
        }
    }

    private async Task LoadVersions()
    {
        try
        {
            var versions = await _cache.GetOrAddAsync("mc_versions", 
                async () => await _launcher.GetAllVersionsAsync(), 
                TimeSpan.FromMinutes(10));

            var versionNames = versions.Select(v => v.Name).ToList();
            VersionComboBox.ItemsSource = versionNames;
            if (versionNames.Count > 0)
                VersionComboBox.SelectedIndex = 0;
            
            LoggingService.Info("Versions loaded successfully (cached).");
        }
        catch (Exception ex)
        {
            LoadingStatus.Text = $"Error: {ex.Message}";
            LoggingService.Error("Failed to load versions", ex);
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
                LoggingService.Info($"Loaded {Accounts.Count} accounts.");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to load accounts", ex);
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
            LoggingService.Error("Failed to save accounts", ex);
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
            LoggingService.Info($"Added new account: {username}");
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
                LoggingService.Error("Failed to switch to experimental UI", ex);
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
                ModloadersView.IsVisible = false;
                ChangelogsView.IsVisible = false;
            }
            else if (item == AccountsNav)
            {
                ViewTitle.Text = "Accounts";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = true;
                SettingsView.IsVisible = false;
                NewsView.IsVisible = false;
                ModsView.IsVisible = false;
                ModloadersView.IsVisible = false;
                ChangelogsView.IsVisible = false;
            }
            else if (item == NewsNav)
            {
                ViewTitle.Text = "News";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = false;
                NewsView.IsVisible = true;
                ModsView.IsVisible = false;
                ModloadersView.IsVisible = false;
                ChangelogsView.IsVisible = false;
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
                ModloadersView.IsVisible = false;
                ChangelogsView.IsVisible = false;
            }
            else if (item == ModloadersNav)
            {
                ViewTitle.Text = "Modloaders";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = false;
                NewsView.IsVisible = false;
                ModsView.IsVisible = false;
                ModloadersView.IsVisible = true;
                ChangelogsView.IsVisible = false;
                ModloaderVersionComboBox.ItemsSource = VersionComboBox.ItemsSource;
            }
            else if (item == ChangelogsNav)
            {
                ViewTitle.Text = "Changelogs";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = false;
                NewsView.IsVisible = false;
                ModsView.IsVisible = false;
                ModloadersView.IsVisible = false;
                ChangelogsView.IsVisible = true;
                if (ChangelogVersions.Count == 0) LoadChangelogs();
            }
            else if (item == SettingsNav)
            {
                ViewTitle.Text = "Settings";
                HomeView.IsVisible = false;
                AccountsView.IsVisible = false;
                SettingsView.IsVisible = true;
                NewsView.IsVisible = false;
                ModsView.IsVisible = false;
                ModloadersView.IsVisible = false;
                ChangelogsView.IsVisible = false;
            }
        }
    }

    private void OpenFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = Path.GetFullPath("./game");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        LoggingService.Info($"Opening game folder: {path}");
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
            LoggingService.Error("Failed to open game folder", ex);
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
        LoggingService.Info($"Attempting to launch {version} for user {username}");

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
            LoggingService.Info("Game process started successfully.");

            if (File.Exists("updater.ps1"))
            {
                LoggingService.Info("Update pending. Starting updater and exiting.");
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
            LoggingService.Error("Failed to launch game", ex);
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
            LoggingService.Info("Update pending. Starting updater on close.");
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
