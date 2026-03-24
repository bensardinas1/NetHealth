using NetHealth.Models;

namespace NetHealth.Services;

public static class NotificationService
{
    private static HealthState _lastOverallState = HealthState.Unknown;

    public static void CheckAndNotify(HealthState currentState, NotifyIcon trayIcon)
    {
        if (currentState == _lastOverallState)
            return;

        var previousState = _lastOverallState;
        _lastOverallState = currentState;

        // Don't notify on first check (Unknown → anything)
        if (previousState == HealthState.Unknown)
            return;

        var (title, text) = currentState switch
        {
            HealthState.Healthy => ("Network Healthy", "All monitored targets are responding normally."),
            HealthState.Degraded => ("Network Degraded", "One or more targets are responding slowly."),
            HealthState.Unhealthy => ("Network Unhealthy", "One or more targets are unreachable."),
            _ => ("Network Status Unknown", "Unable to determine network status.")
        };

        var tipIcon = currentState switch
        {
            HealthState.Healthy => ToolTipIcon.Info,
            HealthState.Degraded => ToolTipIcon.Warning,
            HealthState.Unhealthy => ToolTipIcon.Error,
            _ => ToolTipIcon.None
        };

        trayIcon.ShowBalloonTip(3000, title, text, tipIcon);
    }

    public static void Reset()
    {
        _lastOverallState = HealthState.Unknown;
    }
}
