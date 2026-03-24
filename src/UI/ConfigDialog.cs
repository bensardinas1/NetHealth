using NetHealth.Models;
using NetHealth.Services;

namespace NetHealth.UI;

public sealed class ConfigDialog : Form
{
    private readonly AppConfig _config;
    private readonly NumericUpDown _nudPollInterval;
    private readonly CheckBox _chkOverlay;
    private readonly CheckBox _chkNotify;
    private readonly ListView _lstTargets;

    public ConfigDialog(AppConfig config)
    {
        // Deep-copy so cancel discards changes
        _config = new AppConfig
        {
            PollIntervalSeconds = config.PollIntervalSeconds,
            ShowOverlay = config.ShowOverlay,
            NotifyOnChange = config.NotifyOnChange,
            Targets = config.Targets.Select(t => new TargetConfig
            {
                Name = t.Name,
                Type = t.Type,
                Enabled = t.Enabled,
                Host = t.Host,
                TimeoutMs = t.TimeoutMs,
                ThresholdMs = t.ThresholdMs,
                Resolve = t.Resolve,
                Url = t.Url,
                ExpectedStatusCode = t.ExpectedStatusCode
            }).ToList()
        };

        Text = "NetHealth Configuration";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(560, 480);

        // === Global Settings Group ===
        var grpGlobal = new GroupBox { Text = "Global Settings", Left = 12, Top = 12, Width = 520, Height = 90 };

        grpGlobal.Controls.Add(new Label { Text = "Poll Interval (sec):", Left = 12, Top = 25, AutoSize = true });
        _nudPollInterval = new NumericUpDown { Left = 150, Top = 22, Width = 80, Minimum = 5, Maximum = 3600, Value = _config.PollIntervalSeconds, Increment = 5 };
        grpGlobal.Controls.Add(_nudPollInterval);

        _chkOverlay = new CheckBox { Text = "Show Overlay", Left = 260, Top = 24, Checked = _config.ShowOverlay, AutoSize = true };
        grpGlobal.Controls.Add(_chkOverlay);

        _chkNotify = new CheckBox { Text = "Toast Notifications", Left = 260, Top = 50, Checked = _config.NotifyOnChange, AutoSize = true };
        grpGlobal.Controls.Add(_chkNotify);

        Controls.Add(grpGlobal);

        // === Targets Group ===
        var grpTargets = new GroupBox { Text = "Monitored Targets", Left = 12, Top = 110, Width = 520, Height = 280 };

        _lstTargets = new ListView
        {
            Left = 12, Top = 22, Width = 400, Height = 245,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };
        _lstTargets.Columns.Add("Name", 140);
        _lstTargets.Columns.Add("Type", 60);
        _lstTargets.Columns.Add("Target", 140);
        _lstTargets.Columns.Add("Enabled", 55);
        grpTargets.Controls.Add(_lstTargets);

        var btnAdd = new Button { Text = "Add", Left = 420, Top = 22, Width = 88 };
        btnAdd.Click += OnAdd;
        grpTargets.Controls.Add(btnAdd);

        var btnEdit = new Button { Text = "Edit", Left = 420, Top = 56, Width = 88 };
        btnEdit.Click += OnEdit;
        grpTargets.Controls.Add(btnEdit);

        var btnRemove = new Button { Text = "Remove", Left = 420, Top = 90, Width = 88 };
        btnRemove.Click += OnRemove;
        grpTargets.Controls.Add(btnRemove);

        var btnMoveUp = new Button { Text = "Move Up", Left = 420, Top = 136, Width = 88 };
        btnMoveUp.Click += OnMoveUp;
        grpTargets.Controls.Add(btnMoveUp);

        var btnMoveDown = new Button { Text = "Move Down", Left = 420, Top = 170, Width = 88 };
        btnMoveDown.Click += OnMoveDown;
        grpTargets.Controls.Add(btnMoveDown);

        Controls.Add(grpTargets);

        // === Bottom Buttons ===
        var btnSave = new Button { Text = "Save", Left = 340, Top = 400, Width = 90, DialogResult = DialogResult.OK };
        btnSave.Click += OnSave;
        Controls.Add(btnSave);

        var btnCancel = new Button { Text = "Cancel", Left = 440, Top = 400, Width = 90, DialogResult = DialogResult.Cancel };
        Controls.Add(btnCancel);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        RefreshTargetList();
    }

    public AppConfig GetUpdatedConfig() => _config;

    private void RefreshTargetList()
    {
        _lstTargets.Items.Clear();
        foreach (var t in _config.Targets)
        {
            var target = t.Type.ToLowerInvariant() switch
            {
                "ping" => t.Host ?? "",
                "dns" => $"{t.Host} → {t.Resolve}",
                "http" => t.Url ?? "",
                _ => ""
            };

            var item = new ListViewItem([t.Name, t.Type, target, t.Enabled ? "Yes" : "No"]);
            _lstTargets.Items.Add(item);
        }
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        using var dlg = new TargetEditDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.Targets.Add(dlg.Result);
            RefreshTargetList();
        }
    }

    private void OnEdit(object? sender, EventArgs e)
    {
        var idx = _lstTargets.SelectedIndices.Count > 0 ? _lstTargets.SelectedIndices[0] : -1;
        if (idx < 0) return;

        using var dlg = new TargetEditDialog(_config.Targets[idx]);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.Targets[idx] = dlg.Result;
            RefreshTargetList();
        }
    }

    private void OnRemove(object? sender, EventArgs e)
    {
        var idx = _lstTargets.SelectedIndices.Count > 0 ? _lstTargets.SelectedIndices[0] : -1;
        if (idx < 0) return;

        var name = _config.Targets[idx].Name;
        if (MessageBox.Show($"Remove target \"{name}\"?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _config.Targets.RemoveAt(idx);
            RefreshTargetList();
        }
    }

    private void OnMoveUp(object? sender, EventArgs e)
    {
        var idx = _lstTargets.SelectedIndices.Count > 0 ? _lstTargets.SelectedIndices[0] : -1;
        if (idx <= 0) return;

        (_config.Targets[idx - 1], _config.Targets[idx]) = (_config.Targets[idx], _config.Targets[idx - 1]);
        RefreshTargetList();
        _lstTargets.Items[idx - 1].Selected = true;
    }

    private void OnMoveDown(object? sender, EventArgs e)
    {
        var idx = _lstTargets.SelectedIndices.Count > 0 ? _lstTargets.SelectedIndices[0] : -1;
        if (idx < 0 || idx >= _config.Targets.Count - 1) return;

        (_config.Targets[idx + 1], _config.Targets[idx]) = (_config.Targets[idx], _config.Targets[idx + 1]);
        RefreshTargetList();
        _lstTargets.Items[idx + 1].Selected = true;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _config.PollIntervalSeconds = (int)_nudPollInterval.Value;
        _config.ShowOverlay = _chkOverlay.Checked;
        _config.NotifyOnChange = _chkNotify.Checked;
    }
}
