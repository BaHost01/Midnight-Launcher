using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Midnight_Launcher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            bool useExperimental = false;
            if (System.IO.File.Exists("config.json"))
            {
                try
                {
                    var json = System.IO.File.ReadAllText("config.json");
                    var config = Newtonsoft.Json.Linq.JObject.Parse(json);
                    useExperimental = (bool?)config["ExperimentalUi"] ?? false;
                }
                catch { }
            }

            if (useExperimental)
                desktop.MainWindow = new ExperimentalMainWindow();
            else
                desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
