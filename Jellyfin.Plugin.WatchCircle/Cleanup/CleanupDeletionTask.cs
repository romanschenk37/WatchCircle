using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Runs explicitly enabled automatic deletions through Jellyfin's scheduler.</summary>
public sealed class CleanupDeletionTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly CleanupService _cleanup;
    private readonly IServerConfigurationManager _configuration;

    /// <summary>Initializes a new instance of the <see cref="CleanupDeletionTask"/> class.</summary>
    /// <param name="cleanup">Shared cleanup workflow.</param>
    /// <param name="configuration">Server language for scheduled task labels.</param>
    public CleanupDeletionTask(CleanupService cleanup, IServerConfigurationManager configuration)
    {
        _cleanup = cleanup;
        _configuration = configuration;
    }

    private bool German => _configuration.Configuration.UICulture.StartsWith("de", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string Name => German ? "Bibliothek aufräumen: Fällige Inhalte löschen" : "Library cleanup: Delete due titles";

    /// <inheritdoc />
    public string Key => "WatchCircleLibraryCleanupDeletion";

    /// <inheritdoc />
    public string Description => German
        ? "Löscht fällige Inhalte über Radarr/Sonarr nur bei ausdrücklich aktivierter automatischer Löschung und nach erfolgreicher Auswertung. Fristen, Schutz und Wiedergaben werden erneut geprüft."
        : "Deletes due titles through Radarr/Sonarr only when automatic deletion is explicitly enabled and an evaluation has succeeded. Rechecks deadlines, protection and playback.";

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
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(1).Ticks
        };
    }

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        => _cleanup.DeleteAutomaticallyAsync(cancellationToken, progress);
}
