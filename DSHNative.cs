using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Microsoft.Web.WebView2.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            Log("DSH started.");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SetDpiAwareness();

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            EnsureRuntime();

            string url = EnsureBackendAndGetUrl();
            if (url != null)
            {
                new GlassMainWindow(url).ShowDialog();
            }
        }
        catch (Exception ex)
        {
            Log("Startup error: " + ex);
            ShowImageDialog("DSH", "Startup error: " + ex.Message, "OK", "error");
        }
    }

    public static void Log(string message)
    {
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DSH");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "debug.log"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + "\r\n");
        }
        catch { }
    }

    static void SetDpiAwareness()
    {
        try
        {
            if (!SetProcessDpiAwarenessContext((IntPtr)(-4)))
            {
                SetProcessDPIAware();
            }
        }
        catch { }
    }

    static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        string resource = "DSH." + name;
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
        {
            if (s == null) return null;
            byte[] data = new byte[s.Length];
            s.Read(data, 0, data.Length);
            return Assembly.Load(data);
        }
    }

    static void EnsureRuntime()
    {
        string baseData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DSH");
        string binDir = Path.Combine(baseData, "bin");
        Directory.CreateDirectory(binDir);
        string loader = Path.Combine(binDir, "WebView2Loader.dll");
        if (!File.Exists(loader))
        {
            WriteResource("DSH.WebView2Loader.dll", loader);
        }
        string path = Environment.GetEnvironmentVariable("PATH") ?? "";
        if (!path.Contains(binDir))
        {
            Environment.SetEnvironmentVariable("PATH", binDir + ";" + path);
        }
    }

    static void WriteResource(string resourceName, string destPath)
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        using (Stream f = File.Create(destPath))
        {
            if (s == null) throw new Exception("Missing embedded resource: " + resourceName);
            s.CopyTo(f);
        }
    }

    public static byte[] ReadEmbeddedBytes(string resourceName)
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (s == null) throw new Exception("Missing embedded resource: " + resourceName);
            byte[] data = new byte[s.Length];
            s.Read(data, 0, data.Length);
            return data;
        }
    }

    static string ExeDir
    {
        get { return AppDomain.CurrentDomain.BaseDirectory; }
    }

    static string PhotoDir
    {
        get { return Path.Combine(ExeDir, "photo"); }
    }

    static string ConfigPath
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DSH", "dsh-config.json"); }
    }

    static string GetImagePath(string name)
    {
        if (!Directory.Exists(PhotoDir)) return null;
        string[] candidates = new string[]
        {
            Path.Combine(PhotoDir, name + ".jpg"),
            Path.Combine(PhotoDir, name + ".png"),
            Path.Combine(PhotoDir, "dialog-default.jpg"),
            Path.Combine(PhotoDir, "dialog-default.png")
        };
        foreach (string c in candidates)
        {
            if (File.Exists(c)) return c;
        }
        string[] files = Directory.GetFiles(PhotoDir, "*.jpg");
        if (files.Length == 0) files = Directory.GetFiles(PhotoDir, "*.png");
        if (files.Length > 0) return files[0];
        return null;
    }

    static DialogResult ShowImageDialog(string title, string message, string buttons, string imageName)
    {
        using (Form f = new Form())
        {
            f.Text = title;
            f.ClientSize = new Size(560, 300);
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            f.StartPosition = FormStartPosition.CenterScreen;
            f.ShowInTaskbar = false;

            string img = GetImagePath(imageName);
            int labelX = 20;
            if (img != null)
            {
                PictureBox pb = new PictureBox();
                pb.Image = Image.FromFile(img);
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Location = new Point(14, 14);
                pb.Size = new Size(230, 200);
                f.Controls.Add(pb);
                labelX = 260;
            }

            Label label = new Label();
            label.Text = message;
            label.Location = new Point(labelX, 20);
            label.Size = new Size(560 - labelX - 20, 200);
            label.Font = new Font("Microsoft YaHei UI", 9);
            f.Controls.Add(label);

            if (buttons == "YesNo")
            {
                Button yes = new Button();
                yes.Text = "Yes";
                yes.DialogResult = DialogResult.Yes;
                yes.Location = new Point(300, 240);
                yes.Size = new Size(100, 32);
                f.Controls.Add(yes);

                Button no = new Button();
                no.Text = "No";
                no.DialogResult = DialogResult.No;
                no.Location = new Point(420, 240);
                no.Size = new Size(100, 32);
                f.Controls.Add(no);
            }
            else
            {
                Button ok = new Button();
                ok.Text = "OK";
                ok.DialogResult = DialogResult.OK;
                ok.Location = new Point(420, 240);
                ok.Size = new Size(100, 32);
                f.Controls.Add(ok);
            }

            return f.ShowDialog();
        }
    }

    static string ShowFolderDialog(string title, string message, string imageName)
    {
        using (Form f = new Form())
        {
            f.Text = title;
            f.ClientSize = new Size(620, 300);
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            f.StartPosition = FormStartPosition.CenterScreen;
            f.ShowInTaskbar = false;

            string img = GetImagePath(imageName);
            int labelX = 20;
            if (img != null)
            {
                PictureBox pb = new PictureBox();
                pb.Image = Image.FromFile(img);
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Location = new Point(14, 14);
                pb.Size = new Size(200, 180);
                f.Controls.Add(pb);
                labelX = 230;
            }

            Label label = new Label();
            label.Text = message;
            label.Location = new Point(labelX, 14);
            label.Size = new Size(620 - labelX - 20, 60);
            label.Font = new Font("Microsoft YaHei UI", 9);
            f.Controls.Add(label);

            TextBox box = new TextBox();
            box.Location = new Point(labelX, 90);
            box.Size = new Size(620 - labelX - 90, 26);
            f.Controls.Add(box);

            Button browse = new Button();
            browse.Text = "Browse...";
            browse.Location = new Point(620 - 110, 88);
            browse.Size = new Size(90, 30);
            browse.Click += delegate
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Select DSH main folder";
                    dlg.ShowNewFolderButton = false;
                    if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.SelectedPath;
                }
            };
            f.Controls.Add(browse);

            Button ok = new Button();
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point(380, 240);
            ok.Size = new Size(100, 32);
            f.Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Location = new Point(490, 240);
            cancel.Size = new Size(100, 32);
            f.Controls.Add(cancel);

            if (f.ShowDialog() == DialogResult.OK) return box.Text.Trim();
            return null;
        }
    }

    static bool TestPort(int port)
    {
        try
        {
            using (TcpClient c = new TcpClient())
            {
                IAsyncResult ar = c.BeginConnect("127.0.0.1", port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(1500)) return false;
                c.EndConnect(ar);
                return true;
            }
        }
        catch { return false; }
    }

    static bool TestDshPaths(string node, string bin)
    {
        return !string.IsNullOrEmpty(node) && !string.IsNullOrEmpty(bin) && File.Exists(node) && File.Exists(bin);
    }

    static bool ReadConfig(out string node, out string bin)
    {
        node = null;
        bin = null;
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var cfg = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                node = Convert.ToString(cfg["node"]);
                bin = Convert.ToString(cfg["bin"]);
                if (TestDshPaths(node, bin)) return true;
            }
        }
        catch { }
        node = null;
        bin = null;
        return false;
    }

    static void SaveConfig(string node, string bin)
    {
        try
        {
            var cfg = new Dictionary<string, object>();
            cfg["node"] = node;
            cfg["bin"] = bin;
            cfg["updated"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = new JavaScriptSerializer().Serialize(cfg);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }

    static bool FindDshPaths(out string node, out string bin)
    {
        node = null;
        bin = null;

        string envNode = Environment.GetEnvironmentVariable("DSH_NODE");
        string envBin = Environment.GetEnvironmentVariable("DSH_BIN");
        if (TestDshPaths(envNode, envBin))
        {
            node = envNode;
            bin = envBin;
            return true;
        }

        string envRoot = Environment.GetEnvironmentVariable("DSH_ROOT");
        if (!string.IsNullOrEmpty(envRoot))
        {
            string n = Path.Combine(envRoot, "node.exe");
            string b = Path.Combine(envRoot, "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
            if (TestDshPaths(n, b))
            {
                node = n;
                bin = b;
                return true;
            }
        }

        string cmdNode = FindOnPath("node.exe");
        if (cmdNode != null)
        {
            string dir = Path.GetDirectoryName(cmdNode);
            string b = Path.Combine(dir, "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
            if (TestDshPaths(cmdNode, b))
            {
                node = cmdNode;
                bin = b;
                return true;
            }
        }

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string userNodeRoot = Path.Combine(userProfile, "nodejs");
        if (Directory.Exists(userNodeRoot))
        {
            foreach (string vdir in Directory.GetDirectories(userNodeRoot, "node-v*-win-x64"))
            {
                string n = Path.Combine(vdir, "node.exe");
                string b = Path.Combine(vdir, "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
                if (TestDshPaths(n, b))
                {
                    node = n;
                    bin = b;
                    return true;
                }
            }

            string n2 = Path.Combine(userNodeRoot, "node.exe");
            string b2 = Path.Combine(userNodeRoot, "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
            if (TestDshPaths(n2, b2))
            {
                node = n2;
                bin = b2;
                return true;
            }
        }

        string[] fixedNodes = new string[]
        {
            @"C:\Program Files\nodejs\node.exe",
            @"C:\Program Files (x86)\nodejs\node.exe"
        };
        foreach (string n in fixedNodes)
        {
            string b = Path.Combine(Path.GetDirectoryName(n), "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
            if (TestDshPaths(n, b))
            {
                node = n;
                bin = b;
                return true;
            }
        }

        return false;
    }

    static string FindOnPath(string fileName)
    {
        string path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (string dir in path.Split(';'))
        {
            try
            {
                string full = Path.Combine(dir.Trim(), fileName);
                if (File.Exists(full)) return full;
            }
            catch { }
        }
        return null;
    }

    static bool AskDshRoot(out string node, out string bin)
    {
        node = null;
        bin = null;
        string root = ShowFolderDialog("Select DSH Main Folder",
            "Select the DSH main folder that contains node.exe and node_modules\\@deepseek-ai\\dsh",
            "folder-picker");
        if (string.IsNullOrEmpty(root)) return false;

        string n = Path.Combine(root, "node.exe");
        string b = Path.Combine(root, "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
        if (!TestDshPaths(n, b))
        {
            string nodejsRoot = Path.Combine(root, "nodejs");
            if (Directory.Exists(nodejsRoot))
            {
                string[] vdirs = Directory.GetDirectories(nodejsRoot, "node-v*-win-x64");
                if (vdirs.Length > 0)
                {
                    n = Path.Combine(vdirs[0], "node.exe");
                    b = Path.Combine(vdirs[0], "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
                }
            }
        }

        if (TestDshPaths(n, b))
        {
            node = n;
            bin = b;
            return true;
        }

        ShowImageDialog("DSH", "The selected folder does not look like a DSH main folder.", "OK", "error");
        return false;
    }

    static bool InstallDshRuntime(out string node, out string bin)
    {
        node = null;
        bin = null;
        GlassConfirmDialog confirm = new GlassConfirmDialog();
        if (confirm.ShowDialog() != true) { Log("Auto download cancelled by user."); return false; }
        Log("Auto download confirmed.");

        try
        {
            string baseData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DSH");
            string runtimeDir = Path.Combine(baseData, "runtime");
            Directory.CreateDirectory(runtimeDir);

            string nodeVersion = "v22.23.2";
            string zipName = "node-" + nodeVersion + "-win-x64.zip";
            string zipPath = Path.Combine(runtimeDir, zipName);
            string nodeDir = Path.Combine(runtimeDir, "node-" + nodeVersion + "-win-x64");
            string nodeExe = Path.Combine(nodeDir, "node.exe");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (!File.Exists(zipPath))
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile("https://nodejs.org/dist/" + nodeVersion + "/" + zipName, zipPath);
                }
            }

            if (!File.Exists(nodeExe))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, runtimeDir);
            }

            if (!File.Exists(nodeExe))
            {
                ShowImageDialog("DSH Setup", "Node.js download/extract failed.", "OK", "error");
                return false;
            }

            string dshPrefix = Path.Combine(runtimeDir, "dsh");
            Directory.CreateDirectory(dshPrefix);
            string npm = Path.Combine(nodeDir, "npm.cmd");
            Process.Start(new ProcessStartInfo(npm, "install --prefix \"" + dshPrefix + "\" @deepseek-ai/dsh@latest --no-audit --no-fund")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            }).WaitForExit();

            string binPath = Path.Combine(dshPrefix, "node_modules", "@deepseek-ai", "dsh", "lib", "bin.js");
            if (!File.Exists(binPath))
            {
                ShowImageDialog("DSH Setup", "DSH package install failed.", "OK", "error");
                return false;
            }

            node = nodeExe;
            bin = binPath;
            Log("Auto download completed. node=" + nodeExe + " bin=" + binPath);
            return true;
        }
        catch (Exception ex)
        {
            Log("Auto download error: " + ex);
            ShowImageDialog("DSH Setup", "Automatic setup failed: " + ex.Message, "OK", "error");
            return false;
        }
    }

    static bool ResolveDshPaths(out string node, out string bin)
    {
        node = null;
        bin = null;
        if (ReadConfig(out node, out bin)) return true;
        if (FindDshPaths(out node, out bin)) { SaveConfig(node, bin); return true; }
        if (InstallDshRuntime(out node, out bin)) { SaveConfig(node, bin); return true; }

        for (int i = 0; i < 3; i++)
        {
            if (AskDshRoot(out node, out bin))
            {
                SaveConfig(node, bin);
                return true;
            }
            DialogResult again = ShowImageDialog("DSH", "DSH main folder was not configured. Try again?", "YesNo", "retry");
            if (again != DialogResult.Yes) break;
        }
        return false;
    }

    static string EnsureBackendAndGetUrl()
    {
        const string gui = "http://127.0.0.1:3080";
        const string shell = gui + "/app-shell";

        if (!TestPort(3080))
        {
            string node, bin;
            if (!ResolveDshPaths(out node, out bin))
            {
                ShowImageDialog("DSH", "DSH backend could not be started because the DSH main folder was not configured.", "OK", "error");
                return null;
            }

            ProcessStartInfo psi = new ProcessStartInfo(node, "\"" + bin + "\" --profile web --port 3080");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);

            for (int i = 0; i < 60; i++)
            {
                System.Threading.Thread.Sleep(500);
                if (TestPort(3080)) break;
            }
        }

        string target = InputUrlDialog("WebUI 投影", "输入要在窗口中打开的 WebUI 地址（默认：DSH 界面）：", shell);
        if (string.IsNullOrWhiteSpace(target)) return null;
        target = target.Trim();
        if (!target.StartsWith("http://") && !target.StartsWith("https://")) target = "http://" + target;

        if (target == shell || target == gui || target == gui + "/") return shell;
        return shell + "?u=" + Uri.EscapeDataString(target);
    }

    static string InputUrlDialog(string title, string prompt, string defaultValue)
    {
        GlassInputDialog dlg = new GlassInputDialog(defaultValue);
        if (dlg.ShowDialog() == true) return dlg.AddressText;
        return "";
    }

    [StructLayout(LayoutKind.Sequential)]
    struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    static void EnableAcrylic(IntPtr hwnd, Color tint)
    {
        try
        {
            AccentPolicy accent = new AccentPolicy();
            accent.AccentState = 4; // ACCENT_ENABLE_ACRYLICBLURBEHIND
            accent.GradientColor = (tint.A << 24) | (tint.B << 16) | (tint.G << 8) | tint.R;
            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);
            WindowCompositionAttributeData data = new WindowCompositionAttributeData();
            data.Attribute = 19; // WCA_ACCENT_POLICY
            data.Data = ptr;
            data.SizeOfData = size;
            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }
        catch { }
    }


    [DllImport("user32.dll")]
    static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
}


public class GlassInputDialog : System.Windows.Window
{
    private System.Windows.Controls.TextBox addrBox;
    public string AddressText { get { return addrBox.Text; } }

    public GlassInputDialog(string defaultValue)
    {
        Title = "WebUI 投影";
        Width = 500;
        Height = 500;
        WindowStyle = System.Windows.WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

        var root = new System.Windows.Controls.Canvas();
        root.Width = 500;
        root.Height = 500;

        var bgImage = new System.Windows.Controls.Image();
        bgImage.Width = 500;
        bgImage.Height = 500;
        bgImage.Stretch = System.Windows.Media.Stretch.Fill;
        try
        {
            byte[] imgBytes = Program.ReadEmbeddedBytes("DSH.photo6.png");
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.StreamSource = new System.IO.MemoryStream(imgBytes);
            bmp.EndInit();
            bgImage.Source = bmp;
        }
        catch { }
        System.Windows.Controls.Canvas.SetLeft(bgImage, 0);
        System.Windows.Controls.Canvas.SetTop(bgImage, 0);
        root.Children.Add(bgImage);

        var label = new System.Windows.Controls.TextBlock();
        label.Text = "WebUI 投影地址：";
        label.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(1, 36, 86));
        label.FontSize = 16;
        label.FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei UI");
        System.Windows.Controls.Canvas.SetLeft(label, 70);
        System.Windows.Controls.Canvas.SetTop(label, 179);
        root.Children.Add(label);

        var close = new System.Windows.Controls.TextBlock();
        close.Text = "✕";
        close.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(1, 36, 86));
        close.FontSize = 18;
        close.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        System.Windows.Controls.Canvas.SetLeft(close, 470);
        System.Windows.Controls.Canvas.SetTop(close, 8);
        close.MouseLeftButtonDown += delegate { Close(); };
        root.Children.Add(close);

        var addrRect = new System.Windows.Rect(70, 214, 360, 36);
        var addrGlass = MakeGlass(addrRect, bgImage);
        addrBox = new System.Windows.Controls.TextBox();
        addrBox.Text = defaultValue;
        addrBox.FontSize = 14;
        addrBox.Foreground = System.Windows.Media.Brushes.Black;
        addrBox.Background = System.Windows.Media.Brushes.Transparent;
        addrBox.BorderThickness = new System.Windows.Thickness(0);
        addrBox.Padding = new System.Windows.Thickness(8, 6, 8, 4);
        addrGlass.Children.Add(addrBox);
        root.Children.Add(addrGlass);

        var openRect = new System.Windows.Rect(320, 430, 80, 36);
        var openGlass = MakeGlass(openRect, bgImage);
        var openText = new System.Windows.Controls.TextBlock();
        openText.Text = "打开";
        openText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(1, 36, 86));
        openText.FontSize = 14;
        openText.FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei UI");
        openText.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        openText.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        openGlass.Children.Add(openText);
        root.Children.Add(openGlass);

        var cancelRect = new System.Windows.Rect(410, 430, 80, 36);
        var cancelGlass = MakeGlass(cancelRect, bgImage);
        var cancelText = new System.Windows.Controls.TextBlock();
        cancelText.Text = "取消";
        cancelText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(1, 36, 86));
        cancelText.FontSize = 14;
        cancelText.FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei UI");
        cancelText.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        cancelText.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        cancelGlass.Children.Add(cancelText);
        root.Children.Add(cancelGlass);

        openText.MouseLeftButtonDown += delegate { DialogResult = true; Close(); };
        cancelText.MouseLeftButtonDown += delegate { DialogResult = false; Close(); };

        Content = root;
    }

    private System.Windows.Controls.Grid MakeGlass(System.Windows.Rect rect, System.Windows.Controls.Image bgImage)
    {
        var brush = new System.Windows.Media.VisualBrush(bgImage);
        brush.Viewbox = rect;
        brush.ViewboxUnits = System.Windows.Media.BrushMappingMode.Absolute;
        brush.Stretch = System.Windows.Media.Stretch.Fill;

        var blur = new System.Windows.Media.Effects.BlurEffect();
        blur.Radius = 12;
        blur.KernelType = System.Windows.Media.Effects.KernelType.Gaussian;

        var glass = new System.Windows.Controls.Border();
        glass.Width = rect.Width;
        glass.Height = rect.Height;
        glass.Background = brush;
        glass.Effect = blur;
        glass.CornerRadius = new System.Windows.CornerRadius(8);

        var overlay = new System.Windows.Controls.Border();
        overlay.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 255, 255, 255));
        overlay.CornerRadius = new System.Windows.CornerRadius(8);

        var grid = new System.Windows.Controls.Grid();
        grid.Width = rect.Width;
        grid.Height = rect.Height;
        grid.Children.Add(glass);
        grid.Children.Add(overlay);

        System.Windows.Controls.Canvas.SetLeft(grid, rect.X);
        System.Windows.Controls.Canvas.SetTop(grid, rect.Y);

        return grid;
    }
}

