# Form 10: Real-Time Progress Tracking Service

## Overview

The **Progress Service** (`IProgressService`/`ProgressService`) provides real-time progress tracking, velocity analysis, and completion time estimation for the 6-phase validation pipeline. It enables accurate monitoring of each phase and step, calculates progress velocity, estimates time to completion, and supports milestone tracking with real-time event streaming.

**Key Features:**
- Phase-by-phase progress tracking with status transitions
- Step-level granularity within phases
- Real-time velocity calculation and trend analysis
- Historical estimation based on current progress rates
- Milestone tracking with automatic completion detection
- Event-driven architecture with subscriber callbacks and async streaming
- Artifact collection per step (output references)
- Progress history with configurable retention
- Statistics aggregation (completion rates, averages, projections)

**Responsibilities:**
- Initialize and maintain per-session progress state
- Track phase and step status transitions
- Record and broadcast progress events
- Calculate velocity, estimates, and completion times
- Manage milestones and detect when reached
- Store historical data for comparison and analysis
- Stream real-time updates to multiple subscribers

## Architecture

### Data Models

**ProgressTracker** - Main state object per session
- `SessionId`: Unique session identifier
- `CurrentPhase`: Name of currently executing phase
- `CurrentPhaseNumber`: Ordinal position (1-6)
- `StartTime`: When tracking began (UTC)
- `EstimatedCompletionTime`: Calculated completion time
- `PhaseProgresses`: List<PhaseProgress> for all phases
- `OverallProgress`: 0-100 aggregate percentage
- `ElapsedMs`: Time elapsed since start
- `EstimatedRemainingMs`: Projected remaining duration
- `Status`: "running" | "paused" | "completed" | "failed"

**PhaseProgress** - Per-phase tracking
- `PhaseName`: "Discovery" | "TestGen" | "Execution" | "Comparison" | "Review" | "Reporting"
- `PhaseNumber`: 1-6
- `Status`: "pending" | "running" | "completed" | "failed" | "skipped"
- `Progress`: 0-100 percentage
- `StartTime` / `EndTime`: Timestamps
- `DurationMs`: Total phase duration
- `EstimatedRemainingMs`: Time until completion
- `CompletedSteps` / `TotalSteps`: Step counters
- `Steps`: List<ProgressStep> with detailed tracking
- `StepProgressPercentage`: Calculated from step counters

**ProgressStep** - Individual step within phase
- `StepName`: Human-readable step identifier
- `StepNumber`: Ordinal position within phase
- `Status`: "pending" | "running" | "completed" | "failed" | "skipped"
- `Progress`: 0-100 for long-running steps
- `StartTime` / `EndTime`: Step duration
- `DurationMs`: Elapsed time
- `Details`: Optional metadata or error messages
- `Artifacts`: List<string> of generated outputs/references

**ProgressUpdateEvent** - Real-time event broadcast
- `SessionId`: Originating session
- `EventType`: "PhaseStarted" | "StepStarted" | "StepProgress" | "StepCompleted" | "PhaseCompleted" | "EstimateUpdated" | "MilestoneReached"
- `Timestamp`: When event occurred
- `PhaseName` / `StepName`: Optional context
- `ProgressPercentage`: Current progress
- `ElapsedMs` / `EstimatedRemainingMs`: Time measurements
- `Message`: Optional event description
- `Data`: Arbitrary payload

**ProgressEstimate** - Historical estimation
- `PhaseName`: Phase being estimated
- `AverageDurationMs`: Mean historical duration
- `MinDurationMs` / `MaxDurationMs`: Range
- `SampleCount`: Number of historical samples
- `Confidence`: 0-1 rating based on sample size
- `EstimatedCompletionTime`: Projected completion

**ProgressMilestone** - Progress checkpoint
- `Id`: Unique milestone identifier
- `Name`: "Halfway" | "Schema Analysis Complete" | etc.
- `Description`: Optional details
- `TargetProgress`: 0-100 when milestone triggers
- `CompletedAt`: When reached (nullable)
- `IsCompleted`: Calculated from CompletedAt
- `Priority`: "critical" | "high" | "normal" | "low"

