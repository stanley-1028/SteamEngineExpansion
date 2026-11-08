using System;
using System.Collections.Generic;
using SEE.Core.Save;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;

namespace SEE.Integration.Save
{
    /// <summary>
    /// Recomputes the <c>compat</c> diagnostic snapshot written into the world save node
    /// (requirements §6.3/§10.9, REQ-COMPAT-13/14). The snapshot is derived from the CURRENT
    /// detection result and adapter states, never from the previously saved arrays, so removing or
    /// adding an optional mod rewrites the block on the next save.
    ///
    /// Only module short names and adapter ids/failure keys may appear here; external type names,
    /// component ids or network ids never do (REQ-SAVE-09).
    /// </summary>
    public static class CompatSnapshotBuilder
    {
        /// <summary>Builds a compat snapshot from the current statuses and adapters.</summary>
        /// <param name="statuses">Detected statuses, in the frozen order; null is treated as empty.</param>
        /// <param name="adapters">Adapters; null is treated as empty.</param>
        /// <returns>A new snapshot with de-duplicated, stably ordered arrays.</returns>
        public static SeeCompatSnapshot Build(
            IReadOnlyList<ExternalModStatus>? statuses,
            IReadOnlyList<IExternalAdapter>? adapters)
        {
            var snapshot = new SeeCompatSnapshot();
            if (statuses != null)
            {
                for (int i = 0; i < statuses.Count; i++)
                {
                    ExternalModStatus status = statuses[i];
                    if (status.Detected && !string.IsNullOrEmpty(status.ModId))
                    {
                        AddUnique(snapshot.Detected, status.ModId);
                    }
                }
            }

            if (adapters != null)
            {
                for (int i = 0; i < adapters.Count; i++)
                {
                    IExternalAdapter adapter = adapters[i];
                    if (adapter == null)
                    {
                        continue;
                    }

                    if (adapter.State == AdapterState.Enabled && !string.IsNullOrEmpty(adapter.AdapterId))
                    {
                        AddUnique(snapshot.Enabled, adapter.AdapterId);
                    }
                    else if (adapter.State == AdapterState.Failed)
                    {
                        string key = string.IsNullOrEmpty(adapter.FailureKey) ? adapter.ModId + "-failed" : adapter.FailureKey!;
                        AddUnique(snapshot.Failed, key);
                    }
                }
            }

            return snapshot;
        }

        /// <summary>True when two snapshots carry the same three arrays (order-insensitive per array).</summary>
        /// <param name="left">First snapshot.</param>
        /// <param name="right">Second snapshot.</param>
        /// <returns>True when the content is equal.</returns>
        public static bool ContentEquals(SeeCompatSnapshot? left, SeeCompatSnapshot? right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return SetEquals(left.Detected, right.Detected)
                && SetEquals(left.Enabled, right.Enabled)
                && SetEquals(left.Failed, right.Failed);
        }

        private static void AddUnique(List<string> target, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!target.Contains(value))
            {
                target.Add(value);
            }
        }

        private static bool SetEquals(List<string> left, List<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!right.Contains(left[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
