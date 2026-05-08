namespace ValidationOrchestrator.Comparison;

using System.Diagnostics;
using System.Text.Json;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Validation session memory service implementation
/// Manages context across all 6 phases of validation
/// </summary>
public class ValidationMemoryService : IValidationMemoryService
{
    private readonly ILogger _logger;
    private readonly ICavemanCompressionService _compressionService;
    private MemoryServiceConfiguration _config;

    // In-memory session store
    private Dictionary<string, ValidationSession> _sessions = new();

    // Decompression cache to avoid repeated decompression
    private Dictionary<string, CachedDecompression> _decompressionCache = new();

    public ValidationMemoryService(
        ICavemanCompressionService compressionService,
        ILogger? logger = null)
    {
        _compressionService = compressionService;
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _config = new MemoryServiceConfiguration();
        _logger.Information("Validation memory service initialized");
    }

    public async Task<ValidationSession> CreateSessionAsync(string clientId, string? clientName = null)
    {
        var session = new ValidationSession
        {
            ClientId = clientId,
            ClientName = clientName,
            StartedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        // Initialize phase statuses
        foreach (ValidationPhase phase in Enum.GetValues(typeof(ValidationPhase)))
        {
            session.PhaseStatuses[phase] = new PhaseStatus
            {
                Phase = phase,
                State = phase == ValidationPhase.Initialize ? PhaseState.InProgress : PhaseState.Pending,
                StartedAt = phase == ValidationPhase.Initialize ? DateTime.UtcNow : DateTime.MinValue
            };
        }

        _sessions[session.Id] = session;

        _logger.Information(
            "Validation session created: {SessionId} for client {ClientId}",
            session.Id,
            clientId);

        if (_config.EnablePersistence)
        {
            await PersistSessionAsync(session);
        }

        return session;
    }

    public async Task<ValidationSession?> GetSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastUpdated = DateTime.UtcNow;
            return session;
        }

        // Try to restore from storage
        if (_config.EnablePersistence)
        {
            var restored = await RestoreSessionAsync(sessionId);
            if (restored != null)
            {
                _sessions[sessionId] = restored;
                return restored;
            }
        }

