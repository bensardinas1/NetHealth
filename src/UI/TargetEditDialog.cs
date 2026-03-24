using NetHealth.Models;

namespace NetHealth.UI;

public sealed class TargetEditDialog : Form
{
    private readonly TextBox _txtName;
    private readonly ComboBox _cboType;
    private readonly CheckBox _chkEnabled;
    private readonly TextBox _txtHost;
    private readonly NumericUpDown _nudTimeout;
    private readonly NumericUpDown _nudThreshold;
    private readonly TextBox _txtResolve;
    private readonly TextBox _txtUrl;
    private readonly NumericUpDown _nudExpectedStatus;

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
        Size = new Size(420, 400);
        Padding = new Padding(12);

        var y = 12;

        // --- Name ---
        AddLabel("Name:", 12, y);
        _txtName = new TextBox { Left = 130, Top = y, Width = 250 };
        Controls.Add(_txtName);
        y += 30;

        // --- Type ---
        AddLabel("Type:", 12, y);
        _cboType = new ComboBox
        {
            Left = 130, Top = y, Width = 250,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboType.Items.AddRange(["ping", "dns", "http"]);
        _cboType.SelectedIndexChanged += (_, _) => ShowTypePanel();
        Controls.Add(_cboType);
        y += 30;

        // --- Enabled ---
        _chkEnabled = new CheckBox { Text = "Enabled", Left = 130, Top = y, Checked = true, AutoSize = true };
        Controls.Add(_chkEnabled);
        y += 30;

        // --- Timeout ---
        AddLabel("Timeout (ms):", 12, y);
        _nudTimeout = new NumericUpDown { Left = 130, Top = y, Width = 120, Minimum = 100, Maximum = 60000, Value = 2000, Increment = 100 };
        Controls.Add(_nudTimeout);
        y += 30;

        // === Ping Panel ===
        _panelPing = new Panel { Left = 0, Top = y, Width = 400, Height = 60, Visible = false };
        var pingY = 0;
        _panelPing.Controls.Add(new Label { Text = "Host / IP:", Left = 12, Top = pingY + 3, AutoSize = true });
        _txtHost = new TextBox { Left = 130, Top = pingY, Width = 250 };
        _panelPing.Controls.Add(_txtHost);
        pingY += 30;
        _panelPing.Controls.Add(new Label { Text = "Threshold (ms):", Left = 12, Top = pingY + 3, AutoSize = true });
        _nudThreshold = new NumericUpDown { Left = 130, Top = pingY, Width = 120, Minimum = 1, Maximum = 10000, Value = 100, Increment = 10 };
        _panelPing.Controls.Add(_nudThreshold);
        Controls.Add(_panelPing);

        // === DNS Panel ===
        _panelDns = new Panel { Left = 0, Top = y, Width = 400, Height = 60, Visible = false };
        var dnsY = 0;
        _panelDns.Controls.Add(new Label { Text = "DNS Server:", Left = 12, Top = dnsY + 3, AutoSize = true });
        var txtDnsHost = new TextBox { Name = "txtDnsHost", Left = 130, Top = dnsY, Width = 250 };
        _panelDns.Controls.Add(txtDnsHost);
        dnsY += 30;
        _panelDns.Controls.Add(new Label { Text = "Resolve Domain:", Left = 12, Top = dnsY + 3, AutoSize = true });
        _txtResolve = new TextBox { Left = 130, Top = dnsY, Width = 250 };
        _panelDns.Controls.Add(_txtResolve);
        Controls.Add(_panelDns);

        // === HTTP Panel ===
        _panelHttp = new Panel { Left = 0, Top = y, Width = 400, Height = 60, Visible = false };
        var httpY = 0;
        _panelHttp.Controls.Add(new Label { Text = "URL:", Left = 12, Top = httpY + 3, AutoSize = true });
        _txtUrl = new TextBox { Left = 130, Top = httpY, Width = 250 };
        _panelHttp.Controls.Add(_txtUrl);
        httpY += 30;
        _panelHttp.Controls.Add(new Label { Text = "Expected Status:", Left = 12, Top = httpY + 3, AutoSize = true });
        _nudExpectedStatus = new NumericUpDown { Left = 130, Top = httpY, Width = 120, Minimum = 100, Maximum = 599, Value = 200 };
        _panelHttp.Controls.Add(_nudExpectedStatus);
        Controls.Add(_panelHttp);

        // --- Buttons ---
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 190, Top = 320, Width = 90 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 290, Top = 320, Width = 90 };
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
        _nudTimeout.Value = Math.Clamp(t.TimeoutMs, (int)_nudTimeout.Minimum, (int)_nudTimeout.Maximum);
        _txtHost.Text = t.Host ?? "";
        _nudThreshold.Value = Math.Clamp(t.ThresholdMs, (int)_nudThreshold.Minimum, (int)_nudThreshold.Maximum);
        _txtResolve.Text = t.Resolve ?? "";
        _txtUrl.Text = t.Url ?? "";
        _nudExpectedStatus.Value = Math.Clamp(t.ExpectedStatusCode, (int)_nudExpectedStatus.Minimum, (int)_nudExpectedStatus.Maximum);

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
            MessageBox.Show("Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var type = _cboType.SelectedItem?.ToString() ?? "ping";

        Result = new TargetConfig
        {
            Name = _txtName.Text.Trim(),
            Type = type,
            Enabled = _chkEnabled.Checked,
            TimeoutMs = (int)_nudTimeout.Value,
            ThresholdMs = (int)_nudThreshold.Value,
            ExpectedStatusCode = (int)_nudExpectedStatus.Value
        };

        switch (type)
        {
            case "ping":
                if (string.IsNullOrWhiteSpace(_txtHost.Text))
                {
                    MessageBox.Show("Host is required for ping targets.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                Result.Host = _txtHost.Text.Trim();
                break;
            case "dns":
                var dnsHost = (_panelDns.Controls["txtDnsHost"] as TextBox)?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(dnsHost) || string.IsNullOrWhiteSpace(_txtResolve.Text))
                {
                    MessageBox.Show("DNS server and resolve domain are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                Result.Host = dnsHost;
                Result.Resolve = _txtResolve.Text.Trim();
                break;
            case "http":
                if (string.IsNullOrWhiteSpace(_txtUrl.Text) || !Uri.TryCreate(_txtUrl.Text.Trim(), UriKind.Absolute, out _))
                {
                    MessageBox.Show("A valid URL is required for HTTP targets.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                Result.Url = _txtUrl.Text.Trim();
                break;
        }
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label { Text = text, Left = x, Top = y + 3, AutoSize = true });
    }
}
