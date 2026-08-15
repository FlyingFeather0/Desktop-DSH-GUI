using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

public class GlassWindow : Window
{
    private Image bgImage;

    public GlassWindow()
    {
        Title = "WPF Glass Preview";
        Width = 500;
        Height = 500;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string imgPath = Path.Combine(baseDir, "photo", "6.png");
        if (!File.Exists(imgPath)) imgPath = Path.Combine(baseDir, "photo", "5.jpg");

        var root = new Canvas();
        root.Width = 500;
        root.Height = 500;

        bgImage = new Image();
        bgImage.Width = 500;
        bgImage.Height = 500;
        bgImage.Stretch = Stretch.Fill;
        if (File.Exists(imgPath))
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(imgPath, UriKind.Absolute);
            bmp.EndInit();
            bgImage.Source = bmp;
        }
        Canvas.SetLeft(bgImage, 0);
        Canvas.SetTop(bgImage, 0);
        root.Children.Add(bgImage);

        // WebUI 投影地址
        var label = new TextBlock();
        label.Text = "WebUI 投影地址：";
        label.Foreground = new SolidColorBrush(Color.FromRgb(1, 36, 86));
        label.FontSize = 16;
        label.FontFamily = new FontFamily("Microsoft YaHei UI");
        Canvas.SetLeft(label, 70);
        Canvas.SetTop(label, 179);
        root.Children.Add(label);

        // Close X
        var close = new TextBlock();
        close.Text = "✕";
        close.Foreground = new SolidColorBrush(Color.FromRgb(1, 36, 86));
        close.FontSize = 18;
        close.FontFamily = new FontFamily("Segoe UI");
        Canvas.SetLeft(close, 470);
        Canvas.SetTop(close, 8);
        close.MouseLeftButtonDown += delegate { Close(); };
        root.Children.Add(close);

        // Address glass panel
        var addrRect = new Rect(70, 214, 360, 36);
        var addrGlass = MakeGlass(addrRect);
        var addrBox = new TextBox();
        addrBox.Text = "http://127.0.0.1:3080/app-shell";
        addrBox.FontSize = 14;
        addrBox.Foreground = Brushes.Black;
        addrBox.Background = Brushes.Transparent;
        addrBox.BorderThickness = new Thickness(0);
        addrBox.Padding = new Thickness(8, 6, 8, 4);
        addrGlass.Children.Add(addrBox);
        root.Children.Add(addrGlass);

        // Open button
        var openRect = new Rect(320, 430, 80, 36);
        var openGlass = MakeGlass(openRect);
        var openText = new TextBlock();
        openText.Text = "打开";
        openText.Foreground = new SolidColorBrush(Color.FromRgb(1, 36, 86));
        openText.FontSize = 14;
        openText.FontFamily = new FontFamily("Microsoft YaHei UI");
        openText.HorizontalAlignment = HorizontalAlignment.Center;
        openText.VerticalAlignment = VerticalAlignment.Center;
        openGlass.Children.Add(openText);
        root.Children.Add(openGlass);

        // Cancel button
        var cancelRect = new Rect(410, 430, 80, 36);
        var cancelGlass = MakeGlass(cancelRect);
        var cancelText = new TextBlock();
        cancelText.Text = "取消";
        cancelText.Foreground = new SolidColorBrush(Color.FromRgb(1, 36, 86));
        cancelText.FontSize = 14;
        cancelText.FontFamily = new FontFamily("Microsoft YaHei UI");
        cancelText.HorizontalAlignment = HorizontalAlignment.Center;
        cancelText.VerticalAlignment = VerticalAlignment.Center;
        cancelGlass.Children.Add(cancelText);
        root.Children.Add(cancelGlass);

        Content = root;
    }

    private Grid MakeGlass(Rect rect)
    {
        var brush = new VisualBrush(bgImage);
        brush.Viewbox = rect;
        brush.ViewboxUnits = BrushMappingMode.Absolute;
        brush.Stretch = Stretch.Fill;

        var blur = new BlurEffect();
        blur.Radius = 12;
        blur.KernelType = KernelType.Gaussian;

        var glass = new Border();
        glass.Width = rect.Width;
        glass.Height = rect.Height;
        glass.Background = brush;
        glass.Effect = blur;
        glass.CornerRadius = new CornerRadius(8);

        var overlay = new Border();
        overlay.Background = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
        overlay.CornerRadius = new CornerRadius(8);

        var grid = new Grid();
        grid.Width = rect.Width;
        grid.Height = rect.Height;
        grid.Children.Add(glass);
        grid.Children.Add(overlay);

        Canvas.SetLeft(grid, rect.X);
        Canvas.SetTop(grid, rect.Y);

        return grid;
    }
}

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application();
        app.Run(new GlassWindow());
    }
}
