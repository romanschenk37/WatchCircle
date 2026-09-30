using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Exposes library evaluation to Jellyfin's scheduler and manual task controls.</summary>
public sealed class CleanupEvaluationTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly CleanupService _cleanup;
    private readonly IServerConfigurationManager _configuration;

    /// <summary>Initializes a new instance of the <see cref="CleanupEvaluationTask"/> class.</summary>
    /// <param name="cleanup">Shared cleanup workflow.</param>
    /// <param name="configuration">Server language for scheduled task labels.</param>
    public CleanupEvaluationTask(CleanupService cleanup, IServerConfigurationManager configuration)
    {
        _cleanup = cleanup;
        _configuration = configuration;
    }

    private bool German => _configuration.Configuration.UICulture.StartsWith("de", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string Name => German ? "Bibliothek aufräumen: Inhalte prüfen" : "Library cleanup: Evaluate titles";

    /// <inheritdoc />
    public string Key => "WatchCircleLibraryCleanupEvaluation";

    /// <inheritdoc />
    public string Description => German
        ? "Prüft bei aktivierter WatchCircle-Aufräumfunktion den Bestand und merkt inaktive Inhalte zur Löschung vor. Startet selbst keine Löschungen."
        : "When WatchCircle library cleanup is enabled, evaluates the library and nominates inactive titles for deletion. Does not initiate deletions itself.";

    /// <inheritdoc />
    public string Category => "WatchCircle";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(_cleanup.DefaultEvaluationIntervalHours).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_cleanup.StorageFault is not null)
        {
            throw new InvalidOperationException(_cleanup.StorageFault);
        }

        if (!_cleanup.Enabled)
        {
            progress.Report(100);
            return;
        }

        await _cleanup.EvaluateAsync(cancellationToken, progress).ConfigureAwait(false);
    }
}