**ProgressStatistics** - Aggregated analysis
- `SessionId`: Associated session
- `StartTime` / `EndTime`: Tracking duration
- `TotalDurationMs`: Total elapsed time
- `PhasesCompleted` / `TotalPhases`: Completion status
- `AveragePhaseTime` / `AverageStepTime`: Durations
- `StepsCompleted` / `TotalSteps`: Counters
- `StepsFailed` / `StepsSkipped`: Status breakdown
- `AverageProgressVelocity`: Percent per second
- `EstimatedRemainingTime`: Projected remaining ms
- `ProjectedCompletionTime`: Calculated completion datetime

**ProgressVelocity** - Speed analysis
- `SessionId`: Associated session
- `CurrentVelocityPercentPerSecond`: Recent rate
- `AverageVelocityPercentPerSecond`: Historical mean
- `MaxVelocityPercentPerSecond` / `MinVelocityPercentPerSecond`: Range
- `LastUpdateTime`: When calculated
- `Trend`: "accelerating" | "stable" | "decelerating"

**ProgressTrackingConfig** - Service configuration
- `EnableAutomaticEstimates`: bool (default: true)
- `HistoricalSampleSize`: int (default: 10)
- `UpdateIntervalMs`: int (default: 500)
- `EnableMilestones`: bool (default: true)
- `EnableDetailedSteps`: bool (default: true)
- `KeepHistory`: bool (default: true)
- `HistoryRetentionDays`: int (default: 30)

### Interface: IProgressService

#### Initialization
```csharp
Task<ProgressTracker> InitializeProgressAsync(string sessionId, List<string> phaseNames);
Task<ProgressTracker> GetProgressAsync(string sessionId);
```

#### Phase Tracking
```csharp
Task<PhaseProgress> StartPhaseAsync(string sessionId, string phaseName, int phaseNumber, int estimatedSteps = 5);
Task UpdatePhaseProgressAsync(string sessionId, string phaseName, double progressPercentage);
Task<PhaseProgress> CompletePhaseAsync(string sessionId, string phaseName);
Task<PhaseProgress> FailPhaseAsync(string sessionId, string phaseName, string errorMessage);
```

#### Step Tracking
```csharp
Task<ProgressStep> StartStepAsync(string sessionId, string phaseName, string stepName, int stepNumber);
Task UpdateStepProgressAsync(string sessionId, string phaseName, string stepName, double progressPercentage);
Task<ProgressStep> CompleteStepAsync(string sessionId, string phaseName, string stepName);
Task AddStepArtifactAsync(string sessionId, string phaseName, string stepName, string artifactPath);
```

#### Progress Events
```csharp
Task<ProgressUpdateEvent> RecordProgressEventAsync(ProgressUpdateEvent eventData);
Task<List<ProgressHistoryEntry>> GetProgressHistoryAsync(string sessionId, int? maxEntries = null);
Task<List<ProgressUpdateEvent>> GetRecentUpdatesAsync(string sessionId, int maxEvents = 50);
```

#### Estimation
```csharp
Task<ProgressEstimate> GetPhaseEstimateAsync(string phaseName);
Task<ProgressEstimate> UpdatePhaseEstimateAsync(string sessionId, string phaseName);
Task<DateTime?> GetCompletionEstimateAsync(string sessionId);
Task<long?> GetRemainingTimeEstimateAsync(string sessionId);
```

#### Velocity & Analytics
```csharp
Task<ProgressVelocity> CalculateVelocityAsync(string sessionId);
Task<ProgressStatistics> GetStatisticsAsync(string sessionId);
Task<double> GetProgressComparisonAsync(string sessionId, string phaseName);
```

#### Milestones
```csharp
Task<ProgressMilestone> AddMilestoneAsync(string sessionId, string name, double targetProgress, string priority = "normal");
Task<List<ProgressMilestone>> GetMilestonesAsync(string sessionId);
Task CheckMilestonesAsync(string sessionId);
```

