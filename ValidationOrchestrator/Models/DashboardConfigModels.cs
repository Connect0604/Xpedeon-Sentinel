namespace ValidationOrchestrator.Models;

/// <summary>
/// Dashboard view configuration
/// </summary>
public class DashboardView
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // executive, technical, manager, analyst
    public bool IsDefault { get; set; }
    public List<DashboardWidget> Widgets { get; set; } = new();
    public Dictionary<string, object> LayoutConfig { get; set; } = new();
}

/// <summary>
/// Individual dashboard widget
/// </summary>
public class DashboardWidget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // progress, metrics, alerts, timeline, etc.
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public int Width { get; set; } = 4; // 1-12 grid units
    public int Height { get; set; } = 300; // pixels
    public bool IsVisible { get; set; } = true;
    public Dictionary<string, object> Config { get; set; } = new();
    public List<string> DataSources { get; set; } = new();
}

/// <summary>
/// User-specific dashboard configuration
/// </summary>
public class UserDashboardConfig
{
    public string UserId { get; set; } = string.Empty;
    public string DefaultViewId { get; set; } = string.Empty;
    public string Theme { get; set; } = "light"; // light, dark, auto
    public int RefreshIntervalSeconds { get; set; } = 5;
    public bool ShowMetrics { get; set; } = true;
    public bool ShowAlerts { get; set; } = true;
    public bool ShowTimeline { get; set; } = true;
    public List<string> FavoriteViews { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System-wide dashboard configuration
/// </summary>
public class SystemDashboardConfig
{
    public int MaxConcurrentDashboards { get; set; } = 100;
    public int DefaultRefreshIntervalSeconds { get; set; } = 5;
    public int MaxHistoryPoints { get; set; } = 1000;
    public bool EnableRealtimeUpdates { get; set; } = true;
    public bool EnableCharts { get; set; } = true;
    public bool EnableExport { get; set; } = true;
    public int MaxExportSizeMb { get; set; } = 100;
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string TimeZone { get; set; } = "UTC";
    public Dictionary<string, string> ColorSchemes { get; set; } = new();
}

/// <summary>
/// Dashboard color scheme
/// </summary>
public class ColorScheme
{
    public string Name { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#0066CC";
    public string SuccessColor { get; set; } = "#00AA00";
    public string WarningColor { get; set; } = "#FFAA00";
    public string ErrorColor { get; set; } = "#CC0000";
    public string CriticalColor { get; set; } = "#AA0000";
    public string TextColor { get; set; } = "#333333";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string BorderColor { get; set; } = "#CCCCCC";
}

/// <summary>
/// Widget preset configuration
/// </summary>
public class WidgetPreset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> DefaultConfig { get; set; } = new();
    public List<string> SupportedDataSources { get; set; } = new();
}

/// <summary>
/// Dashboard layout preset
/// </summary>
public class LayoutPreset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DashboardWidget> Widgets { get; set; } = new();
    public Dictionary<string, object> LayoutConfig { get; set; } = new();
    public string TargetRole { get; set; } = string.Empty;
}
