﻿using HandyControl.Themes;
using System.Windows;
using System.Windows.Media;
namespace AppUpdateClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var boot = new Bootstrapper();
            boot.Run();
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }

        internal void UpdateTheme(ApplicationTheme theme)
        {
            if (ThemeManager.Current.ApplicationTheme != theme)
            {
                ThemeManager.Current.ApplicationTheme = theme;
            }
        }

        internal void UpdateAccent(Brush accent)
        {
            if (ThemeManager.Current.AccentColor != accent)
            {
                ThemeManager.Current.AccentColor = accent;
            }
        }
    }
}