#### Real-Time Updates
```csharp
void SubscribeToUpdates(string sessionId, Func<ProgressUpdateEvent, Task> callback);
void UnsubscribeFromUpdates(string sessionId);
IAsyncEnumerable<ProgressUpdateEvent> GetUpdatesStreamAsync(string sessionId, CancellationToken cancellationToken = default);
Task BroadcastUpdateAsync(ProgressUpdateEvent update);
```

#### Configuration
```csharp
void Configure(ProgressTrackingConfig config);
ProgressTrackingConfig GetConfiguration();
```

#### Historical Data
```csharp
Task RecordPhaseMetricsAsync(string phaseName, long durationMs);
Task<ProgressEstimate> GetHistoricalMetricsAsync(string phaseName);
Task PurgeOldHistoryAsync(int retentionDays);
```

### Implementation Details

**State Storage:**
- In-memory Dictionary<string, ProgressTracker> for active sessions
- Dictionary<string, List<ProgressHistoryEntry>> for historical snapshots
- Dictionary<string, Queue<ProgressUpdateEvent>> for event history (max 1000 per session)
- Dictionary<string, List<Func<ProgressUpdateEvent, Task>>> for subscribers
- Dictionary<string, List<ProgressMilestone>> for milestone tracking

**Estimation Algorithm:**
- Uses current elapsed time and progress percentage
- Calculates rate: `time_per_percent = elapsed_ms / progress_percentage`
- Projects remaining time: `(100 - current_progress) * time_per_percent`
- Adjusts based on historical velocity trends

**Velocity Calculation:**
- Current velocity: `progress_change / elapsed_seconds`
- Compares to average to detect trend (accelerating/stable/decelerating)
- Updates every 500ms by default (configurable)

**Milestone Checking:**
- Runs whenever overall progress updates
- Checks if `tracker.OverallProgress >= milestone.TargetProgress`
- Sets `milestone.CompletedAt = DateTime.UtcNow` if reached
- Broadcasts "MilestoneReached" event

**Broadcasting:**
- SubscribeToUpdates adds callback to session's subscriber list
- BroadcastUpdateAsync calls all callbacks with `await Task.WhenAll`
- GetUpdatesStreamAsync yields from _updates queue with 100ms polling
- UnsubscribeFromUpdates removes all callbacks for session

## Usage Examples

### Basic Session Initialization
```csharp
var service = new ProgressService();

// Initialize tracking for 6-phase pipeline
var tracker = await service.InitializeProgressAsync("session-xyz",
    new List<string> { "Discovery", "TestGen", "Execution", "Comparison", "Review", "Reporting" });

Console.WriteLine($"Initialized {tracker.PhaseProgresses.Count} phases");
```

### Tracking Phase Execution
```csharp
// Start Discovery phase with 5 expected steps
var phase = await service.StartPhaseAsync("session-xyz", "Discovery", 1, 5);
Console.WriteLine($"Phase {phase.PhaseName} started at {phase.StartTime}");

// Execute steps
for (int i = 1; i <= 5; i++)
{
    var step = await service.StartStepAsync("session-xyz", "Discovery", $"AnalyzeTable{i}", i);
    
    // Simulate work with progress updates
    for (int progress = 0; progress <= 100; progress += 20)
    {
        await service.UpdateStepProgressAsync("session-xyz", "Discovery", $"AnalyzeTable{i}", progress);
        await Task.Delay(100);
    }
    
    // Add output artifact
    var completed = await service.CompleteStepAsync("session-xyz", "Discovery", $"AnalyzeTable{i}");
    await service.AddStepArtifactAsync("session-xyz", "Discovery", $"AnalyzeTable{i}", 
        "/output/discovery/table-schema-{i}.json");
}

// Complete phase
var completed = await service.CompletePhaseAsync("session-xyz", "Discovery");
Console.WriteLine($"Discovery completed in {completed.DurationMs}ms");
```

