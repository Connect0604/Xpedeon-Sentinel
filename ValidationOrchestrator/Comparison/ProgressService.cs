namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;
using Serilog;

/// <summary>
/// Real-time progress tracking and estimation service
/// Provides accurate progress updates and completion time estimates
/// </summary>
public class ProgressService : IProgressService
{
    private readonly ILogger _logger = Log.ForContext<ProgressService>();
    private readonly Dictionary<string, ProgressTracker> _trackers = new();
    private readonly Dictionary<string, List<ProgressHistoryEntry>> _history = new();
    private readonly Dictionary<string, List<ProgressUpdateEvent>> _updates = new();
    private readonly Dictionary<string, List<Func<ProgressUpdateEvent, Task>>> _subscribers = new();
    private readonly Dictionary<string, List<ProgressMilestone>> _milestones = new();
    private readonly Dictionary<string, Dictionary<string, ProgressEstimate>> _estimates = new();
    private readonly Dictionary<string, ProgressVelocity> _velocities = new();

    private ProgressTrackingConfig _config = new();

    public async Task<ProgressTracker> InitializeProgressAsync(string sessionId, List<string> phaseNames)
    {
        _logger.Information("Initializing progress tracking for session {Session} with {Count} phases", sessionId, phaseNames.Count);

        var tracker = new ProgressTracker
        {
            SessionId = sessionId,
            StartTime = DateTime.UtcNow,
            Status = "running"
        };

        var phaseNumber = 1;
        foreach (var phaseName in phaseNames)
        {
            tracker.PhaseProgresses.Add(new PhaseProgress
            {
                PhaseName = phaseName,
                PhaseNumber = phaseNumber,
                Status = "pending",
                Progress = 0,
                TotalSteps = 5
            });
            phaseNumber++;
        }

        _trackers[sessionId] = tracker;
        _history[sessionId] = new();
        _updates[sessionId] = new();
        _milestones[sessionId] = new();
        _estimates[sessionId] = new();

        return await Task.FromResult(tracker);
    }

    public async Task<ProgressTracker> GetProgressAsync(string sessionId)
    {
        if (_trackers.TryGetValue(sessionId, out var tracker))
        {
            tracker.OverallProgress = tracker.PhaseProgresses.Any()
                ? tracker.PhaseProgresses.Average(p => p.Progress)
                : 0;

            tracker.ElapsedMs = (long)(DateTime.UtcNow - tracker.StartTime).TotalMilliseconds;

            return await Task.FromResult(tracker);
        }

        return await Task.FromResult(new ProgressTracker { SessionId = sessionId });
    }

    public async Task<PhaseProgress> StartPhaseAsync(string sessionId, string phaseName, int phaseNumber, int estimatedSteps = 5)
    {
        _logger.Information("Starting phase {Phase} in session {Session}", phaseName, sessionId);

        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new PhaseProgress();

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            phase.Status = "running";
            phase.StartTime = DateTime.UtcNow;
            phase.TotalSteps = estimatedSteps;
            tracker.CurrentPhase = phaseName;
            tracker.CurrentPhaseNumber = phaseNumber;

            var update = new ProgressUpdateEvent
            {
                SessionId = sessionId,
                EventType = "PhaseStarted",
                PhaseName = phaseName,
                Timestamp = DateTime.UtcNow,
                Message = $"Phase {phaseName} started"
            };

            await RecordProgressEventAsync(update);
            await BroadcastUpdateAsync(update);

            return phase;
        }