public class GlassConfirmDialog : System.Windows.Window
{
    public GlassConfirmDialog()
    {
        Title = "DSH 环境安装确认";
        Width = 620;
        Height = 420;
        WindowStyle = System.Windows.WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

        var root = new System.Windows.Controls.Grid();

        try
        {
            byte[] imgBytes = Program.ReadEmbeddedBytes("DSH.photo1.jpg");
            var bg = new System.Windows.Controls.Image();
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.StreamSource = new System.IO.MemoryStream(imgBytes);
            bmp.EndInit();
            bg.Source = bmp;
            bg.Stretch = System.Windows.Media.Stretch.Fill;
            root.Children.Add(bg);
        }
        catch { }

        string runtimeDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DSH", "runtime");

        // Right-side dark glass panel for readable text.
        var panel = new System.Windows.Controls.Border();
        panel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        panel.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
        panel.Margin = new System.Windows.Thickness(0, 0, 24, 50);
        panel.Width = 320;
        panel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 10, 14, 18));
        panel.CornerRadius = new System.Windows.CornerRadius(12);
        panel.Padding = new System.Windows.Thickness(24, 20, 24, 20);

        var stack = new System.Windows.Controls.StackPanel();

        var text = new System.Windows.Controls.TextBlock();
        text.Text = "即将下载并安装 DSH 运行环境：\n\n" +
                    "来源：https://nodejs.org\n" +
                    "组件：Node.js + @deepseek-ai/dsh\n" +
                    "安装位置：" + runtimeDir + "\n\n" +
                    "是否继续？";
        text.Foreground = System.Windows.Media.Brushes.White;
        text.FontSize = 15;
        text.FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei UI");
        text.TextWrapping = System.Windows.TextWrapping.Wrap;
        stack.Children.Add(text);

        var btnRow = new System.Windows.Controls.StackPanel();
        btnRow.Orientation = System.Windows.Controls.Orientation.Horizontal;
        btnRow.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        btnRow.Margin = new System.Windows.Thickness(0, 20, 0, 0);

        var yes = MakeGlassButton("是");
        yes.Click += delegate { DialogResult = true; Close(); };
        btnRow.Children.Add(yes);

        var no = MakeGlassButton("否");
        no.Click += delegate { DialogResult = false; Close(); };
        no.Margin = new System.Windows.Thickness(10, 0, 0, 0);
        btnRow.Children.Add(no);

        stack.Children.Add(btnRow);
        panel.Child = stack;
        root.Children.Add(panel);

        Content = root;
    }

    private System.Windows.Controls.Button MakeGlassButton(string content)
    {
        var btn = new System.Windows.Controls.Button();
        btn.Content = content;
        btn.Width = 100;
        btn.Height = 38;
        btn.FontSize = 14;
        btn.Foreground = System.Windows.Media.Brushes.White;
        btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(110, 255, 255, 255));
        btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(170, 255, 255, 255));
        btn.BorderThickness = new System.Windows.Thickness(1);
        return btn;
    }

}