### Monitoring Progress and Velocity
```csharp
// Real-time monitoring callback
async Task OnProgressUpdate(ProgressUpdateEvent update)
{
    var tracker = await service.GetProgressAsync("session-xyz");
    Console.WriteLine($"Overall: {tracker.OverallProgress}% | Elapsed: {tracker.ElapsedMs}ms");
    
    if (update.EventType == "PhaseCompleted")
    {
        var velocity = await service.CalculateVelocityAsync("session-xyz");
        Console.WriteLine($"Velocity: {velocity.CurrentVelocityPercentPerSecond:F2}%/sec (Trend: {velocity.Trend})");
    }
}

// Subscribe to updates
service.SubscribeToUpdates("session-xyz", OnProgressUpdate);

// Or use async streaming
await foreach (var update in service.GetUpdatesStreamAsync("session-xyz"))
{
    Console.WriteLine($"[{update.Timestamp:HH:mm:ss}] {update.EventType}: {update.PhaseName}");
}
```

### Estimation and Statistics
```csharp
// Get completion estimate
var completionTime = await service.GetCompletionEstimateAsync("session-xyz");
Console.WriteLine($"Estimated completion: {completionTime:yyyy-MM-dd HH:mm:ss}");

// Get remaining time
var remainingMs = await service.GetRemainingTimeEstimateAsync("session-xyz");
Console.WriteLine($"Estimated remaining: {TimeSpan.FromMilliseconds(remainingMs):hh\\:mm\\:ss}");

// Get detailed statistics
var stats = await service.GetStatisticsAsync("session-xyz");
Console.WriteLine($"Phases: {stats.PhasesCompleted}/{stats.TotalPhases}");
Console.WriteLine($"Steps: {stats.StepsCompleted}/{stats.TotalSteps}");
Console.WriteLine($"Avg velocity: {stats.AverageProgressVelocity:F2}%/sec");
Console.WriteLine($"Avg phase time: {TimeSpan.FromMilliseconds(stats.AveragePhaseTime):hh\\:mm\\:ss}");
```

### Milestone Tracking
```csharp
// Define progress checkpoints
var m1 = await service.AddMilestoneAsync("session-xyz", "Schema Analysis Complete", 25, "high");
var m2 = await service.AddMilestoneAsync("session-xyz", "Halfway Done", 50, "normal");
var m3 = await service.AddMilestoneAsync("session-xyz", "Nearly Complete", 75, "low");

// Milestones are checked automatically on progress updates
// Listen for MilestoneReached events
service.SubscribeToUpdates("session-xyz", async update =>
{
    if (update.EventType == "MilestoneReached")
    {
        Console.WriteLine($"✓ Milestone reached: {update.Message}");
    }
});

// Or manually check
await service.CheckMilestonesAsync("session-xyz");
var milestones = await service.GetMilestonesAsync("session-xyz");
var completed = milestones.Where(m => m.IsCompleted);
```

### Configuration
```csharp
// Customize tracking behavior
var config = new ProgressTrackingConfig
{
    EnableAutomaticEstimates = true,
    HistoricalSampleSize = 20,
    UpdateIntervalMs = 1000,
    EnableMilestones = true,
    EnableDetailedSteps = true,
    KeepHistory = true,
    HistoryRetentionDays = 60
};

service.Configure(config);
```

### Error Handling
```csharp
try
{
    var phase = await service.StartPhaseAsync("session-xyz", "Execution", 3, 10);
    // ... perform phase work ...
}
catch (Exception ex)
{
    // Mark phase as failed
    await service.FailPhaseAsync("session-xyz", "Execution", ex.Message);
    
    var tracker = await service.GetProgressAsync("session-xyz");
    if (tracker.Status == "failed")
    {
        Console.WriteLine("Pipeline halted due to phase failure");
    }
}
```

## Integration Patterns

### With Dashboard Service
```csharp
// Link progress tracking to dashboard updates
service.SubscribeToUpdates("session-xyz", async update =>
{
    var dashboard = await dashboardService.GetDashboardStateAsync("session-xyz");
    
    if (update.EventType == "PhaseCompleted")
    {
        await dashboardService.CompletePhaseAsync("session-xyz", 
            update.PhaseName, 
            update.ElapsedMs.Value);
    }
});
```

