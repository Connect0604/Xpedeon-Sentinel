namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for dashboard configuration management
/// </summary>
public interface IDashboardConfigService
{
    // Views
    Task<DashboardView> CreateViewAsync(DashboardView view);
    Task<DashboardView?> GetViewAsync(string viewId);
    Task<List<DashboardView>> GetAllViewsAsync();
    Task<DashboardView?> GetDefaultViewAsync();
    Task<bool> UpdateViewAsync(DashboardView view);
    Task<bool> DeleteViewAsync(string viewId);

    // Widgets
    Task<DashboardWidget> AddWidgetAsync(string viewId, DashboardWidget widget);
    Task<bool> UpdateWidgetAsync(string viewId, DashboardWidget widget);
    Task<bool> RemoveWidgetAsync(string viewId, string widgetId);
    Task<List<DashboardWidget>> GetWidgetsAsync(string viewId);

    // User Configuration
    Task<UserDashboardConfig> GetUserConfigAsync(string userId);
    Task<bool> UpdateUserConfigAsync(string userId, UserDashboardConfig config);
    Task<bool> SetDefaultViewAsync(string userId, string viewId);
    Task<bool> SetThemeAsync(string userId, string theme);
    Task<bool> AddFavoriteViewAsync(string userId, string viewId);
    Task<bool> RemoveFavoriteViewAsync(string userId, string viewId);

    // System Configuration
    Task<SystemDashboardConfig> GetSystemConfigAsync();
    Task<bool> UpdateSystemConfigAsync(SystemDashboardConfig config);

    // Color Schemes
    Task<ColorScheme> CreateColorSchemeAsync(ColorScheme scheme);
    Task<ColorScheme?> GetColorSchemeAsync(string name);
    Task<List<ColorScheme>> GetAllColorSchemesAsync();

    // Presets
    Task<WidgetPreset> CreateWidgetPresetAsync(WidgetPreset preset);
    Task<WidgetPreset?> GetWidgetPresetAsync(string presetId);
    Task<List<WidgetPreset>> GetAllWidgetPresetsAsync();

    Task<LayoutPreset> CreateLayoutPresetAsync(LayoutPreset preset);
    Task<LayoutPreset?> GetLayoutPresetAsync(string presetId);
    Task<List<LayoutPreset>> GetAllLayoutPresetsAsync();
}
