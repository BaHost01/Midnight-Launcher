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
            try
            {
                var config = new { ExperimentalUi = false };
                File.WriteAllText("config.json", Newtonsoft.Json.JsonConvert.SerializeObject(config));
            }
            catch { }
            
            var main = new MainWindow();
            main.Show();
            this.Close();
        };
    }
}