### With Orchestrator
```csharp
// Track orchestration progress
var tracker = await progressService.InitializeProgressAsync(sessionId, 
    new List<string> { "Discovery", "TestGen", "Execution", "Comparison", "Review", "Reporting" });

for (int i = 0; i < phases.Count; i++)
{
    var phase = phases[i];
    await progressService.StartPhaseAsync(sessionId, phase.Name, i + 1, phase.StepCount);
    
    try
    {
        await orchestrator.ExecutePhaseAsync(sessionId, phase);
        await progressService.CompletePhaseAsync(sessionId, phase.Name);
    }
    catch (Exception ex)
    {
        await progressService.FailPhaseAsync(sessionId, phase.Name, ex.Message);
        throw;
    }
}

var stats = await progressService.GetStatisticsAsync(sessionId);
```

## Performance Characteristics

**Time Complexity:**
- `InitializeProgressAsync`: O(1) - creates tracker
- `StartPhaseAsync`: O(1) - updates phase list
- `CompleteStepAsync`: O(n) - searches phase steps (n ≤ ~20)
- `GetProgressAsync`: O(1) - returns cached tracker
- `BroadcastUpdateAsync`: O(m) - calls m subscribers
- `CalculateVelocityAsync`: O(1) - simple calculation

**Space Complexity:**
- Per session: ~1KB for ProgressTracker
- History: ~100 bytes per HistoryEntry × count
- Updates: ~200 bytes per ProgressUpdateEvent (max 1000/session)
- Subscribers: pointer per callback function

**Scalability:**
- 1000 concurrent sessions: ~1-2 MB total memory
- Event broadcasting: 50+ subscribers per session practical limit
- History retention: configurable purge removes old entries

## Configuration Recommendations

**High-Frequency Tracking (UI updates every 200ms):**
```csharp
UpdateIntervalMs = 200
EnableDetailedSteps = true
HistoricalSampleSize = 20
```

**Batch Monitoring (infrequent checks):**
```csharp
UpdateIntervalMs = 5000
EnableDetailedSteps = false
HistoricalSampleSize = 5
```

**Long-Running Pipelines (7-day migration):**
```csharp
HistoryRetentionDays = 60
KeepHistory = true
EnableAutomaticEstimates = true
HistoricalSampleSize = 10
```

## Testing Strategy

The test suite (`ProgressServiceTests.cs`) includes 47 unit tests covering:
- Initialization and state retrieval
- Phase lifecycle (start, update, complete, fail)
- Step tracking and artifact collection
- Progress event recording and retrieval
- Velocity and statistics calculation
- Milestone detection and completion
- Subscriber callbacks and async streaming
- Configuration management
- End-to-end integration scenario

**Key Test Scenarios:**
1. Single phase completion with step tracking
2. Multiple concurrent phases with velocity comparison
3. Milestone reaching at exact progress percentage
4. Subscriber notification on updates
5. Streaming updates with cancellation
6. Estimation accuracy based on current rate

## Deployment Notes

- Service is stateless except for in-memory storage (suitable for single-instance)
- For horizontal scaling, migrate _trackers to distributed cache (Redis)
- Session cleanup: Consider implementing idle session removal (> 24h inactive)
- Metrics export: Wire GetStatisticsAsync into observability system
- Broadcasts: Consider throttling if > 100 subscribers per session

## Related Forms

- **Form 9: Dashboard Service** - Consumes progress events
- **Form 11: Metrics Service** - Consumes velocity/statistics
- **Form 12: Status Cache** - Distributes progress snapshots
- **Form 16: Ruflow Orchestrator** - Drives phase transitions

## See Also

- `IProgressService`: Interface definition with full method signatures
- `ProgressService`: Implementation with in-memory state management
- `ProgressTrackingModels.cs`: All data model definitions
- `ProgressServiceTests.cs`: 47 comprehensive unit tests
