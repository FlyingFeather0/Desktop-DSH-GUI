using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace WebUiAnalyzer
{
    public class AnalyzerForm : Form
    {
        private WebView2 web;
        private Button btnCapture;
        private TextBox log;

        public AnalyzerForm()
        {
            Text = "DSH WebUI Analyzer";
            ClientSize = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;

            web = new WebView2();
            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = Color.White;
            Controls.Add(web);

            var panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 40;
            Controls.Add(panel);

            btnCapture = new Button();
            btnCapture.Text = "Capture Theme Info";
            btnCapture.Location = new Point(12, 6);
            btnCapture.Size = new Size(180, 28);
            btnCapture.Click += async delegate { await CaptureAsync(); };
            panel.Controls.Add(btnCapture);

            var btnRoot = new Button();
            btnRoot.Text = "Open Root";
            btnRoot.Location = new Point(200, 6);
            btnRoot.Size = new Size(120, 28);
            btnRoot.Click += delegate { web.Source = new Uri("http://127.0.0.1:3080/"); };
            panel.Controls.Add(btnRoot);

            var btnShell = new Button();
            btnShell.Text = "Open App Shell";
            btnShell.Location = new Point(330, 6);
            btnShell.Size = new Size(130, 28);
            btnShell.Click += delegate { web.Source = new Uri("http://127.0.0.1:3080/app-shell"); };
            panel.Controls.Add(btnShell);

            log = new TextBox();
            log.Dock = DockStyle.Right;
            log.Width = 420;
            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Vertical;
            Controls.Add(log);
            log.BringToFront();

            web.Source = new Uri("http://127.0.0.1:3080/app-shell");
        }

        private async System.Threading.Tasks.Task CaptureAsync()
        {
            try
            {
                if (web.CoreWebView2 == null)
                {
                    log.AppendText("CoreWebView2 not ready.\r\n");
                    return;
                }

                string js = @"
(function(){
    function collect(doc, label) {
        if (!doc || !doc.body) return null;
        var styles = Array.from(doc.querySelectorAll('style')).map(function(s){
            return {
                id: s.id || '',
                plugin: s.getAttribute('data-plugin-css') || '',
                len: s.textContent.length,
                head: s.textContent.substring(0, 300)
            };
        });
        var scripts = Array.from(doc.querySelectorAll('script[src]')).map(function(s){ return s.src; });
        var vars = {};
        var names = [
            '--dsw-alias-bg-base',
            '--dsw-alias-bg-layer-1',
            '--dsw-alias-bg-layer-2',
            '--dsw-alias-bg-layer-3',
            '--dsw-specific-sidebar-fill',
            '--dsw-alias-button-primary-fill',
            '--dsw-alias-border-l1'
        ];
        var cs = getComputedStyle(doc.body);
        names.forEach(function(n){ vars[n] = cs.getPropertyValue(n); });
        var side = null, center = null, frame = null;
        var el = doc.querySelector('[class*=""sidebarCol""]');
        if (el) side = { cls: el.className, bg: getComputedStyle(el).backgroundColor, w: el.offsetWidth, h: el.offsetHeight };
        el = doc.querySelector('[class*=""centerCol""]');
        if (el) center = { cls: el.className, bg: getComputedStyle(el).backgroundColor, w: el.offsetWidth, h: el.offsetHeight };
        el = doc.querySelector('[class*=""frame""]');
        if (el) frame = { cls: el.className, bg: getComputedStyle(el).backgroundColor, w: el.offsetWidth, h: el.offsetHeight };
        var iframes = Array.from(doc.querySelectorAll('iframe')).map(function(f){
            var info = { src: f.src, id: f.id || '', cls: f.className || '' };
            try { if (f.contentDocument) info.innerTitle = f.contentDocument.title; } catch(e) { info.error = 'cross-origin'; }
            return info;
        });
        return {
            label: label,
            href: doc.location ? doc.location.href : '',
            title: doc.title,
            bodyClass: doc.body.className,
            styles: styles,
            scripts: scripts,
            vars: vars,
            sidebar: side,
            center: center,
            frame: frame,
            iframes: iframes
        };
    }
    var top = collect(document, 'top');
    var frames = [];
    try {
        var list = document.querySelectorAll('iframe');
        for (var i = 0; i < list.length; i++) {
            try {
                if (list[i].contentDocument) frames.push(collect(list[i].contentDocument, 'frame-' + i));
            } catch(e) {}
        }
    } catch(e) {}

    var bodyChildren = Array.from(document.body.children).map(function(c){
        return c.tagName + '.' + (typeof c.className === 'string' ? c.className : '');
    }).slice(0, 12);

    var settingsBtn = null;
    var all = document.querySelectorAll('button, [role=""button""]');
    for (var i = 0; i < all.length; i++) {
        var t = (all[i].innerText || all[i].textContent || '').trim();
        if (/设置|Settings|通用|General/.test(t)) {
            settingsBtn = { tag: all[i].tagName, cls: all[i].className, text: t };
            break;
        }
    }

    var summary = {
        href: document.location.href,
        title: document.title,
        bodyClass: document.body.className,
        bodyChildrenCount: document.body.children.length,
        bodyChildren: bodyChildren,
        settingsButton: settingsBtn,
        iframeCount: document.querySelectorAll('iframe').length,
        iframes: Array.from(document.querySelectorAll('iframe')).map(function(f){ return f.src; }),
        sidebar: top ? top.sidebar : null,
        center: top ? top.center : null,
        frame: top ? top.frame : null,
        vars: top ? top.vars : null
    };
    return JSON.stringify(summary, null, 2);
})();
";
                string result = await web.CoreWebView2.ExecuteScriptAsync(js);
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webui-analysis.txt");
                File.WriteAllText(path, result);
                log.AppendText("Saved to " + path + "\r\n");
                log.AppendText(result.Substring(0, Math.Min(result.Length, 3000)) + "\r\n");
            }
            catch (Exception ex)
            {
                log.AppendText("Error: " + ex + "\r\n");
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // Ensure WebView2 DLLs from the wv2 subfolder are available next to the analyzer.
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string wv2Dir = Path.Combine(baseDir, "wv2");
                if (Directory.Exists(wv2Dir))
                {
                    foreach (string dll in Directory.GetFiles(wv2Dir, "*.dll"))
                    {
                        string dest = Path.Combine(baseDir, Path.GetFileName(dll));
                        if (!File.Exists(dest)) File.Copy(dll, dest);
                    }
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AnalyzerForm());
        }
    }
}
