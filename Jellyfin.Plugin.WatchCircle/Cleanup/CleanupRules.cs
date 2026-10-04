using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Deterministic cleanup decisions, independent of external side effects.</summary>
internal static class CleanupRules
{
    internal static bool SameIdentity(IEnumerable<string> left, IEnumerable<string> right)
    {
        var a = left.ToArray();
        var b = right.ToArray();
        if (!a.Intersect(b, StringComparer.Ordinal).Any())
        {
            return false;
        }

        // A matching provider must not hide a contradictory second provider.
        return !a.Any(x => b.Any(y => IdentityNamespace(x) == IdentityNamespace(y) && x != y));
    }

    private static string IdentityNamespace(string identity)
        => identity.StartsWith("Episode:Series:", StringComparison.Ordinal)
            ? string.Join(':', identity.Split(':').Take(3)) : identity[..identity.LastIndexOf(':')];

    internal static bool IsProtected(CleanupState state, CleanupEntry entry)
        => entry.Protected || state.Entries.Any(collection => collection.Protected && collection.Media.Kind == "Collection"
            && collection.Media.Members.Contains(entry.Media.ItemId));

    internal static bool IsSelected(CleanupState state, CleanupEntry entry)
        => entry.Media.LibraryIds.Intersect(state.Settings.LibraryIds).Any();

    internal static IReadOnlyList<CleanupEntry> Scope(CleanupState state, CleanupEntry entry)
    {
        var collections = state.Entries.Where(value => value.Media.Kind == "Collection"
            && (value.Id == entry.Id || value.Media.Members.Contains(entry.Media.ItemId))).ToArray();
        var itemIds = collections.SelectMany(value => value.Media.Members).Append(entry.Media.ItemId).ToHashSet();
        return state.Entries.Where(value => itemIds.Contains(value.Media.ItemId) || collections.Contains(value)).ToArray();
    }

    internal static void Cancel(CleanupEntry entry)
    {
        entry.NominationId = null;
        entry.NominatedAt = null;
        entry.DeleteAt = null;
        entry.Revision++;
    }

    internal static void Interact(CleanupState state, CleanupEntry entry, Guid userId, string kind, DateTimeOffset now)
    {
        foreach (var affected in Scope(state, entry))
        {
            // A later scan of another collection member must not move activity backwards.
            if (affected.LastInteraction > now)
            {
                continue;
            }

            affected.LastInteraction = now;
            affected.LastUserId = userId;
            affected.LastKind = kind;
            Cancel(affected);
            foreach (var reply in state.Replies.Where(reply => reply.EntryId == affected.Id && reply.UserId == userId))
            {
                reply.Active = false;
            }
        }
    }

    internal static bool NeedsReply(CleanupState state, CleanupEntry entry, Guid userId)
        => entry.NominationId.HasValue && entry.Present && !IsProtected(state, entry)
            && !state.Deletions.Any(job => job.EntryId == entry.Id && job.Generation == entry.Generation && job.Phase != "Deleted")
            && !state.Replies.Any(reply => reply.EntryId == entry.Id && reply.UserId == userId && reply.Answer == "Indifferent" && reply.Active);

    internal static void Reply(CleanupState state, CleanupEntry entry, Guid nominationId, Guid userId, string userName, string answer, DateTimeOffset now)
    {
        if (!state.Settings.Enabled || !entry.Present || !entry.NominationId.HasValue || entry.NominationId != nominationId || IsProtected(state, entry)
            || state.Deletions.Any(value => value.EntryId == entry.Id && value.Generation == entry.Generation && value.Phase != "Deleted"))
        {
            throw new InvalidOperationException("This nomination is no longer open. Refresh the list.");
        }

        if (answer is not ("Keep" or "Indifferent"))
        {
            throw new ArgumentException("Choose Keep or Indifferent.", nameof(answer));
        }

        foreach (var previous in state.Replies.Where(reply => reply.EntryId == entry.Id && reply.UserId == userId))
        {
            previous.Active = false;
        }

        state.Replies.Add(new CleanupReply
        {
            EntryId = entry.Id, NominationId = nominationId, UserId = userId, UserName = userName,
            Answer = answer, At = now, Active = answer == "Indifferent"
        });
        if (answer == "Keep")
        {
            Interact(state, entry, userId, "Keep", now);
        }
    }