public class GlassMainWindow : System.Windows.Window
{
    private Microsoft.Web.WebView2.WinForms.WebView2 web;

    public GlassMainWindow(string url)
    {
        Title = "DDSH";
        Width = 1240;
        Height = 800;
        WindowStyle = System.Windows.WindowStyle.None;
        ResizeMode = System.Windows.ResizeMode.CanResize;
        Background = System.Windows.Media.Brushes.White;
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

        var chrome = new System.Windows.Shell.WindowChrome();
        chrome.CaptionHeight = 40;
        chrome.ResizeBorderThickness = new System.Windows.Thickness(8);
        chrome.GlassFrameThickness = new System.Windows.Thickness(0);
        chrome.CornerRadius = new System.Windows.CornerRadius(0);
        chrome.UseAeroCaptionButtons = false;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this, chrome);

        var root = new System.Windows.Controls.Grid();
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(40) });
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

        try
        {
            byte[] bgBytes = Program.ReadEmbeddedBytes("DSH.photo2.png");
            var bgImage = new System.Windows.Controls.Image();
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.StreamSource = new System.IO.MemoryStream(bgBytes);
            bmp.EndInit();
            bgImage.Source = bmp;
            bgImage.Stretch = System.Windows.Media.Stretch.UniformToFill;
            System.Windows.Controls.Grid.SetRowSpan(bgImage, 2);
            root.Children.Add(bgImage);
        }
        catch { }

        var topBar = new System.Windows.Controls.Border();
        topBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 255, 255, 255));
        System.Windows.Controls.Grid.SetRow(topBar, 0);
        root.Children.Add(topBar);

        var btnMin = MakeButton("—", 40);
        var btnMax = MakeButton("□", 40);
        var btnClose = MakeButton("✕", 40);
        btnMin.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        btnMin.VerticalAlignment = System.Windows.VerticalAlignment.Top;
        btnMin.Margin = new System.Windows.Thickness(0, 4, 96, 0);
        btnMax.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        btnMax.VerticalAlignment = System.Windows.VerticalAlignment.Top;
        btnMax.Margin = new System.Windows.Thickness(0, 4, 52, 0);
        btnClose.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        btnClose.VerticalAlignment = System.Windows.VerticalAlignment.Top;
        btnClose.Margin = new System.Windows.Thickness(0, 4, 8, 0);
        btnMin.Click += delegate { WindowState = System.Windows.WindowState.Minimized; };
        btnMax.Click += delegate
        {
            WindowState = WindowState == System.Windows.WindowState.Maximized ? System.Windows.WindowState.Normal : System.Windows.WindowState.Maximized;
        };
        btnClose.Click += delegate { Close(); };
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btnMin, true);
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btnMax, true);
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btnClose, true);

        root.Children.Add(btnMin);
        root.Children.Add(btnMax);
        root.Children.Add(btnClose);

        var host = new System.Windows.Forms.Integration.WindowsFormsHost();
        web = new Microsoft.Web.WebView2.WinForms.WebView2();
        web.Dock = System.Windows.Forms.DockStyle.Fill;
        web.DefaultBackgroundColor = System.Drawing.Color.White;
        web.CoreWebView2InitializationCompleted += delegate
        {
            if (web.CoreWebView2 != null)
            {
                // Extract embedded background image to a local app-data folder for WebView2 virtual hosting.
                try
                {
                    string photoDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "DSH", "photo");
                    System.IO.Directory.CreateDirectory(photoDir);
                    string photoFile = System.IO.Path.Combine(photoDir, "2.png");
                    if (!System.IO.File.Exists(photoFile))
                    {
                        System.IO.File.WriteAllBytes(photoFile, Program.ReadEmbeddedBytes("DSH.photo2.png"));
                    }
                    web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "appassets",
                        photoDir,
                        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                }
                catch { }

                // WebUI is a direct page (no iframes); inject theme overrides into the top document.
                Program.Log("WebUI injection enabled.");
                string injectionScript = GetWebUiInjectionScript();
                web.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(injectionScript);
                web.NavigationCompleted += delegate
                {
                    try
                    {
                        web.CoreWebView2.ExecuteScriptAsync(injectionScript);
                    }
                    catch { }
                };
            }
        };
        web.Source = new Uri(url);
        host.Child = web;
        System.Windows.Controls.Grid.SetRow(host, 1);
        root.Children.Add(host);

        Content = root;
    }

    private string GetWebUiInjectionScript()
    {
        string js = @"
(function(){
    var css = [
        'html,body{background:url(\'https://appassets/2.png\') center/cover no-repeat fixed !important;background-color:rgba(0,0,0,0.2) !important;--dsw-alias-bg-base:rgba(255,255,255,0.55) !important;--dsw-alias-bg-layer-1:rgba(255,255,255,0.45) !important;--dsw-alias-bg-layer-2:rgba(240,242,245,0.8) !important;--dsw-specific-sidebar-fill:rgba(255,255,255,0.5) !important;--dsw-specific-bubble:rgba(40,120,220,0.55) !important;--dsw-specific-bubble-highlight:rgba(60,140,240,0.6) !important;}'
    ].join('');

    function injectIntoDocument(doc) {
        if (!doc || doc.getElementById('dsh-injected-style')) return;
        try {
            var style = doc.createElement('style');
            style.id = 'dsh-injected-style';
            style.textContent = css;
            (doc.head || doc.documentElement).appendChild(style);
        } catch(e) {}
    }

    function scanFrames() {
        injectIntoDocument(document);
        var frames = document.querySelectorAll('iframe');
        for (var i = 0; i < frames.length; i++) {
            try {
                if (frames[i].contentDocument) injectIntoDocument(frames[i].contentDocument);
            } catch(e) {}
        }
    }

    scanFrames();
    new MutationObserver(scanFrames).observe(document.documentElement, { childList: true, subtree: true });
})();
";
        return js;
    }

    private System.Windows.Controls.Button MakeButton(string text, int size)
    {
        var btn = new System.Windows.Controls.Button();
        btn.Content = text;
        btn.Width = size;
        btn.Height = 32;
        btn.FontSize = 14;
        btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(1, 36, 86));
        btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255));
        btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(140, 255, 255, 255));
        btn.BorderThickness = new System.Windows.Thickness(1);
        btn.Focusable = false;
        return btn;
    }
}