        return new PhaseProgress();
    }

    public async Task UpdatePhaseProgressAsync(string sessionId, string phaseName, double progressPercentage)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return;

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            phase.Progress = Math.Min(progressPercentage, 100);

            var update = new ProgressUpdateEvent
            {
                SessionId = sessionId,
                EventType = "StepProgress",
                PhaseName = phaseName,
                ProgressPercentage = progressPercentage,
                Timestamp = DateTime.UtcNow,
                Message = $"{phaseName}: {progressPercentage:F1}%"
            };

            await RecordProgressEventAsync(update);
            await BroadcastUpdateAsync(update);
        }

        await Task.CompletedTask;
    }

    public async Task<PhaseProgress> CompletePhaseAsync(string sessionId, string phaseName)
    {
        _logger.Information("Completing phase {Phase} in session {Session}", phaseName, sessionId);

        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new PhaseProgress();

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            phase.Status = "completed";
            phase.Progress = 100;
            phase.EndTime = DateTime.UtcNow;
            phase.DurationMs = (long)(phase.EndTime.Value - phase.StartTime).TotalMilliseconds;

            // Record historical metrics for future estimation
            await RecordPhaseMetricsAsync(phaseName, phase.DurationMs);

            var update = new ProgressUpdateEvent
            {
                SessionId = sessionId,
                EventType = "PhaseCompleted",
                PhaseName = phaseName,
                ProgressPercentage = 100,
                ElapsedMs = phase.DurationMs,
                Timestamp = DateTime.UtcNow,
                Message = $"Phase {phaseName} completed in {phase.DurationMs}ms"
            };

            await RecordProgressEventAsync(update);
            await BroadcastUpdateAsync(update);

            return phase;
        }

        return new PhaseProgress();
    }

    public async Task<PhaseProgress> FailPhaseAsync(string sessionId, string phaseName, string errorMessage)
    {
        _logger.Error("Phase {Phase} failed in session {Session}: {Error}", phaseName, sessionId, errorMessage);

        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new PhaseProgress();

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            phase.Status = "failed";
            phase.EndTime = DateTime.UtcNow;
            phase.DurationMs = (long)(phase.EndTime.Value - phase.StartTime).TotalMilliseconds;
            tracker.Status = "failed";

            var update = new ProgressUpdateEvent
            {
                SessionId = sessionId,
                EventType = "PhaseFailed",
                PhaseName = phaseName,
                Timestamp = DateTime.UtcNow,
                Message = $"Phase {phaseName} failed: {errorMessage}"
            };

            await RecordProgressEventAsync(update);
            await BroadcastUpdateAsync(update);

            return phase;
        }

        return new PhaseProgress();
    }

    public async Task<ProgressStep> StartStepAsync(string sessionId, string phaseName, string stepName, int stepNumber)
    {
        _logger.Information("Starting step {Step} in phase {Phase}", stepName, phaseName);

        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new ProgressStep();

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            var step = new ProgressStep
            {
                StepName = stepName,
                StepNumber = stepNumber,
                Status = "running",
                StartTime = DateTime.UtcNow
            };

            phase.Steps.Add(step);

            var update = new ProgressUpdateEvent
            {
                SessionId = sessionId,
                EventType = "StepStarted",
                PhaseName = phaseName,
                StepName = stepName,
                Message = $"Step {stepName} started"
            };

            await RecordProgressEventAsync(update);
            await BroadcastUpdateAsync(update);

            return step;
        }

        return new ProgressStep();
    }

    public async Task UpdateStepProgressAsync(string sessionId, string phaseName, string stepName, double progressPercentage)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return;

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            var step = phase.Steps.FirstOrDefault(s => s.StepName == stepName);
            if (step != null)
            {
                step.Progress = Math.Min(progressPercentage, 100);

                var update = new ProgressUpdateEvent
                {
                    SessionId = sessionId,
                    EventType = "StepProgress",
                    PhaseName = phaseName,
                    StepName = stepName,
                    ProgressPercentage = progressPercentage,
                    Message = $"{stepName}: {progressPercentage:F1}%"
                };

                await RecordProgressEventAsync(update);
                await BroadcastUpdateAsync(update);
            }
        }

        await Task.CompletedTask;
    }

    public async Task<ProgressStep> CompleteStepAsync(string sessionId, string phaseName, string stepName)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new ProgressStep();

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            var step = phase.Steps.FirstOrDefault(s => s.StepName == stepName);
            if (step != null)
            {
                step.Status = "completed";
                step.Progress = 100;
                step.EndTime = DateTime.UtcNow;
                step.DurationMs = (long)(step.EndTime.Value - step.StartTime).TotalMilliseconds;
                phase.CompletedSteps++;

                // Update phase progress
                if (phase.TotalSteps > 0)
                {
                    phase.Progress = (double)phase.CompletedSteps / phase.TotalSteps * 100;
                }

                var update = new ProgressUpdateEvent
                {
                    SessionId = sessionId,
                    EventType = "StepCompleted",
                    PhaseName = phaseName,
                    StepName = stepName,
                    ElapsedMs = step.DurationMs,
                    Message = $"Step {stepName} completed in {step.DurationMs}ms"
                };

                await RecordProgressEventAsync(update);
                await BroadcastUpdateAsync(update);

                return step;
            }
        }

        return new ProgressStep();
    }

    public async Task AddStepArtifactAsync(string sessionId, string phaseName, string stepName, string artifactPath)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return;

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null)
        {
            var step = phase.Steps.FirstOrDefault(s => s.StepName == stepName);
            if (step != null)
            {
                step.Artifacts.Add(artifactPath);
                _logger.Information("Artifact added to step {Step}: {Path}", stepName, artifactPath);
            }
        }

        await Task.CompletedTask;
    }

    public async Task<ProgressUpdateEvent> RecordProgressEventAsync(ProgressUpdateEvent eventData)
    {
        if (!_updates.ContainsKey(eventData.SessionId))
            _updates[eventData.SessionId] = new();

        _updates[eventData.SessionId].Add(eventData);

        // Keep only recent updates
        if (_updates[eventData.SessionId].Count > 1000)
            _updates[eventData.SessionId].RemoveAt(0);

        return await Task.FromResult(eventData);
    }

    public async Task<List<ProgressHistoryEntry>> GetProgressHistoryAsync(string sessionId, int? maxEntries = null)
    {
        if (_history.TryGetValue(sessionId, out var history))
        {
            var result = maxEntries.HasValue ? history.TakeLast(maxEntries.Value).ToList() : history;
            return await Task.FromResult(result);
        }

        return new();
    }

    public async Task<List<ProgressUpdateEvent>> GetRecentUpdatesAsync(string sessionId, int maxEvents = 50)
    {
        if (_updates.TryGetValue(sessionId, out var updates))
        {
            var result = updates.TakeLast(maxEvents).ToList();
            return await Task.FromResult(result);
        }

        return new();
    }

    public async Task<ProgressEstimate> GetPhaseEstimateAsync(string phaseName)
    {
        // Look for any session with historical data for this phase
        foreach (var sessionEstimates in _estimates.Values)
        {
            if (sessionEstimates.TryGetValue(phaseName, out var estimate))
            {
                return await Task.FromResult(estimate);
            }
        }

        // Default estimate if no history
        return await Task.FromResult(new ProgressEstimate
        {
            PhaseName = phaseName,
            AverageDurationMs = 30000, // 30 seconds default
            Confidence = 0.5
        });
    }

    public async Task<ProgressEstimate> UpdatePhaseEstimateAsync(string sessionId, string phaseName)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new ProgressEstimate();

        var phase = tracker.PhaseProgresses.FirstOrDefault(p => p.PhaseName == phaseName);
        if (phase != null && phase.Status == "running")
        {
            var elapsed = (long)(DateTime.UtcNow - phase.StartTime).TotalMilliseconds;
            var percentComplete = phase.Progress;

            if (percentComplete > 0)
            {
                var estimatedTotal = (long)(elapsed / (percentComplete / 100));
                var estimated = estimatedTotal - elapsed;

                phase.EstimatedRemainingMs = estimated;

                if (!_estimates[sessionId].ContainsKey(phaseName))
                {
                    _estimates[sessionId][phaseName] = new ProgressEstimate
                    {
                        PhaseName = phaseName,
                        EstimatedCompletionTime = DateTime.UtcNow.AddMilliseconds(estimated)
                    };
                }

                var update = new ProgressUpdateEvent
                {
                    SessionId = sessionId,
                    EventType = "EstimateUpdated",
                    PhaseName = phaseName,
                    EstimatedRemainingMs = estimated,
                    Message = $"Estimated {estimated}ms remaining for {phaseName}"
                };

                await BroadcastUpdateAsync(update);
            }
        }

        return await GetPhaseEstimateAsync(phaseName);
    }

    public async Task<DateTime?> GetCompletionEstimateAsync(string sessionId)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return null;

        var totalEstimated = 0L;
        foreach (var phase in tracker.PhaseProgresses)
        {
            if (phase.Status == "completed")
            {
                totalEstimated += phase.DurationMs;
            }
            else if (phase.Status == "running")
            {
                totalEstimated += phase.EstimatedRemainingMs;
            }
            else
            {
                // Use average estimate
                totalEstimated += 30000; // Default 30s
            }
        }

        return await Task.FromResult(DateTime.UtcNow.AddMilliseconds(totalEstimated));
    }

    public async Task<long?> GetRemainingTimeEstimateAsync(string sessionId)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return null;

        var totalEstimated = 0L;
        foreach (var phase in tracker.PhaseProgresses.Where(p => p.Status != "completed"))
        {
            totalEstimated += phase.EstimatedRemainingMs > 0 ? phase.EstimatedRemainingMs : 30000;
        }

        return await Task.FromResult(totalEstimated);
    }

    public async Task<ProgressVelocity> CalculateVelocityAsync(string sessionId)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new ProgressVelocity { SessionId = sessionId };

        var elapsed = (DateTime.UtcNow - tracker.StartTime).TotalSeconds;
        var currentProgress = tracker.OverallProgress;

        var velocity = new ProgressVelocity
        {
            SessionId = sessionId,
            CurrentVelocityPercentPerSecond = elapsed > 0 ? currentProgress / elapsed : 0,
            LastUpdateTime = DateTime.UtcNow
        };

        if (_velocities.TryGetValue(sessionId, out var previous))
        {
            velocity.Trend = velocity.CurrentVelocityPercentPerSecond > previous.CurrentVelocityPercentPerSecond
                ? "accelerating"
                : velocity.CurrentVelocityPercentPerSecond < previous.CurrentVelocityPercentPerSecond
                ? "decelerating"
                : "stable";
        }

        _velocities[sessionId] = velocity;

        return await Task.FromResult(velocity);
    }

    public async Task<ProgressStatistics> GetStatisticsAsync(string sessionId)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker))
            return new ProgressStatistics { SessionId = sessionId };

        var stats = new ProgressStatistics
        {
            SessionId = sessionId,
            StartTime = tracker.StartTime,
            EndTime = tracker.Status == "completed" ? DateTime.UtcNow : null,
            TotalDurationMs = (long)(DateTime.UtcNow - tracker.StartTime).TotalMilliseconds,
            PhasesCompleted = tracker.PhaseProgresses.Count(p => p.Status == "completed"),
            TotalPhases = tracker.PhaseProgresses.Count,
            StepsCompleted = tracker.PhaseProgresses.Sum(p => p.CompletedSteps),
            TotalSteps = tracker.PhaseProgresses.Sum(p => p.TotalSteps)
        };

        if (stats.TotalPhases > 0)
        {
            var completedPhases = tracker.PhaseProgresses.Where(p => p.Status == "completed").ToList();
            stats.AveragePhaseTime = completedPhases.Any() ? completedPhases.Average(p => p.DurationMs) : 0;
        }

        return await Task.FromResult(stats);
    }

    public async Task<double> GetProgressComparisonAsync(string sessionId, string phaseName)
    {
        var current = await GetPhaseEstimateAsync(phaseName);
        var historical = await GetHistoricalMetricsAsync(phaseName);

        if (historical.AverageDurationMs > 0)
        {
            return await Task.FromResult((double)current.AverageDurationMs / historical.AverageDurationMs);
        }

        return await Task.FromResult(1.0);
    }

    public async Task<ProgressMilestone> AddMilestoneAsync(string sessionId, string name, double targetProgress, string priority = "normal")
    {
        var milestone = new ProgressMilestone
        {
            Name = name,
            TargetProgress = targetProgress,
            Priority = priority
        };

        if (!_milestones.ContainsKey(sessionId))
            _milestones[sessionId] = new();

        _milestones[sessionId].Add(milestone);

        return await Task.FromResult(milestone);
    }

    public async Task<List<ProgressMilestone>> GetMilestonesAsync(string sessionId)
    {
        if (_milestones.TryGetValue(sessionId, out var milestones))
            return await Task.FromResult(milestones);

        return new();
    }

    public async Task CheckMilestonesAsync(string sessionId)
    {
        if (!_trackers.TryGetValue(sessionId, out var tracker) || !_milestones.TryGetValue(sessionId, out var milestones))
            return;

        foreach (var milestone in milestones.Where(m => !m.IsCompleted))
        {
            if (tracker.OverallProgress >= milestone.TargetProgress)
            {
                milestone.CompletedAt = DateTime.UtcNow;

                var update = new ProgressUpdateEvent
                {
                    SessionId = sessionId,
                    EventType = "MilestoneReached",
                    ProgressPercentage = milestone.TargetProgress,
                    Message = $"Milestone reached: {milestone.Name}"
                };

                await BroadcastUpdateAsync(update);
            }
        }

        await Task.CompletedTask;
    }

    public void SubscribeToUpdates(string sessionId, Func<ProgressUpdateEvent, Task> callback)
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

    public async IAsyncEnumerable<ProgressUpdateEvent> GetUpdatesStreamAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (_updates.TryGetValue(sessionId, out var updates))
        {
            foreach (var update in updates)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return update;
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    public async Task BroadcastUpdateAsync(ProgressUpdateEvent update)
    {
        if (_subscribers.TryGetValue(update.SessionId, out var callbacks))
        {
            var tasks = callbacks.Select(cb => cb(update));
            await Task.WhenAll(tasks);
        }
    }

    public void Configure(ProgressTrackingConfig config)
    {
        _config = config;
        _logger.Information("Progress tracking configured");
    }

    public ProgressTrackingConfig GetConfiguration()
    {
        return _config;
    }

    public async Task RecordPhaseMetricsAsync(string phaseName, long durationMs)
    {
        _logger.Information("Recording phase metrics for {Phase}: {Duration}ms", phaseName, durationMs);
        await Task.CompletedTask;
    }

    public async Task<ProgressEstimate> GetHistoricalMetricsAsync(string phaseName)
    {
        return await Task.FromResult(new ProgressEstimate
        {
            PhaseName = phaseName,
            AverageDurationMs = 30000,
            SampleCount = 0,
            Confidence = 0.5
        });
    }

    public async Task PurgeOldHistoryAsync(int retentionDays)
    {
        _logger.Information("Purging history older than {Days} days", retentionDays);
        await Task.CompletedTask;
    }
}
