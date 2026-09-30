using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Optional server-side cleanup workflow; ordinary WatchCircle visibility is untouched.</summary>
public sealed class CleanupService : IHostedService, IDisposable
{
    private readonly CleanupStore _store;
    private readonly ICleanupLibrary _library;
    private readonly ArrClient _arr;
    private readonly TimeProvider _time;
    private readonly ILogger<CleanupService> _logger;
    private readonly SemaphoreSlim _operations = new(1, 1);
    private readonly IUserDataManager? _userData;
    private readonly ILibraryManager? _jellyfin;
    private readonly SeerrService? _seerr;
    private int _settingsRevision;
    private volatile bool _evaluationReady;

    /// <summary>Initializes a new instance of the <see cref="CleanupService"/> class.</summary>
    /// <param name="paths">Persistent plugin configuration directory.</param>
    /// <param name="library">Existing Jellyfin library.</param>
    /// <param name="users">Existing accounts.</param>
    /// <param name="data">Existing user states and server events.</param>
    /// <param name="sessions">Active playback sessions.</param>
    /// <param name="http">Bounded external HTTP clients.</param>
    /// <param name="seerr">Existing request attribution.</param>
    /// <param name="logger">Operational errors.</param>
    public CleanupService(IApplicationPaths paths, ILibraryManager library, IUserManager users, IUserDataManager data, ISessionManager sessions, IHttpClientFactory http, SeerrService seerr, ILogger<CleanupService> logger)
        : this(new CleanupStore(paths), new CleanupLibrary(library, users, data, sessions), new ArrClient(http.CreateClient("WatchCircle.Arr")), TimeProvider.System, logger)
    {
        _userData = data;
        _jellyfin = library;
        _seerr = seerr;
    }

    internal CleanupService(CleanupStore store, ICleanupLibrary library, ArrClient arr, TimeProvider time, ILogger<CleanupService> logger)
    {
        _store = store;
        _library = library;
        _arr = arr;
        _time = time;
        _logger = logger;
    }

    internal bool Enabled => _store.Fault is null && _store.Read().Settings.Enabled;

    internal int DefaultEvaluationIntervalHours => Math.Clamp(_store.Read().Settings.IntervalHours, 1, 744);

    internal string? StorageFault => _store.Fault;

    internal object GetSettings()
    {
        var settings = _store.Read().Settings;
        foreach (var connection in new[] { settings.Radarr, settings.Sonarr })
        {
            connection.HasApiKey = !string.IsNullOrEmpty(connection.ApiKey);
            connection.ApiKey = string.Empty;
        }

        return new { Settings = settings, Libraries = _library.Libraries(), Error = _store.Fault };
    }

    internal void SaveSettings(CleanupSettings settings)
    {
        if (settings.InactivityMonths is < 1 or > 120 || settings.WarningDays is < 1 or > 3650
            || (settings.Enabled && settings.LibraryIds.Count == 0))
        {
            throw new ArgumentException("Choose libraries, 1–120 months of inactivity and 1–3650 warning days.");
        }

        _store.Change(state =>
        {
            ValidateConnection(settings.Radarr, state.Settings.Radarr);
            ValidateConnection(settings.Sonarr, state.Settings.Sonarr);
            if (settings.Enabled && !state.Settings.Enabled)
            {
                state.ActivatedAt = _time.GetUtcNow();
                state.LastEvaluation = null;
                foreach (var entry in state.Entries)
                {
                    entry.Baseline = state.ActivatedAt.Value;
                    CleanupRules.Cancel(entry);
                }
            }

            // The old interval seeds Jellyfin's default trigger; the scheduler owns future changes.
            settings.IntervalHours = state.Settings.IntervalHours;
            state.Settings = CleanupStore.Clone(settings);
            _settingsRevision++;
            _evaluationReady = false;
            foreach (var entry in state.Entries)
            {
                entry.Revision++;
            }

            foreach (var entry in state.Entries.Where(entry => !settings.Enabled || !CleanupRules.IsSelected(state, entry)))
            {
                CleanupRules.Cancel(entry);
            }

            return true;
        });
    }

