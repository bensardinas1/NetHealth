using NetHealth.Models;
using NetHealth.Services;

namespace NetHealth.UI;

public sealed class ConfigDialog : Form
{
    private readonly AppConfig _config;
    private readonly CheckBox _chkOverlay;
    private readonly CheckBox _chkNotify;
    private readonly ListView _lstTargets;
    private readonly Button _btnAdd;
    private readonly Button _btnEdit;
    private readonly Button _btnRemove;
    private readonly Button _btnMoveUp;
    private readonly Button _btnMoveDown;
    private readonly Button _btnSave;
    private readonly Button _btnCancel;
    private readonly GroupBox _grpGlobal;
    private readonly GroupBox _grpTargets;

    public ConfigDialog(AppConfig config)
    {
        // Deep-copy so cancel discards changes
        _config = new AppConfig
        {
            ShowOverlay = config.ShowOverlay,
            NotifyOnChange = config.NotifyOnChange,
            Targets = config.Targets.Select(t => new TargetConfig
            {
                Name = t.Name,
                Type = t.Type,
                Enabled = t.Enabled,
                PollIntervalSeconds = t.PollIntervalSeconds,
                Host = t.Host,
                TimeoutMs = t.TimeoutMs,
                ThresholdMs = t.ThresholdMs,
                Resolve = t.Resolve,
                Url = t.Url,
                ExpectedStatusCode = t.ExpectedStatusCode
            }).ToList()
        };

        Text = "NetHealth Configuration";
        MinimumSize = new Size(700, 440);
        Size = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;

        // === Global Settings Group ===
        _grpGlobal = new GroupBox { Text = "Global Settings", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _chkOverlay = new CheckBox { Text = "Show Overlay", Checked = _config.ShowOverlay, AutoSize = true };
        _chkNotify = new CheckBox { Text = "Toast Notifications", Checked = _config.NotifyOnChange, AutoSize = true };
        _grpGlobal.Controls.Add(_chkOverlay);
        _grpGlobal.Controls.Add(_chkNotify);
        Controls.Add(_grpGlobal);

        // === Targets Group ===
        _grpTargets = new GroupBox { Text = "Monitored Targets", Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

        _lstTargets = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _lstTargets.Columns.Add("Name", 150);
        _lstTargets.Columns.Add("Type", 55);
        _lstTargets.Columns.Add("Target", 230);
        _lstTargets.Columns.Add("Poll (s)", 65);
        _lstTargets.Columns.Add("On", 42);
        _lstTargets.DoubleClick += OnEdit;
        _grpTargets.Controls.Add(_lstTargets);

        _btnAdd = new Button { Text = "Add", Width = 90, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnAdd.Click += OnAdd;
        _grpTargets.Controls.Add(_btnAdd);

        _btnEdit = new Button { Text = "Edit", Width = 90, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnEdit.Click += OnEdit;
        _grpTargets.Controls.Add(_btnEdit);

        _btnRemove = new Button { Text = "Remove", Width = 90, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnRemove.Click += OnRemove;
        _grpTargets.Controls.Add(_btnRemove);

        _btnMoveUp = new Button { Text = "Move Up", Width = 90, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnMoveUp.Click += OnMoveUp;
        _grpTargets.Controls.Add(_btnMoveUp);

        _btnMoveDown = new Button { Text = "Move Down", Width = 90, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnMoveDown.Click += OnMoveDown;
        _grpTargets.Controls.Add(_btnMoveDown);

        Controls.Add(_grpTargets);

        // === Bottom Buttons ===
        _btnSave = new Button { Text = "Save", Width = 90, Height = 30, DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        _btnSave.Click += OnSave;
        Controls.Add(_btnSave);

        _btnCancel = new Button { Text = "Cancel", Width = 90, Height = 30, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        Controls.Add(_btnCancel);
        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        Resize += (_, _) => DoLayout();
        DoLayout();
        RefreshTargetList();
    }

    private void DoLayout()
    {
        var pad = 12;
        var cw = ClientSize.Width;
        var ch = ClientSize.Height;

        // Global settings
        _grpGlobal.SetBounds(pad, pad, cw - pad * 2, 55);
        _chkOverlay.SetBounds(16, 22, 140, 22);
        _chkNotify.SetBounds(170, 22, 170, 22);

        // Bottom buttons
        _btnCancel.SetBounds(cw - pad - 90, ch - pad - 30, 90, 30);
        _btnSave.SetBounds(cw - pad - 90 - 8 - 90, ch - pad - 30, 90, 30);

        // Targets group fills remaining space
        var targetsTop = _grpGlobal.Bottom + pad;
        var targetsBottom = _btnSave.Top - pad;
        _grpTargets.SetBounds(pad, targetsTop, cw - pad * 2, targetsBottom - targetsTop);

        // Buttons inside targets group
        var btnLeft = _grpTargets.Width - pad - 90;
        _btnAdd.SetBounds(btnLeft, 24, 90, 30);
        _btnEdit.SetBounds(btnLeft, 60, 90, 30);
        _btnRemove.SetBounds(btnLeft, 96, 90, 30);
        _btnMoveUp.SetBounds(btnLeft, 142, 90, 30);
        _btnMoveDown.SetBounds(btnLeft, 178, 90, 30);

        // ListView fills targets group minus button column
        _lstTargets.SetBounds(pad, 24, btnLeft - pad - 8, _grpTargets.Height - 24 - pad);
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

            var item = new ListViewItem([t.Name, t.Type, target, t.PollIntervalSeconds.ToString(), t.Enabled ? "Yes" : "No"]);
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
        _config.ShowOverlay = _chkOverlay.Checked;
        _config.NotifyOnChange = _chkNotify.Checked;
    }
}
