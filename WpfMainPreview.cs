using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shell;

public class MainGlassWindow : Window
{
    public MainGlassWindow()
    {
        Title = "DSH Main Preview";
        Width = 1240;
        Height = 800;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        Background = Brushes.White;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var chrome = new WindowChrome();
        chrome.CaptionHeight = 40;
        chrome.ResizeBorderThickness = new Thickness(8);
        chrome.GlassFrameThickness = new Thickness(0);
        chrome.CornerRadius = new CornerRadius(0);
        chrome.UseAeroCaptionButtons = false;
        WindowChrome.SetWindowChrome(this, chrome);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Optional background image
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string bgPath = System.IO.Path.Combine(baseDir, "photo", "2.png");
        if (!System.IO.File.Exists(bgPath)) bgPath = System.IO.Path.Combine(baseDir, "photo", "5.jpg");
        if (System.IO.File.Exists(bgPath))
        {
            var bgImage = new Image();
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(bgPath, UriKind.Absolute);
            bmp.EndInit();
            bgImage.Source = bmp;
            bgImage.Stretch = Stretch.UniformToFill;
            Grid.SetRowSpan(bgImage, 2);
            root.Children.Add(bgImage);
        }

        // Top bar
        var topBar = new Border();
        topBar.Background = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
        Grid.SetRow(topBar, 0);
        root.Children.Add(topBar);

        var btnMin = MakeButton("—", 40);
        var btnMax = MakeButton("□", 40);
        var btnClose = MakeButton("✕", 40);
        btnMin.HorizontalAlignment = HorizontalAlignment.Right;
        btnMin.VerticalAlignment = VerticalAlignment.Top;
        btnMin.Margin = new Thickness(0, 4, 96, 0);
        btnMax.HorizontalAlignment = HorizontalAlignment.Right;
        btnMax.VerticalAlignment = VerticalAlignment.Top;
        btnMax.Margin = new Thickness(0, 4, 52, 0);
        btnClose.HorizontalAlignment = HorizontalAlignment.Right;
        btnClose.VerticalAlignment = VerticalAlignment.Top;
        btnClose.Margin = new Thickness(0, 4, 8, 0);
        btnMin.Click += delegate { WindowState = WindowState.Minimized; };
        btnMax.Click += delegate
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        };
        btnClose.Click += delegate { Close(); };

        root.Children.Add(btnMin);
        root.Children.Add(btnMax);
        root.Children.Add(btnClose);

        // Content placeholder (WebView2 will be hosted here later)
        var content = new Border();
        content.Background = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255));
        content.Child = new TextBlock
        WindowChrome.SetIsHitTestVisibleInChrome(btnMin, true);
        WindowChrome.SetIsHitTestVisibleInChrome(btnMax, true);
        WindowChrome.SetIsHitTestVisibleInChrome(btnClose, true);
        {
            Text = "WebView2 content area",
            Foreground = new SolidColorBrush(Color.FromRgb(120, 130, 140)),
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        Content = root;
    }

    private Button MakeButton(string text, int size)
    {
        var btn = new Button();
        btn.Content = text;
        btn.Width = size;
        btn.Height = 32;
        btn.FontSize = 14;
        btn.Foreground = new SolidColorBrush(Color.FromRgb(1, 36, 86));
        btn.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
        btn.BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255));
        btn.BorderThickness = new Thickness(1);
        btn.Focusable = false;
        return btn;
    }
}

public static class MainPreviewProgram
{
    [STAThread]
    public static void Main()
    {
        var app = new Application();
        app.Run(new MainGlassWindow());
    }
}
