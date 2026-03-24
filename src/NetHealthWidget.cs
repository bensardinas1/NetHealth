using NetHealth.Models;
using NetHealth.Services;
using NetHealth.UI;

namespace NetHealth;

public sealed class NetHealthWidget : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly MonitorService _monitorService;
    private AppConfig _config;

    public NetHealthWidget()
    {
        // This form is invisible — we only use the tray icon
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
        Size = new Size(0, 0);

        _config = ConfigService.Load();
        _monitorService = new MonitorService();
        _monitorService.Configure(_config);
        _monitorService.StatusChanged += OnStatusChanged;

        _contextMenu = BuildContextMenu();
        _trayIcon = new NotifyIcon
        {
            Text = "NetHealth — Initializing...",
            Icon = CreateStatusIcon(HealthState.Unknown),
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += OnTrayDoubleClick;

        _monitorService.Start();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Status: Initializing...").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Check Now", null, (_, _) => _ = Task.Run(() => _monitorService.Start()));
        menu.Items.Add("Configure...", null, OnConfigure);
        menu.Items.Add("Open Config Folder", null, OnOpenConfig);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, OnExit);
        return menu;
    }

    private void OnStatusChanged(IReadOnlyDictionary<string, HealthStatus> results)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnStatusChanged(results));
            return;
        }

        var overall = _monitorService.GetOverallState();

        // Update tray icon
        _trayIcon.Icon = CreateStatusIcon(overall);

        // Update tooltip with summary
        var summary = string.Join("\n", results.Values.Select(r =>
            $"{StateIcon(r.State)} {r.TargetName}: {r.LatencyMs}ms"));

        // NotifyIcon.Text max 127 chars
        _trayIcon.Text = summary.Length > 127 ? summary[..124] + "..." : summary;

        // Update context menu status
        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Items[0].Text = $"Status: {overall} ({results.Count} targets)";
        }

        // Toast notification on state change
        if (_config.NotifyOnChange)
        {
            NotificationService.CheckAndNotify(overall, _trayIcon);
        }
    }

    private void OnTrayDoubleClick(object? sender, EventArgs e)
    {
        // Show a simple status popup
        var results = _monitorService.GetLastResults();
        if (results.Count == 0)
        {
            MessageBox.Show("No results yet. Monitoring is starting...", "NetHealth",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var lines = results.Values
            .OrderBy(r => r.State)
            .Select(r => $"{StateIcon(r.State)} {r.TargetName}\n   {r.Detail}\n   Latency: {r.LatencyMs}ms | {r.TimestampUtc:HH:mm:ss} UTC");

        var overall = _monitorService.GetOverallState();
        var icon = overall switch
        {
            HealthState.Healthy => MessageBoxIcon.Information,
            HealthState.Degraded => MessageBoxIcon.Warning,
            HealthState.Unhealthy => MessageBoxIcon.Error,
            _ => MessageBoxIcon.Question
        };

        MessageBox.Show(
            $"Overall: {overall}\n\n{string.Join("\n\n", lines)}",
            "NetHealth Status",
            MessageBoxButtons.OK,
            icon);
    }

    private void OnConfigure(object? sender, EventArgs e)
    {
        using var dlg = new ConfigDialog(_config);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _config = dlg.GetUpdatedConfig();
            ConfigService.Save(_config);
            _monitorService.Configure(_config);
            _monitorService.Start();
            NotificationService.Reset();
        }
    }

    private static void OnOpenConfig(object? sender, EventArgs e)
    {
        var configDir = Path.Combine(AppContext.BaseDirectory, "config");
        if (Directory.Exists(configDir))
            System.Diagnostics.Process.Start("explorer.exe", configDir);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _monitorService.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _monitorService.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnFormClosing(e);
    }

    private static Icon CreateStatusIcon(HealthState state)
    {
        var color = state switch
        {
            HealthState.Healthy => Color.FromArgb(0, 200, 83),
            HealthState.Degraded => Color.FromArgb(255, 193, 7),
            HealthState.Unhealthy => Color.FromArgb(244, 67, 54),
            _ => Color.FromArgb(158, 158, 158)
        };

        // Generate a simple colored circle icon programmatically
        // This avoids needing external .ico files
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 1, 1, 14, 14);
            using var pen = new Pen(Color.FromArgb(80, 0, 0, 0), 1);
            g.DrawEllipse(pen, 1, 1, 14, 14);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static string StateIcon(HealthState state) => state switch
    {
        HealthState.Healthy => "●",
        HealthState.Degraded => "◐",
        HealthState.Unhealthy => "○",
        _ => "?"
    };
}