class MainForm : Form
{
    private const int WM_NCHITTEST = 0x84;
    private const int HTCAPTION = 2;
    private const int HTCLIENT = 1;
    private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

    private WebView2 web;
    private Button btnMin, btnMax, btnClose;
    private Rectangle normalBounds;
    private bool isMaximized;
    private int resizeBorder = 8;
    private int dragHeight = 36;

    public MainForm(string url)
    {
        Text = "DSH";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.White;
        StartPosition = FormStartPosition.Manual;

        int dpi = (int)Program.GetDpiForWindow(this.Handle);
        if (dpi < 96) dpi = 96;
        float scale = dpi / 96f;
        dragHeight = (int)(36 * scale);
        resizeBorder = (int)(8 * scale);

        ClientSize = new Size((int)(1240 * scale), (int)(800 * scale));
        MinimumSize = new Size((int)(640 * scale), (int)(480 * scale));
        Padding = new Padding(resizeBorder, dragHeight, resizeBorder, resizeBorder);

        var wa = Screen.PrimaryScreen.WorkingArea;
        Location = new Point((wa.Width - ClientSize.Width) / 2, (wa.Height - ClientSize.Height) / 2);

        web = new WebView2();
        web.Dock = DockStyle.Fill;
        web.DefaultBackgroundColor = Color.White;
        web.Source = new Uri(url);
        Controls.Add(web);

        int btnSize = (int)(34 * scale);
        int pad = (int)(6 * scale);
        btnMin = MakeButton("\u2013", btnSize);
        btnMax = MakeButton("\u25A1", btnSize);
        btnClose = MakeButton("\u2715", btnSize);
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);

