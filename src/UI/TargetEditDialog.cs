using NetHealth.Models;

namespace NetHealth.UI;

public sealed class TargetEditDialog : Form
{
    private readonly TextBox _txtName;
    private readonly ComboBox _cboType;
    private readonly CheckBox _chkEnabled;
    private readonly TextBox _txtPollInterval;
    private readonly TextBox _txtTimeout;
    private readonly TextBox _txtHost;
    private readonly TextBox _txtThreshold;
    private readonly TextBox _txtResolve;
    private readonly TextBox _txtUrl;
    private readonly TextBox _txtExpectedStatus;

    // Dynamic panels for type-specific fields
    private readonly Panel _panelPing;
    private readonly Panel _panelDns;
    private readonly Panel _panelHttp;

    public TargetConfig Result { get; private set; } = new();

    public TargetEditDialog(TargetConfig? existing = null)
    {
        Text = existing == null ? "Add Target" : "Edit Target";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(440, 440);
        Padding = new Padding(12);

        var y = 12;
        const int labelX = 12;
        const int fieldX = 140;
        const int fieldW = 260;

        // --- Name ---
        AddLabel("Name:", labelX, y);
        _txtName = new TextBox { Left = fieldX, Top = y, Width = fieldW };
        Controls.Add(_txtName);
        y += 30;

        // --- Type ---
        AddLabel("Type:", labelX, y);
        _cboType = new ComboBox
        {
            Left = fieldX, Top = y, Width = fieldW,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboType.Items.AddRange(["ping", "dns", "http"]);
        _cboType.SelectedIndexChanged += (_, _) => ShowTypePanel();
        Controls.Add(_cboType);
        y += 30;

        // --- Enabled ---
        _chkEnabled = new CheckBox { Text = "Enabled", Left = fieldX, Top = y, Checked = true, AutoSize = true };
        Controls.Add(_chkEnabled);
        y += 30;

        // --- Poll Interval ---
        AddLabel("Poll Interval (sec):", labelX, y);
        _txtPollInterval = new TextBox { Left = fieldX, Top = y, Width = 80, Text = "30" };
        Controls.Add(_txtPollInterval);
        y += 30;

        // --- Timeout ---
        AddLabel("Timeout (ms):", labelX, y);
        _txtTimeout = new TextBox { Left = fieldX, Top = y, Width = 80, Text = "2000" };
        Controls.Add(_txtTimeout);
        y += 30;

        // === Ping Panel ===
        _panelPing = new Panel { Left = 0, Top = y, Width = 420, Height = 65, Visible = false };
        var pingY = 0;
        _panelPing.Controls.Add(new Label { Text = "Host / IP:", Left = labelX, Top = pingY + 3, AutoSize = true });
        _txtHost = new TextBox { Left = fieldX, Top = pingY, Width = 160 };
        _panelPing.Controls.Add(_txtHost);
        _panelPing.Controls.Add(new Label { Text = "(\"auto\" = detect gateway)", Left = fieldX + 165, Top = pingY + 3, AutoSize = true, ForeColor = Color.Gray });
        pingY += 30;
        _panelPing.Controls.Add(new Label { Text = "Threshold (ms):", Left = labelX, Top = pingY + 3, AutoSize = true });
        _txtThreshold = new TextBox { Left = fieldX, Top = pingY, Width = 80, Text = "100" };
        _panelPing.Controls.Add(_txtThreshold);
        Controls.Add(_panelPing);

        // === DNS Panel ===
        _panelDns = new Panel { Left = 0, Top = y, Width = 420, Height = 65, Visible = false };
        var dnsY = 0;
        _panelDns.Controls.Add(new Label { Text = "DNS Server:", Left = labelX, Top = dnsY + 3, AutoSize = true });
        var txtDnsHost = new TextBox { Name = "txtDnsHost", Left = fieldX, Top = dnsY, Width = fieldW };
        _panelDns.Controls.Add(txtDnsHost);
        dnsY += 30;
        _panelDns.Controls.Add(new Label { Text = "Resolve Domain:", Left = labelX, Top = dnsY + 3, AutoSize = true });
        _txtResolve = new TextBox { Left = fieldX, Top = dnsY, Width = fieldW };
        _panelDns.Controls.Add(_txtResolve);
        Controls.Add(_panelDns);

        // === HTTP Panel ===
        _panelHttp = new Panel { Left = 0, Top = y, Width = 420, Height = 65, Visible = false };
        var httpY = 0;
        _panelHttp.Controls.Add(new Label { Text = "URL:", Left = labelX, Top = httpY + 3, AutoSize = true });
        _txtUrl = new TextBox { Left = fieldX, Top = httpY, Width = fieldW };
        _panelHttp.Controls.Add(_txtUrl);
        httpY += 30;
        _panelHttp.Controls.Add(new Label { Text = "Expected Status:", Left = labelX, Top = httpY + 3, AutoSize = true });
        _txtExpectedStatus = new TextBox { Left = fieldX, Top = httpY, Width = 80, Text = "200" };
        _panelHttp.Controls.Add(_txtExpectedStatus);
        Controls.Add(_panelHttp);

        // --- Buttons ---
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 220, Top = 360, Width = 90, Height = 30 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 320, Top = 360, Width = 90, Height = 30 };
        btnOk.Click += OnOk;
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        // Load existing values
        if (existing != null)
            LoadTarget(existing);
        else
            _cboType.SelectedIndex = 0;
    }

