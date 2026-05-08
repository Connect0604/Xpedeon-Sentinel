namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;
using Serilog;

/// <summary>
/// Real-time dashboard state management and update service
/// Provides API for frontend dashboard consumption
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ILogger _logger = Log.ForContext<DashboardService>();
    private readonly Dictionary<string, DashboardState> _sessions = new();
    private readonly Dictionary<string, List<DashboardAlert>> _alerts = new();
    private readonly Dictionary<string, List<ProgressItem>> _progress = new();
    private readonly Dictionary<string, List<DashboardUpdate>> _updateHistory = new();
    private readonly Dictionary<string, List<Func<DashboardUpdate, Task>>> _subscribers = new();
    private readonly Dictionary<string, DashboardUserSession> _userSessions = new();
    private readonly Dictionary<string, DashboardNotification> _notifications = new();

    private DashboardConfig _config = new();
    private LiveUpdateConfig _liveUpdateConfig = new();

    private DateTime _startTime = DateTime.UtcNow;
    private int _totalUpdates = 0;
    private int _totalAlerts = 0;
    private long _totalUpdateLatency = 0;

    public async Task<DashboardState> GetDashboardStateAsync(string sessionId)
    {
        _logger.Information("Getting dashboard state for session {Session}", sessionId);

        if (_sessions.TryGetValue(sessionId, out var state))
        {
            return await Task.FromResult(state);
        }

        _logger.Warning("Dashboard state not found for session {Session}", sessionId);
        return new DashboardState { SessionId = sessionId };
    }

    public async Task<DashboardState> InitializeDashboardAsync(string sessionId, string clientId)
    {
        _logger.Information("Initializing dashboard for session {Session}, client {Client}", sessionId, clientId);

        var state = new DashboardState
        {
            SessionId = sessionId,
            ClientId = clientId,
            PipelineStatus = new ValidationPipelineStatus
            {
                SessionId = sessionId,
                StartTime = DateTime.UtcNow,
                TotalPhases = 6,
                IsRunning = true,
                Phases = new List<PhaseStatus>
                {
                    CreatePhaseStatus("Discovery", 1),
                    CreatePhaseStatus("Test Generation", 2),
                    CreatePhaseStatus("Execution", 3),
                    CreatePhaseStatus("Comparison", 4),
                    CreatePhaseStatus("Expert Review", 5),
                    CreatePhaseStatus("Reporting", 6)
                }
            },
            Metrics = new DashboardMetrics(),
            ExecutiveSnapshot = new ExecutiveSnapshot()
        };

        _sessions[sessionId] = state;
        _alerts[sessionId] = new();
        _progress[sessionId] = new();
        _updateHistory[sessionId] = new();

        _logger.Information("Dashboard initialized for session {Session}", sessionId);

        return await Task.FromResult(state);
    }

    public async Task<DashboardState> RefreshDashboardAsync(string sessionId)
    {
        _logger.Information("Refreshing dashboard for session {Session}", sessionId);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return new DashboardState { SessionId = sessionId };

        state.LastUpdated = DateTime.UtcNow;

        // Calculate overall progress
        if (state.PipelineStatus.Phases.Any())
        {
            state.OverallProgress = state.PipelineStatus.Phases.Average(p => p.Progress);
        }

        return await Task.FromResult(state);
    }

    public async Task UpdatePhaseStatusAsync(string sessionId, string phaseName, string status, double progress)
    {
        _logger.Information("Updating phase {Phase} status to {Status}, progress {Progress}%", phaseName, status, progress);

        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            _logger.Warning("Session not found: {Session}", sessionId);
            return;
        }

        var phase = state.PipelineStatus.Phases.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            phase.Status = status;
            phase.Progress = progress;
            state.PipelineStatus.CurrentPhase = status == "InProgress" ? phaseName : state.PipelineStatus.CurrentPhase;

            var update = new DashboardUpdate
            {
                SessionId = sessionId,
                EventType = "PhaseUpdated",
                Payload = phase,
                Message = $"{phaseName} updated: {status} ({progress:F0}%)"
            };

            await BroadcastUpdateAsync(update);
        }

        await Task.CompletedTask;
    }

    public async Task<PhaseStatus> CompletePhaseAsync(string sessionId, string phaseName, long durationMs)
    {
        _logger.Information("Completing phase {Phase} with duration {Duration}ms", phaseName, durationMs);

        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            return new PhaseStatus();
        }

        var phase = state.PipelineStatus.Phases.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            phase.Status = "Completed";
            phase.Progress = 100;
            phase.EndTime = DateTime.UtcNow;
            phase.DurationMs = durationMs;

            var update = new DashboardUpdate
            {
                SessionId = sessionId,
                EventType = "PhaseCompleted",
                Payload = phase,
                Message = $"{phaseName} completed in {durationMs}ms"
            };

            await BroadcastUpdateAsync(update);
            return phase;
        }

        return new PhaseStatus();
    }

    public async Task RecordPhaseErrorAsync(string sessionId, string phaseName, string errorMessage)
    {
        _logger.Error("Phase {Phase} error: {Error}", phaseName, errorMessage);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return;

        var phase = state.PipelineStatus.Phases.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            phase.Status = "Failed";
            phase.ErrorMessage = errorMessage;
            state.PipelineStatus.HasErrors = true;
            state.PipelineStatus.ErrorMessages.Add(errorMessage);
        }

        var alert = await AddAlertAsync(sessionId, "error", $"Phase Error: {phaseName}", errorMessage);

        await Task.CompletedTask;
    }

    public async Task UpdateMetricsAsync(
        string sessionId,
        DiscrepancyAnalysisResult? discrepancies = null,
        MigrationRiskAssessment? riskAssessment = null)
    {
        _logger.Information("Updating metrics for session {Session}", sessionId);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return;

        // Update risk and health scores
        if (riskAssessment != null)
        {
            state.Metrics.RiskScore = new MetricCard
            {
                Title = "Risk Score",
                Value = $"{riskAssessment.OverallRiskScore:F1}",
                Unit = "/100",
                Status = riskAssessment.OverallRiskScore > 50 ? "critical" :
                        riskAssessment.OverallRiskScore > 25 ? "warning" : "normal"
            };

            state.Metrics.HealthScore = new MetricCard
            {
                Title = "Health Score",
                Value = $"{riskAssessment.OverallHealthScore:F1}",
                Unit = "/100",
                Status = riskAssessment.OverallHealthScore > 75 ? "success" :
                        riskAssessment.OverallHealthScore > 50 ? "normal" : "warning"
            };

            state.Metrics.BlockingIssues = new MetricCard
            {
                Title = "Blocking Issues",
                Value = riskAssessment.GoBlockers.Count.ToString(),
                Status = riskAssessment.GoBlockers.Count > 0 ? "critical" : "success"
            };
        }

        // Update discrepancy metrics
        if (discrepancies != null)
        {
            state.Metrics.DiscrepancyCount = new MetricCard
            {
                Title = "Total Discrepancies",
                Value = discrepancies.Discrepancies.Count.ToString(),
                Status = discrepancies.Discrepancies.Count > 10 ? "critical" :
                        discrepancies.Discrepancies.Count > 5 ? "warning" : "normal"
            };

            // Build severity distribution chart
            state.Metrics.SeverityDistribution = new ChartData
            {
                Title = "Severity Distribution",
                ChartType = "pie",
                Labels = new List<string> { "Critical", "High", "Medium", "Low" },
                Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Discrepancies",
                        Data = new List<double>
                        {
                            discrepancies.Summary.CriticalCount,
                            discrepancies.Summary.HighCount,
                            discrepancies.Summary.MediumCount,
                            discrepancies.Summary.LowCount
                        },
                        BackgroundColor = new List<string> { "#ff4444", "#ff8800", "#ffbb00", "#00cc00" }
                    }
                }
            };
        }

        state.LastUpdated = DateTime.UtcNow;
        _totalUpdates++;

        await Task.CompletedTask;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var state))
            return await Task.FromResult(state.Metrics);

        return await Task.FromResult(new DashboardMetrics());
    }

    public async Task<MetricCard> UpdateMetricAsync(string sessionId, string metricName, object value)
    {
        var metric = new MetricCard { Title = metricName, Value = value.ToString() ?? string.Empty };
        return await Task.FromResult(metric);
    }

    public async Task<DashboardAlert> AddAlertAsync(string sessionId, string type, string title, string message)
    {
        _logger.Information("Adding alert for session {Session}: {Title}", sessionId, title);

        var alert = new DashboardAlert
        {
            Type = type,
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        if (!_alerts.ContainsKey(sessionId))
            _alerts[sessionId] = new();

        _alerts[sessionId].Add(alert);
        _totalAlerts++;

        var update = new DashboardUpdate
        {
            SessionId = sessionId,
            EventType = "AlertAdded",
            Payload = alert
        };

        await BroadcastUpdateAsync(update);

        return alert;
    }

    public async Task<List<DashboardAlert>> GetAlertsAsync(string sessionId, bool includeAcknowledged = false)
    {
        if (_alerts.TryGetValue(sessionId, out var alerts))
        {
            var result = includeAcknowledged ? alerts : alerts.Where(a => !a.Acknowledged).ToList();
            return await Task.FromResult(result);
        }

        return new();
    }

    public async Task AcknowledgeAlertAsync(string alertId)
    {
        foreach (var alerts in _alerts.Values)
        {
            var alert = alerts.FirstOrDefault(a => a.Id == alertId);
            if (alert != null)
            {
                alert.Acknowledged = true;
                _logger.Information("Alert acknowledged: {Alert}", alertId);
            }
        }

        await Task.CompletedTask;
    }

    public async Task ClearAlertsAsync(string sessionId)
    {
        if (_alerts.ContainsKey(sessionId))
        {
            _alerts[sessionId].Clear();
            _logger.Information("Alerts cleared for session {Session}", sessionId);
        }

        await Task.CompletedTask;
    }

    public async Task<ProgressItem> AddProgressItemAsync(string sessionId, string title, string status)
    {
        var item = new ProgressItem
        {
            Title = title,
            Status = status,
            Timestamp = DateTime.UtcNow
        };

        if (!_progress.ContainsKey(sessionId))
            _progress[sessionId] = new();

        _progress[sessionId].Add(item);

        return await Task.FromResult(item);
    }

    public async Task<List<ProgressItem>> GetProgressAsync(string sessionId)
    {
        if (_progress.TryGetValue(sessionId, out var items))
            return await Task.FromResult(items.OrderByDescending(p => p.Timestamp).ToList());

        return new();
    }

    public async Task UpdateProgressItemAsync(string progressId, string status, long durationMs)
    {
        foreach (var items in _progress.Values)
        {
            var item = items.FirstOrDefault(p => p.Id == progressId);
            if (item != null)
            {
                item.Status = status;
                item.DurationMs = durationMs;
            }
        }

        await Task.CompletedTask;
    }

    public async Task<ExecutiveSnapshot> GetExecutiveSnapshotAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var state))
            return await Task.FromResult(state.ExecutiveSnapshot);

        return await Task.FromResult(new ExecutiveSnapshot());
    }

    public async Task UpdateExecutiveSnapshotAsync(string sessionId, MigrationRiskAssessment riskAssessment)
    {
        if (!_sessions.TryGetValue(sessionId, out var state))
            return;

        state.ExecutiveSnapshot = new ExecutiveSnapshot
        {
            Decision = riskAssessment.Recommendation.Decision.ToString(),
            OverallRiskScore = riskAssessment.OverallRiskScore,
            OverallHealthScore = riskAssessment.OverallHealthScore,
            CriticalBlockers = riskAssessment.GoBlockers.Count,
            ConfidenceLevel = riskAssessment.Recommendation.ConfidenceLevel,
            ProjectedReadyDate = DateTime.UtcNow.AddDays(riskAssessment.Recommendation.MinimumDaysToReady)
        };

        await Task.CompletedTask;
    }

    public async Task<ModuleDetails> GetModuleDetailsAsync(string sessionId, string moduleName)
    {
        var details = new ModuleDetails { ModuleName = moduleName };
        return await Task.FromResult(details);
    }

    public async Task<List<ModuleDetails>> GetModulesOverviewAsync(string sessionId)
    {
        return new();
    }

    public async IAsyncEnumerable<DashboardUpdate> GetUpdatesStreamAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (_updateHistory.TryGetValue(sessionId, out var history))
        {
            foreach (var update in history)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return update;
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    public void SubscribeToUpdates(string sessionId, Func<DashboardUpdate, Task> callback)
    {
        if (!_subscribers.ContainsKey(sessionId))
            _subscribers[sessionId] = new();

        _subscribers[sessionId].Add(callback);
        _logger.Information("Subscriber added for session {Session}", sessionId);
    }

    public void UnsubscribeFromUpdates(string sessionId)
    {
        if (_subscribers.ContainsKey(sessionId))
        {
            _subscribers.Remove(sessionId);
            _logger.Information("Subscribers removed for session {Session}", sessionId);
        }
    }

    public async Task BroadcastUpdateAsync(DashboardUpdate update)
    {
        var startTime = DateTime.UtcNow;

        if (_updateHistory.TryGetValue(update.SessionId, out var history))
        {
            history.Add(update);
            if (history.Count > _liveUpdateConfig.MetricsHistoryPoints)
                history.RemoveAt(0);
        }

        if (_subscribers.TryGetValue(update.SessionId, out var callbacks))
        {
            var tasks = callbacks.Select(cb => cb(update));
            await Task.WhenAll(tasks);
        }

        var latency = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        _totalUpdateLatency += latency;
    }

    public async Task<DashboardNotification> SendNotificationAsync(
        string userId,
        string type,
        string title,
        string message,
        int priority = 1)
    {
        var notification = new DashboardNotification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Priority = priority,
            CreatedAt = DateTime.UtcNow
        };

        _notifications[notification.Id] = notification;
        _logger.Information("Notification sent to user {User}: {Title}", userId, title);

        return await Task.FromResult(notification);
    }

    public async Task<List<DashboardNotification>> GetNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var notifications = _notifications.Values
            .Where(n => n.UserId == userId && (!unreadOnly || !n.Read))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        return await Task.FromResult(notifications);
    }

    public async Task MarkNotificationAsReadAsync(string notificationId)
    {
        if (_notifications.TryGetValue(notificationId, out var notification))
        {
            notification.Read = true;
        }

        await Task.CompletedTask;
    }

    public async Task<DashboardState> ApplyFilterAsync(string sessionId, DashboardFilter filter)
    {
        if (_sessions.TryGetValue(sessionId, out var state))
        {
            return await Task.FromResult(state);
        }

        return await Task.FromResult(new DashboardState());
    }

    public async Task<DashboardState> ClearFiltersAsync(string sessionId)
    {
        return await GetDashboardStateAsync(sessionId);
    }

    public async Task<TrendLine> GetTrendAsync(string sessionId, string metricName, int hoursBack = 24)
    {
        var trend = new TrendLine { Title = metricName };
        return await Task.FromResult(trend);
    }

    public async Task<List<DashboardMetrics>> GetHistoricalMetricsAsync(
        string sessionId,
        DateTime startTime,
        DateTime endTime)
    {
        return new();
    }

    public async Task<DashboardExport> ExportDashboardAsync(
        string sessionId,
        string format = "json",
        List<string>? sections = null)
    {
        var export = new DashboardExport
        {
            SessionId = sessionId,
            Format = format,
            ExportedAt = DateTime.UtcNow,
            IncludedSections = sections ?? new()
        };

        return await Task.FromResult(export);
    }

    public async Task<DashboardExport?> GetExportStatusAsync(string exportId)
    {
        return await Task.FromResult<DashboardExport?>(null);
    }

    public async Task<DashboardUserSession> CreateUserSessionAsync(string userId, string sessionId)
    {
        var session = new DashboardUserSession
        {
            UserId = userId,
            SessionId = sessionId,
            ConnectedAt = DateTime.UtcNow,
            IsActive = true
        };

        _userSessions[userId] = session;
        _logger.Information("User session created: {User} -> {Session}", userId, sessionId);

        return await Task.FromResult(session);
    }

    public async Task UpdateUserSessionAsync(string userId, string sessionId, string currentView)
    {
        if (_userSessions.TryGetValue(userId, out var session))
        {
            session.CurrentView = currentView;
            session.ViewHistory.Add(currentView);
        }

        await Task.CompletedTask;
    }

    public async Task EndUserSessionAsync(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var session))
        {
            session.IsActive = false;
            session.DisconnectedAt = DateTime.UtcNow;
            _logger.Information("User session ended: {User}", userId);
        }

        await Task.CompletedTask;
    }

    public DashboardConfig GetConfiguration()
    {
        return _config;
    }

    public void Configure(DashboardConfig config)
    {
        _config = config;
        _logger.Information("Dashboard configured");
    }

    public LiveUpdateConfig GetLiveUpdateConfig()
    {
        return _liveUpdateConfig;
    }

    public void ConfigureLiveUpdates(LiveUpdateConfig config)
    {
        _liveUpdateConfig = config;
        _logger.Information("Live updates configured");
    }

    public async Task<DashboardServiceHealth> GetHealthAsync()
    {
        var health = new DashboardServiceHealth
        {
            Status = "healthy",
            UptimeMs = (long)(DateTime.UtcNow - _startTime).TotalMilliseconds,
            ActiveConnections = _userSessions.Count(s => s.Value.IsActive),
            AverageResponseTimeMs = _totalUpdates > 0 ? _totalUpdateLatency / _totalUpdates : 0,
            ErrorCount = 0,
            LastHealthCheck = DateTime.UtcNow
        };

        return await Task.FromResult(health);
    }

    public async Task<int> GetActiveSessionsCountAsync()
    {
        return await Task.FromResult(_sessions.Count(s => s.Value.PipelineStatus.IsRunning));
    }

    public async Task<DashboardServiceMetrics> GetServiceMetricsAsync()
    {
        var uptime = DateTime.UtcNow - _startTime;
        var metrics = new DashboardServiceMetrics
        {
            TotalUpdatesProcessed = _totalUpdates,
            TotalAlertsGenerated = _totalAlerts,
            ActiveDashboards = _sessions.Count,
            AverageUpdateLatencyMs = _totalUpdates > 0 ? _totalUpdateLatency / _totalUpdates : 0,
            UpdatesPerSecond = uptime.TotalSeconds > 0 ? _totalUpdates / uptime.TotalSeconds : 0,
            PeakConcurrentConnections = _userSessions.Count
        };

        return await Task.FromResult(metrics);
    }

    private PhaseStatus CreatePhaseStatus(string phaseName, int phaseNumber)
    {
        return new PhaseStatus
        {
            PhaseName = phaseName,
            PhaseNumber = phaseNumber,
            Status = "Pending",
            Progress = 0,
            StartTime = DateTime.UtcNow,
            TotalSteps = 5
        };
    }
}