    private static void ValidateConnection(ArrConnection next, ArrConnection previous)
    {
        next.Url = next.Url.Trim().TrimEnd('/');
        if (next.Url.Length > 0 && (!Uri.TryCreate(next.Url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0))
        {
            throw new ArgumentException("Use an HTTP(S) URL without credentials, query or fragment.");
        }

        if (next.Url != previous.Url && string.IsNullOrWhiteSpace(next.ApiKey) && !next.ClearApiKey && previous.ApiKey.Length > 0)
        {
            throw new ArgumentException("Enter the API key again when changing the service URL.");
        }

        next.ApiKey = next.ClearApiKey ? string.Empty : string.IsNullOrWhiteSpace(next.ApiKey) ? previous.ApiKey : next.ApiKey.Trim();
        next.ClearApiKey = false;
        if (next.ApiKey.Contains('\r', StringComparison.Ordinal) || next.ApiKey.Contains('\n', StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid API key.");
        }

        foreach (var mapping in next.Paths)
        {
            ArrClient.NormalizePath(mapping.Jellyfin);
            ArrClient.NormalizePath(mapping.Arr);
        }
    }

    internal Task<string> TestAsync(string service, CancellationToken cancellationToken)
    {
        var settings = _store.Read().Settings;
        return _arr.TestAsync(service == "Sonarr" ? settings.Sonarr : settings.Radarr, cancellationToken);
    }

    internal object Pending(Guid userId)
    {
        var state = _store.Read();
        if (!Enabled)
        {
            return Array.Empty<object>();
        }

        return state.Entries.Where(entry => CleanupRules.NeedsReply(state, entry, userId) && _library.CanAccess(entry.Media.ItemId, userId))
            .Select(entry => new { entry.Id, entry.Media.ItemId, entry.Media.Name, entry.Media.Kind, entry.NominationId, entry.DeleteAt }).ToArray();
    }

    internal void Reply(Guid entryId, Guid userId, CleanupReplyRequest request)
    {
        var user = _library.Users().SingleOrDefault(value => value.Id == userId) ?? throw new UnauthorizedAccessException();
        _store.Change(state =>
        {
            var entry = state.Entries.Single(value => value.Id == entryId);
            if (!_library.CanAccess(entry.Media.ItemId, userId))
            {
                throw new UnauthorizedAccessException();
            }

            CleanupRules.Reply(state, entry, request.NominationId, userId, user.Name, request.Answer, _time.GetUtcNow());
            return true;
        });
    }

    internal object ItemStatuses(Guid userId, IReadOnlyList<Guid> itemIds)
    {
        if (itemIds.Count > 200)
        {
            throw new ArgumentException("Request at most 200 items at a time.");
        }

        var state = _store.Read();
        if (!Enabled)
        {
            return Array.Empty<object>();
        }

        var result = new List<object>();
        foreach (var itemId in itemIds.Distinct().Where(id => _library.CanAccess(id, userId)))
        {
            var rootId = _library.RootItemId(itemId);
            var entry = state.Entries.FirstOrDefault(value => value.Media.ItemId == rootId && value.Present && value.NominationId.HasValue);
            if (entry is null || CleanupRules.IsProtected(state, entry) || !_library.CanAccess(rootId, userId))
            {
                continue;
            }

            var reply = state.Replies.LastOrDefault(value => value.EntryId == entry.Id && value.UserId == userId && value.Active);
            result.Add(new
            {
                ItemId = itemId, RootItemId = rootId, entry.Id, entry.Media.Name, entry.Media.Kind, entry.NominationId, entry.DeleteAt,
                Answer = reply?.Answer, AnswerAt = reply?.At,
                Deleting = state.Deletions.Any(job => job.EntryId == entry.Id && job.Generation == entry.Generation && job.Phase != "Deleted")
            });
        }

        return result;
    }

    internal object AdminView()
    {
        var state = _store.Read();
        var users = _library.Users();
        return new
        {
            state.Settings.Enabled, state.Settings.AutomaticDeletion, state.LastEvaluation,
            Error = _store.Fault ?? state.Error,
            Entries = state.Entries.Select(entry => new
            {
                Entry = entry, EffectiveProtection = CleanupRules.IsProtected(state, entry),
                LastUserName = users.FirstOrDefault(user => user.Id == entry.LastUserId)?.Name,
                Collections = state.Entries.Where(collection => entry.Media.Collections.Contains(collection.Media.ItemId)).Select(collection => new { collection.Id, collection.Media.Name }),
                Replies = state.Replies.Where(reply => reply.EntryId == entry.Id),
                Deletions = state.Deletions.Where(job => job.EntryId == entry.Id),
                RestoreErrors = state.Archives.Where(archive => archive.EntryId == entry.Id && archive.Error is not null).Select(archive => archive.Error).Distinct()
            })
        };
    }

    internal async Task<object> AdminDetailsAsync(Guid entryId, Guid adminId, CancellationToken cancellationToken)
    {
        var state = _store.Read();
        var entry = state.Entries.Single(value => value.Id == entryId);
        var users = _library.Users();
        var states = entry.Present && entry.Media.Kind != "Collection" ? _library.ReadStates(entry.Media) : Array.Empty<CleanupUserState>();
        var requesters = entry.Present && _seerr is not null
            ? await _seerr.GetAdminRequestersAsync(entry.Media.ItemId, adminId, users.Select(user => user.Id).ToArray(), cancellationToken).ConfigureAwait(false)
            : Array.Empty<SeerrRequesterDto>();
        string? mappingError = null;
        ArrMatch? mapping = null;
        if (entry.Present && entry.Media.Kind != "Collection")
        {
            try
            {
                mapping = await _arr.MatchAsync(entry.Media, Connection(state, entry), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException or TaskCanceledException)
            {
                mappingError = SafeError(ex);
            }
        }

        return new
        {
            Entry = entry, Mapping = mapping, MappingError = mappingError, Requesters = requesters,
            Members = state.Entries.Where(value => entry.Media.Members.Contains(value.Media.ItemId)).Select(value => new { value.Id, value.Media.Name }),
            Users = users.Select(user => new
            {
                user.Id, user.Name, Progress = states.FirstOrDefault(value => value.UserId == user.Id)?.Progress,
                Replies = state.Replies.Where(reply => reply.EntryId == entryId && reply.UserId == user.Id),
                Open = CleanupRules.NeedsReply(state, entry, user.Id) && _library.CanAccess(entry.Media.ItemId, user.Id),
                IsRequester = requesters.Any(requester => requester.ProfileUserId == user.Id)
            })
        };
    }

    internal void Protect(Guid entryId, bool protect)
    {
        _store.Change(state =>
        {
            CleanupRules.Protect(state, state.Entries.Single(entry => entry.Id == entryId), protect, _time.GetUtcNow());
            return true;
        });
    }

    internal object ProtectionForItem(Guid itemId)
    {
        var state = _store.Read();
        var entry = state.Entries.FirstOrDefault(value => value.Media.ItemId == _library.RootItemId(itemId));
        return new { Id = entry?.Id, Protected = entry?.Protected ?? false, EffectiveProtection = entry is not null && CleanupRules.IsProtected(state, entry) };
    }

    internal async Task EvaluateAsync(CancellationToken cancellationToken, IProgress<double>? progress = null)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _evaluationReady = false;
            var settingsRevision = _store.Locked(() => _settingsRevision);
            progress?.Report(0);
            cancellationToken.ThrowIfCancellationRequested();
            await VerifyPendingAsync(cancellationToken).ConfigureAwait(false);
            var inventory = _library.Inventory();
            cancellationToken.ThrowIfCancellationRequested();
            _store.Change(state =>
            {
                CleanupRules.Reconcile(state, inventory, _time.GetUtcNow());
                return true;
            });
            if (!Enabled)
            {
                progress?.Report(100);
                return;
            }

            var entries = _store.Read().Entries.Where(value => value.Present && value.Media.Kind != "Collection").ToArray();
            progress?.Report(10);
            var processed = 0;
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var states = _library.ReadStates(entry.Media);
                cancellationToken.ThrowIfCancellationRequested();
                Restore(entry, states);
                _store.Change(state =>
                {
                    var current = state.Entries.Single(value => value.Id == entry.Id);
                    foreach (var value in states)
                    {
                        var previous = state.Observations.FirstOrDefault(item => item.ItemId == value.ItemId && item.UserId == value.UserId);
                        if (previous is null)
                        {
                            state.Observations.Add(new UserObservation { ItemId = value.ItemId, UserId = value.UserId, Identities = value.Identities, Played = value.Played, Favorite = value.Favorite });
                        }

                        if (value.LastPlayed.HasValue && (!current.LastInteraction.HasValue || value.LastPlayed > current.LastInteraction) && value.LastPlayed > current.Baseline)
                        {
                            CleanupRules.Interact(state, current, value.UserId, "Playback", value.LastPlayed.Value);
                        }
                    }

                    return true;
                });
                progress?.Report(10 + (80d * ++processed / entries.Length));
            }

            cancellationToken.ThrowIfCancellationRequested();
            _store.Locked(() =>
            {
                if (_settingsRevision != settingsRevision)
                {
                    throw new InvalidOperationException("Cleanup settings changed during evaluation. Run the evaluation again.");
                }

                _store.Change(state =>
                {
                    CleanupRules.Evaluate(state, _time.GetUtcNow());
                    state.Error = null;
                    return true;
                });
                _evaluationReady = true;
                return true;
            });
            progress?.Report(100);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_store.Fault is null)
            {
                _store.Change(state =>
                {
                    state.Error = SafeError(ex);
                    return true;
                });
            }

            throw;
        }
        finally
        {
            _operations.Release();
        }
    }

    internal CleanupPlan Plan(Guid entryId, Guid adminId)
    {
        return _store.Change(state =>
        {
            var entry = state.Entries.Single(value => value.Id == entryId);
            var entries = entry.Media.Kind == "Collection"
                ? state.Entries.Where(value => entry.Media.Members.Contains(value.Media.ItemId) && value.Present).ToArray() : new[] { entry };
            if (entries.Length == 0 || CleanupRules.IsProtected(state, entry))
            {
                throw new InvalidOperationException("The selection is protected or contains no available media.");
            }

            var plan = new CleanupPlan { CreatedAt = _time.GetUtcNow(), UserId = adminId };
            foreach (var value in entries)
            {
                var target = new PlanTarget
                {
                    EntryId = value.Id, Generation = value.Generation, NominationId = value.NominationId ?? Guid.Empty,
                    Revision = value.Revision, Name = value.Media.Name, Kind = value.Media.Kind
                };
                CleanupRules.CheckDeletion(state, value, target, _time.GetUtcNow());
                if (_library.IsPlaying(CleanupRules.Scope(state, value)))
                {
                    throw new InvalidOperationException("Active playback prevents deletion.");
                }

                plan.Targets.Add(target);
            }

            state.Plans.RemoveAll(value => value.CreatedAt < _time.GetUtcNow().AddHours(-1));
            state.Plans.Add(plan);
            return plan;
        });
    }

    internal async Task ExecutePlanAsync(Guid planId, Guid adminId, CancellationToken cancellationToken)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (adminId == Guid.Empty && !_evaluationReady)
            {
                throw new InvalidOperationException("Automatic deletion requires a successful evaluation since startup and the latest settings change.");
            }

            var plan = _store.Read().Plans.Single(value => value.Id == planId && value.UserId == adminId);
            await ExecutePlanCoreAsync(plan, adminId == Guid.Empty, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operations.Release();
        }
    }

    private async Task ExecutePlanCoreAsync(CleanupPlan plan, bool automatic, CancellationToken cancellationToken)
    {
        if (plan.CreatedAt < _time.GetUtcNow().AddMinutes(-15))
        {
            throw new InvalidOperationException("The confirmation expired. Review the selection again.");
        }

        foreach (var target in plan.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DeleteTargetAsync(target, automatic, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        _store.Change(state => state.Plans.RemoveAll(value => value.Id == plan.Id));
    }

    internal async Task DeleteAutomaticallyAsync(CancellationToken cancellationToken, IProgress<double>? progress = null)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_store.Fault is not null)
            {
                throw new InvalidOperationException(_store.Fault);
            }

            var state = _store.Read();
            if (!state.Settings.Enabled || !state.Settings.AutomaticDeletion)
            {
                progress?.Report(100);
                return;
            }

            progress?.Report(0);
            if (!_evaluationReady || state.Error is not null)
            {
                throw new InvalidOperationException("Run 'Library cleanup: Evaluate titles' successfully before automatic deletion. An evaluation is required after startup, settings changes or an interrupted evaluation.");
            }

            var entries = state.Entries.Where(value => value.Present && value.Media.Kind != "Collection"
                && value.DeleteAt <= _time.GetUtcNow() && value.NominationId.HasValue && value.Error is null).ToArray();
            var processed = 0;
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = _store.Read();
                if (!current.Settings.Enabled || !current.Settings.AutomaticDeletion)
                {
                    break;
                }

                if (!_evaluationReady || current.Error is not null)
                {
                    throw new InvalidOperationException("Cleanup settings or evaluation status changed. Run 'Library cleanup: Evaluate titles' again before automatic deletion.");
                }

                var plan = Plan(entry.Id, Guid.Empty);
                await ExecutePlanCoreAsync(plan, true, cancellationToken).ConfigureAwait(false);
                var error = _store.Read().Entries.Single(value => value.Id == entry.Id).Error;
                if (error is not null)
                {
                    throw new InvalidOperationException(error);
                }

                progress?.Report(90d * ++processed / entries.Length);
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(100);
        }
        finally
        {
            _operations.Release();
        }
    }

    private async Task DeleteTargetAsync(PlanTarget target, bool automatic, CancellationToken cancellationToken)
    {
        DeletionRecord? job = null;
        try
        {
            // Refresh membership and file scope; this can invalidate the reviewed plan.
            var inventory = _library.Inventory();
            _store.Change(state =>
            {
                CleanupRules.Reconcile(state, inventory, _time.GetUtcNow());
                return true;
            });
            var state = _store.Read();
            var entry = state.Entries.Single(value => value.Id == target.EntryId);
            CleanupRules.CheckDeletion(state, entry, target, _time.GetUtcNow());
            var connection = Connection(state, entry);
            var previous = state.Deletions.LastOrDefault(value => value.EntryId == entry.Id && value.Generation == entry.Generation && value.ArrId > 0);
            if (previous is not null && previous.ArrUrl == connection.Url
                && await _arr.IsRemovedAsync(entry.Media, connection, previous.ArrId, cancellationToken).ConfigureAwait(false))
            {
                job = previous;
                await VerifyAsync(entry, connection, job, cancellationToken).ConfigureAwait(false);
                return;
            }

            var mapping = await _arr.MatchAsync(entry.Media, connection, cancellationToken).ConfigureAwait(false);
            var backup = _library.ReadStates(entry.Media).Where(value => value.Identities.Any(id => id.StartsWith("Movie:", StringComparison.Ordinal) || id.StartsWith("Episode:", StringComparison.Ordinal))).ToArray();
            if (entry.Media.Paths.Length == 0 || backup.Length == 0 || backup.Any(value => value.Identities.Length == 0)
                || backup.Any(value => backup.Any(other => value.UserId == other.UserId && value.ItemId != other.ItemId && CleanupRules.SameIdentity(value.Identities, other.Identities))))
            {
                throw new InvalidOperationException("Watched-state backup is incomplete or ambiguous. Nothing was deleted.");
            }

            _store.Change(current =>
            {
                foreach (var value in backup)
                {
                    current.Archives.RemoveAll(archive => archive.EntryId == entry.Id && archive.Generation == entry.Generation && archive.UserId == value.UserId
                        && CleanupRules.SameIdentity(archive.Identities, value.Identities));
                    current.Archives.Add(new SeenArchive
                    {
                        EntryId = entry.Id, Generation = entry.Generation, UserId = value.UserId,
                        Identities = value.Identities, Played = value.Played, CapturedAt = _time.GetUtcNow()
                    });
                }

                return true;
            });

            var latestMapping = await _arr.MatchAsync(entry.Media, connection, cancellationToken).ConfigureAwait(false);
            if (latestMapping.Id != mapping.Id || latestMapping.Path != mapping.Path)
            {
                throw new InvalidOperationException("The external media entry changed while preparing deletion.");
            }

            Task request = _store.Locked(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var finalInventory = _library.Inventory();
                _store.Change(next =>
                {
                    CleanupRules.Reconcile(next, finalInventory, _time.GetUtcNow());
                    return true;
                });
                var current = _store.Read();
                var latest = current.Entries.Single(value => value.Id == entry.Id);
                CleanupRules.CheckDeletion(current, latest, target, _time.GetUtcNow());
                if ((automatic && (!current.Settings.AutomaticDeletion || !_evaluationReady))
                    || Connection(current, latest).Url != connection.Url || Connection(current, latest).ApiKey != connection.ApiKey
                    || _library.IsPlaying(CleanupRules.Scope(current, latest)))
                {
                    throw new InvalidOperationException("Playback or connection settings changed before deletion.");
                }

                job = new DeletionRecord
                {
                    Media = CleanupStore.Clone(entry.Media),
                    EntryId = entry.Id, Generation = entry.Generation, NominationId = target.NominationId, ArrId = mapping.Id,
                    ArrUrl = connection.Url, ArrPath = mapping.Path, Paths = entry.Media.Paths, Phase = "Dispatched", At = _time.GetUtcNow()
                };
                _store.Change(next =>
                {
                    next.Deletions.Add(job);
                    return true;
                });
                return _arr.DeleteAsync(entry.Media, connection, mapping.Id, cancellationToken);
            });
            await request.ConfigureAwait(false);
            await VerifyAsync(entry, connection, job!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or InvalidOperationException or System.Text.Json.JsonException or OperationCanceledException)
        {
            if (_store.Fault is not null || (job is null && ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                throw;
            }

            _store.Change(state =>
            {
                var entry = state.Entries.Single(value => value.Id == target.EntryId);
                entry.Error = SafeError(ex);
                if (job is not null)
                {
                    var record = state.Deletions.Single(value => value.Id == job.Id);
                    record.Phase = "Failed";
                    record.Error = entry.Error;
                }

                return true;
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task VerifyAsync(CleanupEntry entry, ArrConnection connection, DeletionRecord job, CancellationToken cancellationToken)
    {
        if (!await _arr.IsRemovedAsync(job.Media, connection, job.ArrId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The Radarr/Sonarr entry still exists. Deletion is not confirmed.");
        }

        if (!_library.FilesAbsent(job.Paths))
        {
            throw new InvalidOperationException("The original media files still exist. Check Arr deletion logs, permissions and recycle-bin configuration.");
        }

        await _library.RefreshAsync(cancellationToken).ConfigureAwait(false);
        if (_library.Inventory().Any(value => value.ItemId == job.Media.ItemId))
        {
            throw new InvalidOperationException("The files are gone, but Jellyfin's library scan has not removed the item yet.");
        }

        _store.Change(state =>
        {
            var record = state.Deletions.Single(value => value.Id == job.Id);
            record.Phase = "Deleted";
            record.FinishedAt = _time.GetUtcNow();
            record.Error = null;
            var current = state.Entries.Single(value => value.Id == entry.Id);
            if (current.Generation == job.Generation)
            {
                current.Present = false;
                current.Error = null;
                CleanupRules.Cancel(current);
            }

            return true;
        });
    }

    private void Restore(CleanupEntry entry, IReadOnlyList<CleanupUserState> currentStates)
    {
        foreach (var value in currentStates)
        {
            _store.Locked(() =>
            {
                var state = _store.Read();
                var archives = state.Archives.Where(archive => archive.EntryId == entry.Id && archive.Generation != entry.Generation && archive.UserId == value.UserId
                    && state.Deletions.Any(job => job.EntryId == archive.EntryId && job.Generation == archive.Generation && job.Phase == "Deleted")
                    && CleanupRules.SameIdentity(archive.Identities, value.Identities)).OrderByDescending(archive => archive.CapturedAt).ToArray();
                var archive = archives.FirstOrDefault();
                if (archive is null || archive.RestoredItemIds.Contains(value.ItemId))
                {
                    return false;
                }

                var newerDecision = state.Observations.Any(observation => observation.UserId == value.UserId && observation.DecisionAt >= archive.CapturedAt
                    && CleanupRules.SameIdentity(observation.Identities, value.Identities));
                var ambiguous = currentStates.Any(other => other.UserId == value.UserId && other.ItemId != value.ItemId && CleanupRules.SameIdentity(other.Identities, value.Identities));
                if (ambiguous || entry.Media.Problem is not null)
                {
                    _store.Change(next =>
                    {
                        next.Archives.First(item => item.EntryId == archive.EntryId && item.CapturedAt == archive.CapturedAt && item.UserId == archive.UserId
                            && CleanupRules.SameIdentity(item.Identities, archive.Identities)).Error = "Ambiguous restore identity. No state was transferred.";
                        return true;
                    });
                    return false;
                }

                if (archive.Played && !value.Played && !newerDecision)
                {
                    _library.RestorePlayed(value.UserId, value.ItemId, true);
                    value.Played = true;
                }

                _store.Change(next =>
                {
                    var saved = next.Archives.First(item => item.EntryId == archive.EntryId && item.CapturedAt == archive.CapturedAt && item.UserId == archive.UserId
                        && CleanupRules.SameIdentity(item.Identities, archive.Identities));
                    saved.RestoredItemIds.Add(value.ItemId);
                    saved.Error = null;
                    return true;
                });
                return true;
            });
        }
    }

    internal void Observe(Guid rootItemId, Guid itemId, Guid userId, string[] identities, bool played, bool favorite, UserDataSaveReason reason)
    {
        if (!Enabled || reason == UserDataSaveReason.Import)
        {
            return;
        }

        _store.Change(state =>
        {
            var now = _time.GetUtcNow();
            var previous = state.Observations.FirstOrDefault(value => value.ItemId == itemId && value.UserId == userId);
            var playback = reason is UserDataSaveReason.PlaybackStart or UserDataSaveReason.PlaybackProgress or UserDataSaveReason.PlaybackFinished;
            var manual = reason == UserDataSaveReason.TogglePlayed
                || (reason == UserDataSaveReason.UpdateUserData && (previous is null || previous.Played != played));
            var addedFavorite = favorite && (previous is null || !previous.Favorite);
            if (previous is null)
            {
                previous = new UserObservation { ItemId = itemId, UserId = userId, Identities = identities };
                state.Observations.Add(previous);
            }

            previous.Played = played;
            previous.Favorite = favorite;
            if (manual)
            {
                previous.DecisionAt = now;
            }

            var entry = state.Entries.FirstOrDefault(value => value.Media.ItemId == rootItemId);
            if (entry is not null && (playback || manual || addedFavorite))
            {
                CleanupRules.Interact(state, entry, userId, playback ? "Playback" : manual ? "WatchedStatus" : "FavoriteAdded", now);
            }

            return true;
        });
    }

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs args)
    {
        try
        {
            var root = args.Item is Episode episode ? episode.SeriesId : args.Item is Season season ? season.SeriesId : args.Item.Id;
            Observe(root, args.Item.Id, args.UserId, CleanupLibrary.Identities(args.Item), args.UserData.Played, args.UserData.IsFavorite, args.SaveReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WatchCircle cleanup could not record an interaction");
            RecoverLostEvent();
        }
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs args)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            var rootId = args.Item is Episode episode ? episode.SeriesId : args.Item.Id;
            _store.Change(state =>
            {
                var entry = state.Entries.FirstOrDefault(value => value.Media.ItemId == rootId);
                if (entry is not null)
                {
                    entry.Baseline = _time.GetUtcNow();
                    CleanupRules.Cancel(entry);
                }

                state.LastEvaluation = null;
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WatchCircle cleanup could not record newly added media");
            RecoverLostEvent();
        }
    }

    private void RecoverLostEvent()
    {
        if (_store.Fault is not null)
        {
            return;
        }

        _store.Change(state =>
        {
            foreach (var entry in state.Entries)
            {
                entry.Baseline = _time.GetUtcNow();
                CleanupRules.Cancel(entry);
            }

            state.Error = "An interaction could not be recorded. All inactivity periods were restarted conservatively.";
            return true;
        });
    }

    private async Task VerifyPendingAsync(CancellationToken cancellationToken)
    {
        var snapshot = _store.Read();
        foreach (var job in snapshot.Deletions.Where(value => value.Phase != "Deleted" && value.ArrId > 0))
        {
            var entry = snapshot.Entries.Single(value => value.Id == job.EntryId);
            var connection = Connection(snapshot, entry);
            try
            {
                if (connection.Url != job.ArrUrl)
                {
                    throw new InvalidOperationException("The original service URL changed. Restore it to verify this deletion.");
                }

                await VerifyAsync(entry, connection, job, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _store.Change(state =>
                {
                    var record = state.Deletions.Single(value => value.Id == job.Id);
                    record.Phase = "Failed";
                    record.Error = SafeError(ex);
                    var current = state.Entries.Single(value => value.Id == entry.Id);
                    if (current.Generation == job.Generation)
                    {
                        current.Error = record.Error;
                    }

                    return true;
                });
            }
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_userData is not null)
        {
            _userData.UserDataSaved += OnUserDataSaved;
        }

        if (_jellyfin is not null)
        {
            _jellyfin.ItemAdded += OnItemAdded;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Unsubscribe();
        return Task.CompletedTask;
    }

    private void Unsubscribe()
    {
        if (_userData is not null)
        {
            _userData.UserDataSaved -= OnUserDataSaved;
        }

        if (_jellyfin is not null)
        {
            _jellyfin.ItemAdded -= OnItemAdded;
        }
    }

    private static ArrConnection Connection(CleanupState state, CleanupEntry entry) => entry.Media.Kind == "Series" ? state.Settings.Sonarr : state.Settings.Radarr;

    /// <inheritdoc />
    public void Dispose()
    {
        Unsubscribe();
        _operations.Dispose();
    }

    internal static string SafeError(Exception exception) => exception is HttpRequestException or TaskCanceledException
        ? "Radarr/Sonarr is unreachable or rejected the request. Check the connection and API key."
        : exception is System.Text.Json.JsonException ? "The external service returned an invalid response."
        : exception is IOException or UnauthorizedAccessException ? "Required state backup, filesystem access or deletion verification failed. Check storage and the deletion log."
        : exception.Message;
}
