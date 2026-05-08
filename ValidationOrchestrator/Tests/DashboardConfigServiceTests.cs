namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for dashboard configuration service
/// </summary>
public class DashboardConfigServiceTests
{
    private readonly IDashboardConfigService _service = new DashboardConfigService();

    [Fact]
    public async Task CreateViewAsync_ShouldCreateView()
    {
        // Act
        var view = await _service.CreateViewAsync(new DashboardView { Name = "Executive" });

        // Assert
        view.Should().NotBeNull();
        view.Name.Should().Be("Executive");
    }

    [Fact]
    public async Task GetViewAsync_ShouldReturnView()
    {
        // Arrange
        var created = await _service.CreateViewAsync(new DashboardView { Name = "Test" });

        // Act
        var retrieved = await _service.GetViewAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetAllViewsAsync_ShouldReturnAllViews()
    {
        // Arrange
        await _service.CreateViewAsync(new DashboardView { Name = "View1" });
        await _service.CreateViewAsync(new DashboardView { Name = "View2" });

        // Act
        var views = await _service.GetAllViewsAsync();

        // Assert
        views.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetDefaultViewAsync_ShouldReturnDefaultView()
    {
        // Arrange
        await _service.CreateViewAsync(new DashboardView { Name = "Default", IsDefault = true });

        // Act
        var view = await _service.GetDefaultViewAsync();

        // Assert
        view.Should().NotBeNull();
        view.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateViewAsync_ShouldUpdateView()
    {
        // Arrange
        var view = await _service.CreateViewAsync(new DashboardView { Name = "Original" });
        view.Name = "Updated";

        // Act
        var result = await _service.UpdateViewAsync(view);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteViewAsync_ShouldDeleteView()
    {
        // Arrange
        var view = await _service.CreateViewAsync(new DashboardView { Name = "ToDelete" });

        // Act
        var result = await _service.DeleteViewAsync(view.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddWidgetAsync_ShouldAddWidget()
    {
        // Arrange
        var view = await _service.CreateViewAsync(new DashboardView { Name = "Test" });
        var widget = new DashboardWidget { Type = "progress", Title = "Progress" };

        // Act
        var added = await _service.AddWidgetAsync(view.Id, widget);

        // Assert
        added.Should().NotBeNull();
        added.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWidgetsAsync_ShouldReturnWidgets()
    {
        // Arrange
        var view = await _service.CreateViewAsync(new DashboardView { Name = "Test" });
        await _service.AddWidgetAsync(view.Id, new DashboardWidget { Type = "progress" });
        await _service.AddWidgetAsync(view.Id, new DashboardWidget { Type = "metrics" });

        // Act
        var widgets = await _service.GetWidgetsAsync(view.Id);

        // Assert
        widgets.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveWidgetAsync_ShouldRemoveWidget()
    {
        // Arrange
        var view = await _service.CreateViewAsync(new DashboardView { Name = "Test" });
        var widget = await _service.AddWidgetAsync(view.Id, new DashboardWidget { Type = "progress" });

        // Act
        var result = await _service.RemoveWidgetAsync(view.Id, widget.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserConfigAsync_ShouldReturnConfig()
    {
        // Act
        var config = await _service.GetUserConfigAsync("user-1");

        // Assert
        config.Should().NotBeNull();
        config.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task UpdateUserConfigAsync_ShouldUpdateConfig()
    {
        // Arrange
        var config = new UserDashboardConfig { UserId = "user-1", Theme = "dark" };

        // Act
        var result = await _service.UpdateUserConfigAsync("user-1", config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetThemeAsync_ShouldSetTheme()
    {
        // Act
        var result = await _service.SetThemeAsync("user-1", "dark");
        var config = await _service.GetUserConfigAsync("user-1");

        // Assert
        result.Should().BeTrue();
        config.Theme.Should().Be("dark");
    }

    [Fact]
    public async Task AddFavoriteViewAsync_ShouldAddFavorite()
    {
        // Arrange
        var view = await _service.CreateViewAsync(new DashboardView { Name = "Favorite" });

        // Act
        var result = await _service.AddFavoriteViewAsync("user-1", view.Id);
        var config = await _service.GetUserConfigAsync("user-1");

        // Assert
        result.Should().BeTrue();
        config.FavoriteViews.Should().Contain(view.Id);
    }

    [Fact]
    public async Task RemoveFavoriteViewAsync_ShouldRemoveFavorite()
    {
        // Arrange
        var view = await _service.CreateViewAsync(new DashboardView { Name = "Favorite" });
        await _service.AddFavoriteViewAsync("user-1", view.Id);

        // Act
        var result = await _service.RemoveFavoriteViewAsync("user-1", view.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetSystemConfigAsync_ShouldReturnConfig()
    {
        // Act
        var config = await _service.GetSystemConfigAsync();

        // Assert
        config.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSystemConfigAsync_ShouldUpdateConfig()
    {
        // Arrange
        var config = new SystemDashboardConfig { MaxConcurrentDashboards = 200 };

        // Act
        await _service.UpdateSystemConfigAsync(config);
        var retrieved = await _service.GetSystemConfigAsync();

        // Assert
        retrieved.MaxConcurrentDashboards.Should().Be(200);
    }

    [Fact]
    public async Task CreateColorSchemeAsync_ShouldCreateScheme()
    {
        // Arrange
        var scheme = new ColorScheme { Name = "Vibrant" };

        // Act
        var created = await _service.CreateColorSchemeAsync(scheme);

        // Assert
        created.Should().NotBeNull();
        created.Name.Should().Be("Vibrant");
    }

    [Fact]
    public async Task GetColorSchemeAsync_ShouldReturnScheme()
    {
        // Arrange
        var scheme = new ColorScheme { Name = "Blue" };
        await _service.CreateColorSchemeAsync(scheme);

        // Act
        var retrieved = await _service.GetColorSchemeAsync("Blue");

        // Assert
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateWidgetPresetAsync_ShouldCreatePreset()
    {
        // Arrange
        var preset = new WidgetPreset { Name = "ProgressBar" };

        // Act
        var created = await _service.CreateWidgetPresetAsync(preset);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWidgetPresetAsync_ShouldReturnPreset()
    {
        // Arrange
        var preset = new WidgetPreset { Name = "Test" };
        var created = await _service.CreateWidgetPresetAsync(preset);

        // Act
        var retrieved = await _service.GetWidgetPresetAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateLayoutPresetAsync_ShouldCreateLayout()
    {
        // Arrange
        var layout = new LayoutPreset { Name = "Executive" };

        // Act
        var created = await _service.CreateLayoutPresetAsync(layout);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLayoutPresetAsync_ShouldReturnLayout()
    {
        // Arrange
        var layout = new LayoutPreset { Name = "Test" };
        var created = await _service.CreateLayoutPresetAsync(layout);

        // Act
        var retrieved = await _service.GetLayoutPresetAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
    }
}
