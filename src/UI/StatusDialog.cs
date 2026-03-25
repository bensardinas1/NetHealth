using NetHealth.Models;
using NetHealth.Services;

namespace NetHealth.UI;

public sealed class StatusDialog : Form
{
    private readonly MonitorService _monitorService;
    private readonly TextBox _txtContent;
    private readonly Button _btnOk;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private const int MaxFormHeight = 900;
    private const int Pad = 16;
    private const int BtnHeight = 30;
    private const int ContentWidth = 480;

    public StatusDialog(MonitorService monitorService)
    {
        _monitorService = monitorService;

        Text = "NetHealth Status";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        ShowInTaskbar = false;

        _txtContent = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = BackColor,
            Left = Pad,
            Top = Pad,
            Width = ContentWidth,
            Font = new Font("Segoe UI", 9.5f),
            ScrollBars = ScrollBars.None,
            WordWrap = true
        };
        Controls.Add(_txtContent);

        _btnOk = new Button
        {
            Text = "OK",
            Width = 90,
            Height = BtnHeight
        };
        _btnOk.Click += (_, _) => Close();
        Controls.Add(_btnOk);
        AcceptButton = _btnOk;

        // Refresh every 2 seconds
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _refreshTimer.Tick += (_, _) => RefreshContent();
        _refreshTimer.Start();

        RefreshContent();
    }

    private void LayoutToContent()
    {
        // Measure how tall the text needs
        var textHeight = TextRenderer.MeasureText(
            _txtContent.Text, _txtContent.Font,
            new Size(ContentWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl
        ).Height + 8; // small buffer for TextBox internal padding

        var idealClientHeight = Pad + textHeight + Pad + BtnHeight + Pad;
        var maxClientHeight = MaxFormHeight - (Height - ClientSize.Height); // account for title bar

        if (idealClientHeight <= maxClientHeight)
        {
            // Fits — no scrollbar needed
            _txtContent.ScrollBars = ScrollBars.None;
            _txtContent.Height = textHeight;
            ClientSize = new Size(Pad + ContentWidth + Pad, idealClientHeight);
        }
        else
        {
            // Capped at max — enable scrollbar
            _txtContent.ScrollBars = ScrollBars.Vertical;
            var contentAreaHeight = maxClientHeight - Pad - Pad - BtnHeight - Pad;
            _txtContent.Height = contentAreaHeight;
            ClientSize = new Size(Pad + ContentWidth + Pad, maxClientHeight);
        }

        _btnOk.Location = new Point(ClientSize.Width - _btnOk.Width - Pad, ClientSize.Height - BtnHeight - Pad);
    }

    private void RefreshContent()
    {
        var results = _monitorService.GetLastResults();
        if (results.Count == 0)
        {
            _txtContent.Text = "No results yet. Monitoring is starting...";
            LayoutToContent();
            return;
        }

        var stats = _monitorService.GetStats();
        var addresses = _monitorService.GetTargetAddresses();
        var overall = _monitorService.GetOverallState();

        var lines = results.Values
            .OrderBy(r => r.State)
            .Select(r =>
            {
                var addr = addresses.TryGetValue(r.TargetName, out var a) ? a : "";
                var displayName = string.IsNullOrEmpty(addr)
                    ? r.TargetName
                    : $"{r.TargetName} [{addr}]";

                var local = r.TimestampUtc.ToLocalTime();
                var avg = stats.TryGetValue(r.TargetName, out var s) ? s.GetRollingAverageMs() : 0;
                var lastFail = stats.TryGetValue(r.TargetName, out var s2) && s2.LastFailureUtc.HasValue
                    ? $"{s2.LastFailureUtc.Value.ToLocalTime():HH:mm:ss} / {s2.LastFailureUtc.Value:HH:mm:ss} UTC"
                    : "Never";

                return $"{StateIcon(r.State)} {displayName}\r\n" +
                       $"   {r.Detail}\r\n" +
                       $"   Last: {r.LatencyMs}ms | Avg (5m): {avg:F0}ms\r\n" +
                       $"   Last Failure: {lastFail}\r\n" +
                       $"   {local:HH:mm:ss} local | {r.TimestampUtc:HH:mm:ss} UTC";
            });

        var text = $"Overall: {overall}\r\n\r\n{string.Join("\r\n\r\n", lines)}";
        if (_txtContent.Text != text)
        {
            var pos = _txtContent.SelectionStart;
            _txtContent.Text = text;
            _txtContent.SelectionStart = pos;
            _txtContent.SelectionLength = 0;
            LayoutToContent();
        }
    }

    private static string StateIcon(HealthState state) => state switch
    {
        HealthState.Healthy => "\u25CF",   // filled circle
        HealthState.Degraded => "\u25B2",  // triangle
        HealthState.Unhealthy => "\u2716", // X
        _ => "\u25CB"                       // empty circle
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
