using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using LazyCache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Midnight_Launcher;

public partial class MainWindow : Window
{
    private readonly IAppCache _cache = new CachingService();
    private readonly HttpClient _httpClient = new();
    private readonly string _currentVersion = "v1.1.9";
    private readonly string _accountsPath = LauncherPaths.AccountsPath;

    private LauncherConfig _config = new();
    private MinecraftLauncher _launcher = null!;

    private List<NewsItem> _newsFeed = new();

    public ObservableCollection<string> Accounts { get; } = new();
    public ObservableCollection<NewsItem> News { get; } = new();
    public ObservableCollection<ModItem> Mods { get; } = new();
    public ObservableCollection<string> ChangelogVersions { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ConfigureEventHandlers();
        ConfigureBindings();
        _ = InitializeAsync();
    }

    private void ConfigureBindings()
    {
        AccountComboBox.ItemsSource = Accounts;
        AccountsListBox.ItemsSource = Accounts;
        NewsListBox.ItemsSource = News;
        ModsListBox.ItemsSource = Mods;
        ChangelogVersionListBox.ItemsSource = ChangelogVersions;
    }

    private void ConfigureEventHandlers()
    {
        AddAccountButton.Click += AddAccountButton_Click;
        PlayButton.Click += PlayButton_Click;
        RefreshVersionsButton.Click += async (_, _) => await LoadVersionsAsync();
        OpenFolderButton.Click += OpenFolderButton_Click;
        ChangeFolderButton.Click += ChangeFolderButton_Click;
        SearchModsButton.Click += async (_, _) => await SearchModsAsync();
        InstallForgeButton.Click += async (_, _) => await InstallForgeAsync();
        InstallFabricButton.Click += async (_, _) => await InstallFabricAsync();
        ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
        ExperimentalUiToggle.IsCheckedChanged += ExperimentalUiToggle_IsCheckedChanged;
        NewsListBox.SelectionChanged += NewsListBox_SelectionChanged;
        ModsListBox.SelectionChanged += ModsListBox_SelectionChanged;
        ChangelogVersionListBox.SelectionChanged += ChangelogVersionListBox_SelectionChanged;
        NavListBox.SelectionChanged += NavListBox_SelectionChanged;
        CheckUpdatesButton.Click += async (_, _) =>
        {
            ShowNotification("Update Service", "Checking for the latest launcher release...");
            await CheckForUpdatesAsync(downloadIfNewer: true, silent: false);
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MidnightLauncher/1.2");
    }

    private async Task InitializeAsync()
    {
        try
        {
            LoadingOverlay.IsVisible = true;
            BrandingOverlay.IsVisible = false;
            LoadingStatus.Text = "Loading launcher data...";

            _config = ConfigService.Load();
            EnsureRequiredDirectories();
            ConfigureLauncher(_config.GamePath);
            ApplyTheme(_config.Theme);

            try
            {
                _ = SettingsService.Load();
                SecurityService.GenerateTokens(_currentVersion);
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to initialize optional security settings", ex);
                ShowNotification("Security Warning", "Local security tokens could not be refreshed.", true);
            }

            _launcher.FileProgressChanged += (_, e) =>
                Dispatcher.UIThread.Post(() =>
                {
                    StatusTextBlock.Text = e.Name;
                    if (e.TotalTasks > 0)
                        DownloadProgressBar.Value = (double)e.ProgressedTasks / e.TotalTasks * 100;
                });

            LoadAccounts();
            await LoadVersionsAsync();
            ShowView(LauncherView.Home);

            RamSlider.Value = _config.SelectedRam;
            RamValueText.Text = $"{_config.SelectedRam} MB";
            LauncherVersionText.Text = $"{_currentVersion} Stable";

            NavListBox.SelectedIndex = 0;

            LoadingStatus.Text = "Ready.";
            await Task.Delay(150);

            BrandingOverlay.IsVisible = true;
            await Task.Delay(700);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Launcher initialization failed", ex);
            StatusTextBlock.Text = "Launcher initialization failed.";
            ShowNotification("Startup Error", "The launcher could not finish loading.", true);
        }
        finally
        {
            BrandingOverlay.IsVisible = false;
            LoadingOverlay.IsVisible = false;
            LoggingService.Info($"Application started. Version: {_currentVersion}");
            _ = CheckForUpdatesAsync(downloadIfNewer: false, silent: true);
        }
    }

    private void EnsureRequiredDirectories()
    {
        try
        {
            LauncherPaths.Ensure();
            if (!string.IsNullOrWhiteSpace(_config.GamePath))
                Directory.CreateDirectory(_config.GamePath);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to create required directories", ex);
        }
    }

    private void ConfigureLauncher(string gamePath)
    {
        _launcher = new MinecraftLauncher(new MinecraftPath(gamePath));
    }

    private void ApplyTheme(string theme)
    {
        if (Application.Current == null)
            return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Dark
        };

        var selectedItem = ThemeComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Content?.ToString(), theme, StringComparison.OrdinalIgnoreCase));

        if (selectedItem != null)
            ThemeComboBox.SelectedItem = selectedItem;
    }

    private async Task LoadVersionsAsync()
    {
        try
        {
            LoadingStatus.Text = "Loading Minecraft versions...";

            var cacheKey = $"mc_versions:{_config.GamePath}";
            var versions = await _cache.GetOrAddAsync(
                cacheKey,
                async () => await _launcher.GetAllVersionsAsync(),
                TimeSpan.FromMinutes(10));

            var displayNames = versions
                .Select(version => version.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            VersionComboBox.ItemsSource = displayNames;
            ModloaderVersionComboBox.ItemsSource = displayNames;

            if (displayNames.Count > 0)
            {
                VersionComboBox.SelectedIndex = 0;
                ModloaderVersionComboBox.SelectedIndex = 0;
            }

            StatusTextBlock.Text = displayNames.Count > 0
                ? $"Loaded {displayNames.Count} version(s)."
                : "No Minecraft versions were found.";
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load versions", ex);
            StatusTextBlock.Text = $"Version load failed: {ex.Message}";
        }
    }

    private void LoadAccounts()
    {
        Accounts.Clear();

        if (File.Exists(_accountsPath))
        {
            try
            {
                var json = File.ReadAllText(_accountsPath);
                var savedAccounts = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                foreach (var account in savedAccounts.Where(a => !string.IsNullOrWhiteSpace(a)))
                    Accounts.Add(account);

                LoggingService.Info($"Loaded {Accounts.Count} account(s).");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to load accounts", ex);
            }
        }
        else
        {
            SaveAccounts();
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
            LauncherPaths.Ensure();
            var json = System.Text.Json.JsonSerializer.Serialize(Accounts.ToList());
            File.WriteAllText(_accountsPath, json);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to save accounts", ex);
        }
    }

    private async Task<List<NewsItem>> GetMojangFeedAsync()
    {
        return await _cache.GetOrAddAsync(
            "mojang_news",
            async () =>
            {
                var response = await _httpClient.GetStringAsync("https://launchercontent.mojang.com/news.json");
                var data = JObject.Parse(response);
                var entries = data["entries"] ?? Enumerable.Empty<JToken>();

                var list = new List<NewsItem>();
                foreach (var entry in entries)
                {
                    list.Add(new NewsItem
                    {
                        Title = entry["title"]?.ToString() ?? string.Empty,
                        Date = entry["date"]?.ToString() ?? string.Empty,
                        Summary = entry["text"]?.ToString() ?? string.Empty,
                        ImageUrl = "https://launchercontent.mojang.com" + entry["playPageImage"]?["url"]?.ToString(),
                        Url = entry["readMoreLink"]?.ToString() ?? string.Empty
                    });
                }

                return list;
            },
            TimeSpan.FromMinutes(30));
    }

    private async Task LoadNewsAsync()
    {
        try
        {
            _newsFeed = await GetMojangFeedAsync();

            News.Clear();
            foreach (var item in _newsFeed)
                News.Add(item);

            if (News.Count == 0)
                StatusTextBlock.Text = "No news entries were returned.";
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load news", ex);
            StatusTextBlock.Text = "News feed unavailable.";
        }
    }

    private async Task LoadChangelogAsync()
    {
        try
        {
            if (_newsFeed.Count == 0)
                _newsFeed = await GetMojangFeedAsync();

            ChangelogVersions.Clear();
            foreach (var item in _newsFeed.Where(item => !string.IsNullOrWhiteSpace(item.Title)))
                ChangelogVersions.Add(item.Title);

            if (ChangelogVersions.Count == 0)
                ChangelogContentText.Text = "No changelogs were found in the Mojang feed.";
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load changelog list", ex);
            ChangelogContentText.Text = "Failed to load changelog data.";
        }
    }

    private async Task SearchModsAsync()
    {
        var query = ModSearchTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            StatusTextBlock.Text = "Type a mod search term first.";
            return;
        }

        try
        {
            StatusTextBlock.Text = $"Searching Modrinth for '{query}'...";
            var response = await _httpClient.GetStringAsync($"https://api.modrinth.com/v2/search?query={Uri.EscapeDataString(query)}&limit=20");
            var data = JObject.Parse(response);
            var hits = data["hits"] ?? Enumerable.Empty<JToken>();

            Mods.Clear();
            foreach (var hit in hits)
            {
                Mods.Add(new ModItem
                {
                    Title = hit["title"]?.ToString() ?? string.Empty,
                    Description = hit["description"]?.ToString() ?? string.Empty,
                    IconUrl = hit["icon_url"]?.ToString() ?? string.Empty,
                    Id = hit["project_id"]?.ToString() ?? string.Empty
                });
            }

            StatusTextBlock.Text = Mods.Count > 0
                ? $"Found {Mods.Count} Modrinth result(s)."
                : "No mods matched the search.";
        }
        catch (Exception ex)
        {
            LoggingService.Error("Mod search failed", ex);
            StatusTextBlock.Text = "Mod search failed.";
        }
    }

    private async Task InstallForgeAsync()
    {
        var version = ModloaderVersionComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(version))
        {
            ShowNotification("Forge", "Select a target Minecraft version first.", true);
            return;
        }

        ShowNotification("Forge", $"Opening Forge resources for {version}.");
        await OpenUrlAsync("https://files.minecraftforge.net/");
    }

    private async Task InstallFabricAsync()
    {
        var version = ModloaderVersionComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(version))
        {
            ShowNotification("Fabric", "Select a target Minecraft version first.", true);
            return;
        }

        ShowNotification("Fabric", $"Opening Fabric installer resources for {version}.");
        await OpenUrlAsync("https://fabricmc.net/use/installer/");
    }

    private async Task OpenUrlAsync(string url)
    {
        try
        {
            await Task.Run(() =>
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                }));
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to open URL", ex);
            ShowNotification("Open Link Failed", "The launcher could not open your browser.", true);
        }
    }

    private void ShowNotification(string title, string message, bool isError = false)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var notification = new Border
            {
                Background = new SolidColorBrush(Color.Parse(isError ? "#B3261E" : "#202020")),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(10),
                BoxShadow = new BoxShadows(new BoxShadow { Blur = 10, Color = Color.FromArgb(96, 0, 0, 0) }),
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

            _ = Task.Delay(5000).ContinueWith(_ =>
                Dispatcher.UIThread.Post(() => NotificationArea.Children.Remove(notification)));
        });
    }

    private async Task CheckForUpdatesAsync(bool downloadIfNewer, bool silent)
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://api.github.com/repos/BaHost01/Midnight-Launcher/releases/latest");
            var release = JObject.Parse(response);
            var latestTag = release["tag_name"]?.ToString();

            if (string.IsNullOrWhiteSpace(latestTag))
                return;

            if (!TryParseVersion(_currentVersion, out var currentVersion) ||
                !TryParseVersion(latestTag, out var latestVersion))
            {
                LoggingService.Warn($"Could not parse launcher versions: current={_currentVersion}, latest={latestTag}");
                return;
            }

            if (latestVersion <= currentVersion)
            {
                if (!silent)
                    ShowNotification("Update Service", "You are already on the latest launcher version.");
                return;
            }

            var asset = release["assets"]?.FirstOrDefault(a => a["name"]?.ToString().EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
            var downloadUrl = asset?["browser_download_url"]?.ToString();

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                ShowNotification("Update Available", $"{latestTag} is available, but no downloadable asset was found.", true);
                return;
            }

            ShowNotification("Update Available", $"Midnight Launcher {latestTag} is ready.");

            if (downloadIfNewer)
                await DownloadUpdateAsync(downloadUrl);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Update check failed", ex);
            if (!silent)
                ShowNotification("Update Failed", "The launcher could not contact GitHub Releases.", true);
        }
    }

    private static bool TryParseVersion(string input, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        var match = Regex.Match(input, @"(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?");
        if (!match.Success)
            return false;

        var major = int.Parse(match.Groups[1].Value);
        var minor = int.Parse(match.Groups[2].Value);
        var build = int.Parse(match.Groups[3].Value);
        var revision = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
        version = new Version(major, minor, build, revision);
        return true;
    }

    private async Task DownloadUpdateAsync(string url)
    {
        try
        {
            StatusTextBlock.Text = "Downloading launcher update...";
            DownloadProgressBar.IsIndeterminate = true;

            Directory.CreateDirectory(LauncherPaths.UpdatesDirectory);
            var zipPath = Path.Combine(LauncherPaths.UpdatesDirectory, "update.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes > 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long bytesRead = 0;
            int read;

            while ((read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                bytesRead += read;

                if (canReportProgress)
                {
                    var progress = (double)bytesRead / totalBytes * 100;
                    Dispatcher.UIThread.Post(() =>
                    {
                        DownloadProgressBar.IsIndeterminate = false;
                        DownloadProgressBar.Value = progress;
                        StatusTextBlock.Text = $"Update download: {progress:F1}%";
                    });
                }
            }

            DownloadProgressBar.IsIndeterminate = false;
            StatusTextBlock.Text = "Update downloaded.";

            if (OperatingSystem.IsWindows())
            {
                PrepareUpdaterScript();
                ShowNotification("Update Ready", "The launcher will update itself on the next close or restart.");
            }
            else
            {
                ShowNotification("Update Ready", "The update archive was downloaded, but self-apply is Windows-only.", true);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to download update", ex);
            ShowNotification("Update Failed", "The update archive could not be downloaded.", true);
            StatusTextBlock.Text = "Update failed.";
            DownloadProgressBar.Value = 0;
            DownloadProgressBar.IsIndeterminate = false;
        }
    }

    private void PrepareUpdaterScript()
    {
        try
        {
            var zipPath = Path.Combine(LauncherPaths.UpdatesDirectory, "update.zip");
            var appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var exeName = Path.GetFileName(Environment.ProcessPath ?? "Midnight-Launcher.exe");
            var exePath = Path.Combine(appDir, exeName);

            var script = $@"
Start-Sleep -Seconds 2
$zipPath = '{zipPath.Replace("'", "''")}'
$destPath = '{appDir.Replace("'", "''")}'
Expand-Archive -Path $zipPath -DestinationPath $destPath -Force
Remove-Item $zipPath -Force
Start-Process '{exePath.Replace("'", "''")}'
";

            File.WriteAllText(LauncherPaths.UpdaterScriptPath, script);
            LoggingService.Info("Updater script prepared.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to prepare updater script", ex);
        }
    }

    private void StartUpdaterIfPending()
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(LauncherPaths.UpdaterScriptPath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{LauncherPaths.UpdaterScriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            File.Delete(LauncherPaths.UpdaterScriptPath);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to launch updater script", ex);
        }
    }

    private async void PlayButton_Click(object? sender, RoutedEventArgs e)
    {
        var rawVersion = VersionComboBox.SelectedItem as string;
        var username = AccountComboBox.SelectedItem as string;

        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            StatusTextBlock.Text = "Please select a Minecraft version.";
            return;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            StatusTextBlock.Text = "Please select an account.";
            return;
        }

        var version = NormalizeVersion(rawVersion);
        PlayButton.IsEnabled = false;
        StatusTextBlock.Text = $"Preparing {version}...";

        try
        {
            await _launcher.InstallAsync(version);

            var process = await _launcher.BuildProcessAsync(version, new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(username),
                MaximumRamMb = (int)RamSlider.Value
            });

            process.Start();
            StatusTextBlock.Text = "Game started.";
            LoggingService.Info($"Game process started for {username} on {version}.");

            if (File.Exists(LauncherPaths.UpdaterScriptPath))
            {
                StartUpdaterIfPending();
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to launch game", ex);
            StatusTextBlock.Text = $"Launch failed: {ex.Message}";
        }
        finally
        {
            PlayButton.IsEnabled = true;
        }
    }

    private static string NormalizeVersion(string input)
    {
        const string downloadedSuffix = " (Downloaded)";
        return input.EndsWith(downloadedSuffix, StringComparison.OrdinalIgnoreCase)
            ? input[..^downloadedSuffix.Length]
            : input;
    }

    private void AddAccountButton_Click(object? sender, RoutedEventArgs e)
    {
        var username = NewUsernameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(username))
            return;

        if (Accounts.Any(account => string.Equals(account, username, StringComparison.OrdinalIgnoreCase)))
        {
            StatusTextBlock.Text = "That account already exists.";
            return;
        }

        Accounts.Add(username);
        SaveAccounts();
        AccountComboBox.SelectedItem = username;
        AccountsListBox.SelectedItem = username;
        NewUsernameTextBox.Clear();
        StatusTextBlock.Text = $"Account '{username}' saved.";
        LoggingService.Info($"Added account: {username}");
    }

    private void ThemeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is not ComboBoxItem item)
            return;

        var theme = item.Content?.ToString();
        if (string.IsNullOrWhiteSpace(theme))
            return;

        ApplyTheme(theme);
        _config.Theme = theme;
        ConfigService.Save(_config);
        LoggingService.Info($"Theme changed to {theme}.");
    }

    private void ExperimentalUiToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ExperimentalUiToggle.IsChecked != true)
            return;

        try
        {
            _config.ExperimentalUi = true;
            _config.SelectedRam = (int)RamSlider.Value;
            ConfigService.Save(_config);

            var experimentalWindow = new ExperimentalMainWindow();
            experimentalWindow.Show();
            Close();
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to switch to experimental UI", ex);
            ShowNotification("UI Switch Failed", "The experimental launcher could not be opened.", true);
        }
    }

    private void NavListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavListBox.SelectedItem is not ListBoxItem item)
            return;

        if (item == HomeNav) ShowView(LauncherView.Home);
        else if (item == AccountsNav) ShowView(LauncherView.Accounts);
        else if (item == NewsNav)
        {
            ShowView(LauncherView.News);
            _ = LoadNewsAsync();
        }
        else if (item == ModsNav) ShowView(LauncherView.Mods);
        else if (item == ModloadersNav) ShowView(LauncherView.Modloaders);
        else if (item == ChangelogsNav)
        {
            ShowView(LauncherView.Changelogs);
            _ = LoadChangelogAsync();
        }
        else if (item == SettingsNav) ShowView(LauncherView.Settings);
    }

    private void ShowView(LauncherView view)
    {
        ViewTitle.Text = view switch
        {
            LauncherView.Home => "Home",
            LauncherView.Accounts => "Accounts",
            LauncherView.News => "News",
            LauncherView.Mods => "Mods",
            LauncherView.Modloaders => "Modloaders",
            LauncherView.Changelogs => "Changelogs",
            LauncherView.Settings => "Settings",
            _ => "Home"
        };

        HomeView.IsVisible = view == LauncherView.Home;
        AccountsView.IsVisible = view == LauncherView.Accounts;
        NewsView.IsVisible = view == LauncherView.News;
        ModsView.IsVisible = view == LauncherView.Mods;
        ModloadersView.IsVisible = view == LauncherView.Modloaders;
        ChangelogsView.IsVisible = view == LauncherView.Changelogs;
        SettingsView.IsVisible = view == LauncherView.Settings;
    }

    private void NewsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NewsListBox.SelectedItem is not NewsItem item || string.IsNullOrWhiteSpace(item.Url))
            return;

        _ = OpenUrlAsync(item.Url);
        NewsListBox.SelectedItem = null;
    }

    private void ModsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ModsListBox.SelectedItem is not ModItem item || string.IsNullOrWhiteSpace(item.ProjectUrl))
            return;

        _ = OpenUrlAsync(item.ProjectUrl);
        ModsListBox.SelectedItem = null;
    }

    private async void ChangelogVersionListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ChangelogVersionListBox.SelectedItem is not string title)
            return;

        try
        {
            if (_newsFeed.Count == 0)
                _newsFeed = await GetMojangFeedAsync();

            var entry = _newsFeed.FirstOrDefault(item => string.Equals(item.Title, title, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                return;

            ChangelogContentText.Text = $"Date: {entry.Date}\n\n{entry.Summary}";
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to fetch changelog detail", ex);
            ChangelogContentText.Text = "Failed to load changelog details.";
        }
    }

    private async void ChangeFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Game Folder",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return;

        var selectedPath = folders[0].Path.LocalPath;
        try
        {
            Directory.CreateDirectory(selectedPath);
            _config.GamePath = selectedPath;
            ConfigService.Save(_config);
            ConfigureLauncher(selectedPath);
            await LoadVersionsAsync();
            ShowNotification("Path Updated", "The launcher will use the new game directory now.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to change game folder", ex);
            ShowNotification("Path Change Failed", "The selected folder could not be applied.", true);
        }
    }

    private void OpenFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = _config.GamePath;

        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetFullPath(path),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to open game folder", ex);
            StatusTextBlock.Text = $"Could not open folder: {ex.Message}";
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (File.Exists(LauncherPaths.UpdaterScriptPath))
            StartUpdaterIfPending();

        base.OnClosing(e);
    }
}

internal enum LauncherView
{
    Home,
    Accounts,
    News,
    Mods,
    Modloaders,
    Changelogs,
    Settings
}

public class NewsItem
{
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class ModItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string ProjectUrl => string.IsNullOrWhiteSpace(Id) ? string.Empty : $"https://modrinth.com/mod/{Id}";
}
