using Avalonia.Controls;
using System;
using System.IO;

namespace Midnight_Launcher;

public partial class ExperimentalMainWindow : Window
{
    public ExperimentalMainWindow()
    {
        InitializeComponent();
        ReturnButton.Click += (s, e) =>
        {
            var config = ConfigService.Load();
            config.ExperimentalUi = false;
            ConfigService.Save(config);
            
            var main = new MainWindow();
            main.Show();
            this.Close();
        };
    }
}