        btnMin.Click += delegate { WindowState = FormWindowState.Minimized; };
        btnMax.Click += delegate { ToggleMaximize(); };
        btnClose.Click += delegate { Close(); };

        Controls.Add(btnMin);
        Controls.Add(btnMax);
        Controls.Add(btnClose);
        btnMin.BringToFront();
        btnMax.BringToFront();
        btnClose.BringToFront();

        Resize += delegate { LayoutButtons(); };
        LayoutButtons();
    }

    private void LayoutButtons()
    {
        if (btnMin == null || btnMax == null || btnClose == null) return;
        int btnSize = btnMin.Width;
        int pad = (int)(6 * ((int)Program.GetDpiForWindow(this.Handle) / 96f));
        int x = ClientSize.Width - btnSize * 3 - pad * 4 - resizeBorder;
        int y = (dragHeight - btnSize) / 2;
        btnMin.Location = new Point(x, y);
        btnMax.Location = new Point(x + btnSize + pad, y);
        btnClose.Location = new Point(x + 2 * (btnSize + pad), y);
    }

    private Button MakeButton(string text, int size)
    {
        Button b = new Button();
        b.Text = text;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.BorderColor = Color.White;
        b.TabStop = false;
        b.BackColor = Color.White;
        b.ForeColor = Color.FromArgb(60, 64, 67);
        b.Font = new Font("Segoe UI", 10);
        b.Size = new Size(size, size);
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 234, 238);
        b.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 223, 228);
        return b;
    }

    private void ToggleMaximize()
    {
        if (!isMaximized)
        {
            normalBounds = Bounds;
            Bounds = Screen.PrimaryScreen.WorkingArea;
            isMaximized = true;
            btnMax.Text = "\u2750";
        }
        else
        {
            Bounds = normalBounds;
            isMaximized = false;
            btnMax.Text = "\u25A1";
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.Style |= 0x00040000; // WS_THICKFRAME
            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            int x = (short)((int)m.LParam & 0xFFFF);
            int y = (short)(((int)m.LParam >> 16) & 0xFFFF);
            Program.RECT r;
            Program.GetWindowRect(this.Handle, out r);
            bool left = x < r.Left + resizeBorder;
            bool right = x > r.Right - resizeBorder;
            bool top = y < r.Top + resizeBorder;
            bool bottom = y > r.Bottom - resizeBorder;
            int ht = HTCLIENT;
            if (top && left) ht = HTTOPLEFT;
            else if (top && right) ht = HTTOPRIGHT;
            else if (bottom && left) ht = HTBOTTOMLEFT;
            else if (bottom && right) ht = HTBOTTOMRIGHT;
            else if (top) ht = HTTOP;
            else if (bottom) ht = HTBOTTOM;
            else if (left) ht = HTLEFT;
            else if (right) ht = HTRIGHT;
            else if (y < r.Top + dragHeight) ht = HTCAPTION;
            m.Result = (IntPtr)ht;
            return;
        }
        base.WndProc(ref m);
    }
}