        _logger.Warning("Session not found: {SessionId}", sessionId);
        return null;
    }

    public async Task UpdateSessionAsync(ValidationSession session)
    {
        session.LastUpdated = DateTime.UtcNow;
        _sessions[session.Id] = session;

        if (_config.EnablePersistence && _config.AutoPersistAfterPhase)
        {
            await PersistSessionAsync(session);
        }

        _logger.Debug("Session updated: {SessionId}", session.Id);
    }

    public async Task ArchiveSessionAsync(string sessionId, string reason = "Validation completed")
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        session.IsArchived = true;
        session.ArchiveReason = reason;
        session.CompletedAt = DateTime.UtcNow;

        await UpdateSessionAsync(session);

        _logger.Information(
            "Session archived: {SessionId} - Reason: {Reason}",
            sessionId,
            reason);
    }

    public async Task TransitionPhaseAsync(string sessionId, ValidationPhase nextPhase)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        var currentPhase = session.CurrentPhase;
        var stopwatch = Stopwatch.StartNew();

        // Mark current phase as complete
        if (session.PhaseStatuses.TryGetValue(currentPhase, out var currentStatus))
        {
            currentStatus.State = PhaseState.Completed;
            currentStatus.CompletedAt = DateTime.UtcNow;
            stopwatch.Stop();
            currentStatus.DurationMs = stopwatch.ElapsedMilliseconds;
        }

        // Mark next phase as in progress
        session.CurrentPhase = nextPhase;
        if (session.PhaseStatuses.TryGetValue(nextPhase, out var nextStatus))
        {
            nextStatus.State = PhaseState.InProgress;
            nextStatus.StartedAt = DateTime.UtcNow;
        }

        await UpdateSessionAsync(session);

        _logger.Information(
            "Phase transition: {SessionId} - {CurrentPhase} → {NextPhase}",
            sessionId,
            currentPhase,
            nextPhase);
    }

    public async Task<PhaseStatus?> GetPhaseStatusAsync(string sessionId, ValidationPhase phase)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return null;

        return session.PhaseStatuses.TryGetValue(phase, out var status) ? status : null;
    }

    public async Task UpdatePhaseStatusAsync(string sessionId, PhaseStatus status)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        session.PhaseStatuses[status.Phase] = status;
        await UpdateSessionAsync(session);

        _logger.Debug(
            "Phase status updated: {SessionId} - {Phase} - {State}",
            sessionId,
            status.Phase,
            status.State);
    }

    public async Task StoreArtifactAsync(string sessionId, CompressedArtifact artifact)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        var phaseKey = artifact.Phase;
        if (!session.PhaseArtifacts.ContainsKey(phaseKey))
        {
            session.PhaseArtifacts[phaseKey] = new List<CompressedArtifact>();
        }

        session.PhaseArtifacts[phaseKey].Add(artifact);

        // Update session size
        session.TotalSizeBytes += artifact.OriginalContent.Length;
        session.CompressedSizeBytes += artifact.CompressedContent.Length;

        await UpdateSessionAsync(session);

        _logger.Debug(
            "Artifact stored: {SessionId} - {Phase} - {Type} ({Size} bytes)",
            sessionId,
            artifact.Phase,
            artifact.ArtifactType,
            artifact.CompressedContent.Length);
    }

    public async Task<CompressedArtifact?> GetArtifactAsync(string sessionId, string artifactId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return null;

        foreach (var artifacts in session.PhaseArtifacts.Values)
        {
            var artifact = artifacts.FirstOrDefault(a => a.Id == artifactId);
            if (artifact != null) return artifact;
        }

        return null;
    }

    public async Task<List<CompressedArtifact>> GetPhaseArtifactsAsync(
        string sessionId,
        ValidationPhase phase)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return new();

        var phaseKey = phase.ToString();
        return session.PhaseArtifacts.TryGetValue(phaseKey, out var artifacts)
            ? artifacts
            : new();
    }

    public async Task<string> GetContextForPhaseAsync(string sessionId, ValidationPhase phase)
    {
        var context = new List<string>();

        // Get artifacts from this phase
        var artifacts = await GetPhaseArtifactsAsync(sessionId, phase);
        foreach (var artifact in artifacts)
        {
            var decompressed = await DecompressArtifactAsync(artifact);
            if (!string.IsNullOrEmpty(decompressed))
            {
                context.Add($"[{artifact.ArtifactType}] {decompressed}");
            }
        }

        return string.Join("\n\n", context);
    }

    public async Task<string> GetPreviousPhaseContextAsync(string sessionId, ValidationPhase upToPhase)
    {
        var context = new List<string>();
        var session = await GetSessionAsync(sessionId);
        if (session == null) return "";

        // Iterate through phases up to specified phase
        for (int i = 0; i < (int)upToPhase; i++)
        {
            var phase = (ValidationPhase)i;
            var phaseContext = await GetContextForPhaseAsync(sessionId, phase);
            if (!string.IsNullOrEmpty(phaseContext))
            {
                context.Add($"[{phase}] {phaseContext}");
            }
        }

        return string.Join("\n\n", context);
    }

    public async Task<string> GetFullSessionContextAsync(string sessionId)
    {
        return await GetPreviousPhaseContextAsync(sessionId, ValidationPhase.Complete);
    }

    public async Task StoreDiscoveryFindingsAsync(string sessionId, DiscoveryFindings findings)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        session.DiscoveryFindings = findings;
        await UpdateSessionAsync(session);

        _logger.Information(
            "Discovery findings stored: {SessionId} - {ItemCount} items",
            sessionId,
            findings.TotalBusinessLogicItems);
    }

    public async Task<DiscoveryFindings?> GetDiscoveryFindingsAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        return session?.DiscoveryFindings;
    }

    public async Task StoreExecutionResultsAsync(string sessionId, ExecutionResults results)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        session.ExecutionResults = results;
        await UpdateSessionAsync(session);

        _logger.Information(
            "Execution results stored: {SessionId} - {TestCount} tests",
            sessionId,
            results.TotalTestsRun);
    }

    public async Task<ExecutionResults?> GetExecutionResultsAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        return session?.ExecutionResults;
    }

    public async Task StoreDiscrepanciesAsync(string sessionId, List<Discrepancy> discrepancies)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        session.Discrepancies = discrepancies;
        await UpdateSessionAsync(session);

        var criticalCount = discrepancies.Count(d => d.Severity == DiscrepancySeverity.Critical);
        _logger.Information(
            "Discrepancies stored: {SessionId} - {TotalCount} total, {CriticalCount} critical",
            sessionId,
            discrepancies.Count,
            criticalCount);
    }

    public async Task<List<Discrepancy>> GetDiscrepanciesAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        return session?.Discrepancies ?? new();
    }

    public async Task StoreRiskAssessmentAsync(string sessionId, RiskAssessment assessment)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        session.RiskAssessment = assessment;
        await UpdateSessionAsync(session);

        _logger.Information(
            "Risk assessment stored: {SessionId} - Score: {RiskScore:F1} - {Decision}",
            sessionId,
            assessment.OverallRiskScore,
            assessment.GoNoGo ? "GO" : "NO-GO");
    }

    public async Task<RiskAssessment?> GetRiskAssessmentAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        return session?.RiskAssessment;
    }

    public async Task PersistSessionAsync(ValidationSession session)
    {
        try
        {
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });

            // Compress the entire session
            var compressed = await _compressionService.CompressAsync(
                json,
                session.DefaultCompressionLevel);

            if (!Directory.Exists(_config.PersistencePath))
            {
                Directory.CreateDirectory(_config.PersistencePath);
            }

            var filePath = Path.Combine(_config.PersistencePath, $"{session.Id}.session");
            await File.WriteAllTextAsync(filePath, compressed.CompressedContent);

            _logger.Information(
                "Session persisted: {SessionId} - Compressed: {Size} bytes ({Reduction:F1}%)",
                session.Id,
                compressed.CompressedLength,
                compressed.EstimatedReduction);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to persist session: {SessionId}", session.Id);
        }
    }

    public async Task<ValidationSession?> RestoreSessionAsync(string sessionId)
    {
        try
        {
            var filePath = Path.Combine(_config.PersistencePath, $"{sessionId}.session");
            if (!File.Exists(filePath))
            {
                _logger.Warning("Session file not found: {FilePath}", filePath);
                return null;
            }

            var compressed = await File.ReadAllTextAsync(filePath);

            // Decompress the session
            var decompressed = await _compressionService.DecompressAsync(compressed);
            if (!decompressed.Success)
            {
                _logger.Error("Failed to decompress session: {SessionId}", sessionId);
                return null;
            }

            var session = JsonSerializer.Deserialize<ValidationSession>(
                decompressed.DecompressedContent);

            _logger.Information("Session restored: {SessionId}", sessionId);
            return session;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to restore session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<List<ValidationSession>> ListSessionsAsync(string? clientId = null)
    {
        var sessions = _sessions.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(clientId))
        {
            sessions = sessions.Where(s => s.ClientId == clientId);
        }

        return sessions.OrderByDescending(s => s.StartedAt).ToList();
    }

    public SessionMemoryStats GetMemoryStatistics(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return new();
        }

        var stats = new SessionMemoryStats
        {
            TotalPhases = Enum.GetValues(typeof(ValidationPhase)).Length,
            CompletedPhases = session.PhaseStatuses.Values.Count(p => p.State == PhaseState.Completed),
            TotalArtifacts = session.PhaseArtifacts.Values.Sum(a => a.Count),
            TotalOriginalSize = session.TotalSizeBytes,
            TotalCompressedSize = session.CompressedSizeBytes
        };

        if (stats.TotalOriginalSize > 0)
        {
            stats.CompressionSavings = 100 * (1 - (double)stats.TotalCompressedSize / stats.TotalOriginalSize);
        }

        stats.CachedItems = _decompressionCache.Count;
        stats.CacheMemoryUsage = _decompressionCache.Values.Sum(c => c.Content.Length);

        foreach (var kvp in session.PhaseArtifacts)
        {
            stats.ArtifactsByPhase[kvp.Key] = kvp.Value.Count;
            foreach (var artifact in kvp.Value)
            {
                if (stats.ArtifactsByType.ContainsKey(artifact.ArtifactType))
                {
                    stats.ArtifactsByType[artifact.ArtifactType]++;
                }
                else
                {
                    stats.ArtifactsByType[artifact.ArtifactType] = 1;
                }
            }
        }

        return stats;
    }

    public Dictionary<string, int> GetCacheStatistics()
    {
        return new Dictionary<string, int>
        {
            ["TotalCacheItems"] = _decompressionCache.Count,
            ["CacheMemoryUsage"] = _decompressionCache.Values.Sum(c => c.Content.Length),
            ["SessionsInMemory"] = _sessions.Count
        };
    }

    public void ClearCache()
    {
        var clearedItems = _decompressionCache.Count;
        _decompressionCache.Clear();
        _logger.Information("Decompression cache cleared: {ItemCount} items removed", clearedItems);
    }

    public async Task ClearSessionMemoryAsync(string sessionId)
    {
        if (_sessions.Remove(sessionId))
        {
            // Remove related cache entries
            var cacheKeysToRemove = _decompressionCache.Keys
                .Where(k => k.StartsWith(sessionId))
                .ToList();

            foreach (var key in cacheKeysToRemove)
            {
                _decompressionCache.Remove(key);
            }

            _logger.Information(
                "Session memory cleared: {SessionId} - Removed {CacheEntries} cache entries",
                sessionId,
                cacheKeysToRemove.Count);
        }
    }

    /// <summary>
    /// Decompress artifact with caching
    /// </summary>
    private async Task<string> DecompressArtifactAsync(CompressedArtifact artifact)
    {
        var cacheKey = $"{artifact.Id}_{artifact.Phase}";

        // Check cache
        if (_config.EnableCaching && _decompressionCache.TryGetValue(cacheKey, out var cached))
        {
            cached.LastAccessedAt = DateTime.UtcNow;
            return cached.Content;
        }

        // Decompress
        var result = await _compressionService.DecompressAsync(artifact.CompressedContent);
        if (!result.Success)
        {
            return artifact.CompressedContent; // Return compressed if decompression fails
        }

        // Cache the decompressed content
        if (_config.EnableCaching && _decompressionCache.Count < _config.CacheSizeLimit)
        {
            _decompressionCache[cacheKey] = new CachedDecompression
            {
                Content = result.DecompressedContent,
                CachedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };
        }

        return result.DecompressedContent;
    }

    /// <summary>
    /// Cached decompressed content
    /// </summary>
    private class CachedDecompression
    {
        public string Content { get; set; } = string.Empty;
        public DateTime CachedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }
}