    private void LoadTarget(TargetConfig t)
    {
        _txtName.Text = t.Name;
        _cboType.SelectedItem = t.Type.ToLowerInvariant();
        _chkEnabled.Checked = t.Enabled;
        _txtPollInterval.Text = t.PollIntervalSeconds.ToString();
        _txtTimeout.Text = t.TimeoutMs.ToString();
        _txtHost.Text = t.Host ?? "";
        _txtThreshold.Text = t.ThresholdMs.ToString();
        _txtResolve.Text = t.Resolve ?? "";
        _txtUrl.Text = t.Url ?? "";
        _txtExpectedStatus.Text = t.ExpectedStatusCode.ToString();

        // Also set DNS host field
        var dnsHost = _panelDns.Controls["txtDnsHost"] as TextBox;
        if (dnsHost != null) dnsHost.Text = t.Host ?? "";

        ShowTypePanel();
    }

    private void ShowTypePanel()
    {
        var type = _cboType.SelectedItem?.ToString() ?? "ping";
        _panelPing.Visible = type == "ping";
        _panelDns.Visible = type == "dns";
        _panelHttp.Visible = type == "http";
    }

    private void OnOk(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            ShowError("Name is required.");
            return;
        }

        if (!int.TryParse(_txtPollInterval.Text, out var pollInterval) || pollInterval < 5)
        {
            ShowError("Poll interval must be a number >= 5.");
            return;
        }

        if (!int.TryParse(_txtTimeout.Text, out var timeout) || timeout < 100)
        {
            ShowError("Timeout must be a number >= 100.");
            return;
        }

        var type = _cboType.SelectedItem?.ToString() ?? "ping";

        Result = new TargetConfig
        {
            Name = _txtName.Text.Trim(),
            Type = type,
            Enabled = _chkEnabled.Checked,
            PollIntervalSeconds = pollInterval,
            TimeoutMs = timeout
        };

        switch (type)
        {
            case "ping":
                if (string.IsNullOrWhiteSpace(_txtHost.Text))
                {
                    ShowError("Host is required for ping targets.");
                    return;
                }
                if (!int.TryParse(_txtThreshold.Text, out var threshold) || threshold < 1)
                {
                    ShowError("Threshold must be a number >= 1.");
                    return;
                }
                Result.Host = _txtHost.Text.Trim();
                Result.ThresholdMs = threshold;
                break;
            case "dns":
                var dnsHost = (_panelDns.Controls["txtDnsHost"] as TextBox)?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(dnsHost) || string.IsNullOrWhiteSpace(_txtResolve.Text))
                {
                    ShowError("DNS server and resolve domain are required.");
                    return;
                }
                Result.Host = dnsHost;
                Result.Resolve = _txtResolve.Text.Trim();
                break;
            case "http":
                if (string.IsNullOrWhiteSpace(_txtUrl.Text) || !Uri.TryCreate(_txtUrl.Text.Trim(), UriKind.Absolute, out _))
                {
                    ShowError("A valid URL is required for HTTP targets.");
                    return;
                }
                if (!int.TryParse(_txtExpectedStatus.Text, out var status) || status < 100 || status > 599)
                {
                    ShowError("Expected status must be 100-599.");
                    return;
                }
                Result.Url = _txtUrl.Text.Trim();
                Result.ExpectedStatusCode = status;
                break;
        }
    }

    private void ShowError(string msg)
    {
        MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        DialogResult = DialogResult.None;
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label { Text = text, Left = x, Top = y + 3, AutoSize = true });
    }
}