    internal static void Protect(CleanupState state, CleanupEntry entry, bool protect, DateTimeOffset now)
    {
        entry.Protected = protect;
        var affected = entry.Media.Kind == "Collection"
            ? state.Entries.Where(value => entry.Media.Members.Contains(value.Media.ItemId)).Append(entry)
            : new[] { entry };
        foreach (var value in affected)
        {
            Cancel(value);
            // Removing protection cannot continue a previously due deletion.
            value.Baseline = now;
        }
    }

    internal static void Evaluate(CleanupState state, DateTimeOffset now)
    {
        if (!state.Settings.Enabled)
        {
            return;
        }

        foreach (var entry in state.Entries.Where(entry => entry.Media.Kind != "Collection"))
        {
            if (!entry.Present || !IsSelected(state, entry) || IsProtected(state, entry))
            {
                if (entry.NominationId.HasValue)
                {
                    Cancel(entry);
                }

                continue;
            }

            var last = entry.LastInteraction > entry.Baseline ? entry.LastInteraction.Value : entry.Baseline;
            if (last.AddMonths(state.Settings.InactivityMonths) > now || entry.Media.Problem is not null)
            {
                continue;
            }

            if (!entry.NominationId.HasValue)
            {
                entry.NominationId = Guid.NewGuid();
                entry.NominatedAt = now;
                entry.DeleteAt = now.AddDays(state.Settings.WarningDays);
                entry.Revision++;
            }
        }

        state.LastEvaluation = now;
    }

    internal static void CheckDeletion(CleanupState state, CleanupEntry entry, PlanTarget target, DateTimeOffset now)
    {
        var last = entry.LastInteraction > entry.Baseline ? entry.LastInteraction.Value : entry.Baseline;
        if (!state.Settings.Enabled || state.Error is not null || !entry.Present || !IsSelected(state, entry) || IsProtected(state, entry)
            || entry.Generation != target.Generation || entry.NominationId != target.NominationId || entry.Revision != target.Revision
            || !entry.DeleteAt.HasValue || entry.DeleteAt > now || last.AddMonths(state.Settings.InactivityMonths) > now
            || entry.Media.Problem is not null)
        {
            throw new InvalidOperationException("Deletion is no longer eligible. Protection, activity, scope or deadlines changed.");
        }
    }

    internal static void Reconcile(CleanupState state, IReadOnlyList<CleanupMedia> inventory, DateTimeOffset now)
    {
        var found = new HashSet<Guid>();
        foreach (var media in inventory)
        {
            var exact = state.Entries.SingleOrDefault(value => value.Media.ItemId == media.ItemId);
            var matches = state.Entries.Where(value => SameIdentity(value.Media.Identities, media.Identities)).ToArray();
            var duplicates = inventory.Count(value => SameIdentity(value.Identities, media.Identities));
            if (matches.Length > 1 || duplicates > 1)
            {
                media.Problem = "Ambiguous content identity. Resolve duplicate titles before cleanup.";
            }

            var entry = exact ?? (matches.Length == 1 && duplicates == 1 ? matches[0] : null);
            if (entry is null)
            {
                entry = new CleanupEntry { Media = media, Baseline = now };
                state.Entries.Add(entry);
            }
            else if (!entry.Present || entry.Media.ItemId != media.ItemId || !entry.Media.Paths.SequenceEqual(media.Paths, StringComparer.Ordinal)
                || (entry.Media.Identities.Length > 0 && !SameIdentity(entry.Media.Identities, media.Identities)))
            {
                entry.Baseline = now;
                entry.Generation = Guid.NewGuid();
                Cancel(entry);
            }

            entry.Media = media;
            entry.Present = true;
            found.Add(entry.Id);
        }

        foreach (var entry in state.Entries.Where(value => value.Present && !found.Contains(value.Id)))
        {
            entry.Present = false;
            Cancel(entry);
        }
    }
}
