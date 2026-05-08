namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Implementation of dashboard configuration management
/// </summary>
public class DashboardConfigService : IDashboardConfigService
{
    private readonly Dictionary<string, DashboardView> _views = new();
    private readonly Dictionary<string, UserDashboardConfig> _userConfigs = new();
    private readonly Dictionary<string, ColorScheme> _colorSchemes = new();
    private readonly Dictionary<string, WidgetPreset> _widgetPresets = new();
    private readonly Dictionary<string, LayoutPreset> _layoutPresets = new();
    private SystemDashboardConfig _systemConfig = new();

    public async Task<DashboardView> CreateViewAsync(DashboardView view)
    {
        if (string.IsNullOrEmpty(view.Id))
        {
            view.Id = Guid.NewGuid().ToString();
        }

        _views[view.Id] = view;
        return view;
    }

    public async Task<DashboardView?> GetViewAsync(string viewId)
    {
        _views.TryGetValue(viewId, out var view);
        return view;
    }

    public async Task<List<DashboardView>> GetAllViewsAsync()
    {
        return _views.Values.ToList();
    }

    public async Task<DashboardView?> GetDefaultViewAsync()
    {
        return _views.Values.FirstOrDefault(v => v.IsDefault);
    }

    public async Task<bool> UpdateViewAsync(DashboardView view)
    {
        _views[view.Id] = view;
        return true;
    }

    public async Task<bool> DeleteViewAsync(string viewId)
    {
        return _views.Remove(viewId);
    }

    public async Task<DashboardWidget> AddWidgetAsync(string viewId, DashboardWidget widget)
    {
        if (_views.TryGetValue(viewId, out var view))
        {
            if (string.IsNullOrEmpty(widget.Id))
            {
                widget.Id = Guid.NewGuid().ToString();
            }

            widget.Order = view.Widgets.Count + 1;
            view.Widgets.Add(widget);
        }

        return widget;
    }

    public async Task<bool> UpdateWidgetAsync(string viewId, DashboardWidget widget)
    {
        if (_views.TryGetValue(viewId, out var view))
        {
            var existing = view.Widgets.FirstOrDefault(w => w.Id == widget.Id);

            if (existing != null)
            {
                var index = view.Widgets.IndexOf(existing);
                view.Widgets[index] = widget;
                return true;
            }
        }

        return false;
    }

    public async Task<bool> RemoveWidgetAsync(string viewId, string widgetId)
    {
        if (_views.TryGetValue(viewId, out var view))
        {
            var widget = view.Widgets.FirstOrDefault(w => w.Id == widgetId);

            if (widget != null)
            {
                view.Widgets.Remove(widget);
                return true;
            }
        }

        return false;
    }

    public async Task<List<DashboardWidget>> GetWidgetsAsync(string viewId)
    {
        if (_views.TryGetValue(viewId, out var view))
        {
            return view.Widgets.OrderBy(w => w.Order).ToList();
        }

        return new();
    }

    public async Task<UserDashboardConfig> GetUserConfigAsync(string userId)
    {
        if (!_userConfigs.TryGetValue(userId, out var config))
        {
            config = new() { UserId = userId };
            _userConfigs[userId] = config;
        }

        return config;
    }

    public async Task<bool> UpdateUserConfigAsync(string userId, UserDashboardConfig config)
    {
        config.UserId = userId;
        config.LastModified = DateTime.UtcNow;
        _userConfigs[userId] = config;
        return true;
    }

    public async Task<bool> SetDefaultViewAsync(string userId, string viewId)
    {
        var config = await GetUserConfigAsync(userId);
        config.DefaultViewId = viewId;
        _userConfigs[userId] = config;
        return true;
    }

    public async Task<bool> SetThemeAsync(string userId, string theme)
    {
        var config = await GetUserConfigAsync(userId);
        config.Theme = theme;
        _userConfigs[userId] = config;
        return true;
    }

    public async Task<bool> AddFavoriteViewAsync(string userId, string viewId)
    {
        var config = await GetUserConfigAsync(userId);

        if (!config.FavoriteViews.Contains(viewId))
        {
            config.FavoriteViews.Add(viewId);
            _userConfigs[userId] = config;
        }

        return true;
    }

    public async Task<bool> RemoveFavoriteViewAsync(string userId, string viewId)
    {
        var config = await GetUserConfigAsync(userId);

        if (config.FavoriteViews.Remove(viewId))
        {
            _userConfigs[userId] = config;
            return true;
        }

        return false;
    }

    public async Task<SystemDashboardConfig> GetSystemConfigAsync()
    {
        return _systemConfig;
    }

    public async Task<bool> UpdateSystemConfigAsync(SystemDashboardConfig config)
    {
        _systemConfig = config;
        return true;
    }

    public async Task<ColorScheme> CreateColorSchemeAsync(ColorScheme scheme)
    {
        _colorSchemes[scheme.Name] = scheme;
        return scheme;
    }

    public async Task<ColorScheme?> GetColorSchemeAsync(string name)
    {
        _colorSchemes.TryGetValue(name, out var scheme);
        return scheme;
    }

    public async Task<List<ColorScheme>> GetAllColorSchemesAsync()
    {
        return _colorSchemes.Values.ToList();
    }

    public async Task<WidgetPreset> CreateWidgetPresetAsync(WidgetPreset preset)
    {
        if (string.IsNullOrEmpty(preset.Id))
        {
            preset.Id = Guid.NewGuid().ToString();
        }

        _widgetPresets[preset.Id] = preset;
        return preset;
    }

    public async Task<WidgetPreset?> GetWidgetPresetAsync(string presetId)
    {
        _widgetPresets.TryGetValue(presetId, out var preset);
        return preset;
    }

    public async Task<List<WidgetPreset>> GetAllWidgetPresetsAsync()
    {
        return _widgetPresets.Values.ToList();
    }

    public async Task<LayoutPreset> CreateLayoutPresetAsync(LayoutPreset preset)
    {
        if (string.IsNullOrEmpty(preset.Id))
        {
            preset.Id = Guid.NewGuid().ToString();
        }

        _layoutPresets[preset.Id] = preset;
        return preset;
    }

    public async Task<LayoutPreset?> GetLayoutPresetAsync(string presetId)
    {
        _layoutPresets.TryGetValue(presetId, out var preset);
        return preset;
    }

    public async Task<List<LayoutPreset>> GetAllLayoutPresetsAsync()
    {
        return _layoutPresets.Values.ToList();
    }
}
