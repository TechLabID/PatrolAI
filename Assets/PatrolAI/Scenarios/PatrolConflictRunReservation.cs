using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PatrolAI.Scenarios
{
    /// <summary>Atomic, process-owned D lease. The OS lock protects acquisition and the entire run.</summary>
    public sealed class PatrolConflictRunReservation : IDisposable
    {
        private FileStream ownershipLock;
        private readonly string markerPath;
        private readonly string metadata;
        public string MarkerPath => markerPath;
        public bool RecoveredStaleMarker { get; private set; }

        private PatrolConflictRunReservation(FileStream ownershipLock, string markerPath, string metadata)
        { this.ownershipLock = ownershipLock; this.markerPath = markerPath; this.metadata = metadata; }

        public static PatrolConflictRunReservation Acquire(string directory)
        {
            Directory.CreateDirectory(directory);
            string marker = Path.Combine(directory, "demo_started.json");
            // Never unlink this sidecar: deleting a lock pathname can admit two simultaneous owners.
            var gate = new FileStream(Path.Combine(directory, "demo_run.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            bool ownsMarker = false;
            string pendingMarker = null;
            try {
                bool recovered = false;
                if (File.Exists(marker)) {
                    if (new FileInfo(marker).Length > 4096 || !CanRecover(File.ReadAllText(marker)))
                        throw new IOException("D reservation owner is active or cannot be verified dead.");
                    File.Delete(marker);
                    recovered = true;
                }
                string data;
                using (var process = Process.GetCurrentProcess()) {
                    data = "{\"schemaVersion\":1,\"reservedRequests\":2,\"processId\":" + process.Id
                        + ",\"processStartUtcTicks\":" + process.StartTime.ToUniversalTime().Ticks
                        + ",\"runId\":\"" + Guid.NewGuid().ToString("N") + "\"}";
                }
                pendingMarker = Path.Combine(directory, "demo_reservation_" + Guid.NewGuid().ToString("N") + ".tmp");
                using (var stream = new FileStream(pendingMarker, FileMode.CreateNew, FileAccess.Write)) {
                    byte[] bytes = Encoding.UTF8.GetBytes(data);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                File.Move(pendingMarker, marker); // Atomic same-directory publication, never a partial owner record.
                pendingMarker = null;
                ownsMarker = true;
                return new PatrolConflictRunReservation(gate, marker, data) { RecoveredStaleMarker = recovered };
            }
            catch {
                try {
                    if (ownsMarker) File.Delete(marker);
                    if (pendingMarker != null) File.Delete(pendingMarker);
                }
                finally { gate.Dispose(); }
                throw;
            }
        }

        private static bool CanRecover(string data)
        {
            var match = Regex.Match(data, @"^{""schemaVersion"":1,""reservedRequests"":2,""processId"":([0-9]+),""processStartUtcTicks"":([0-9]+),""runId"":""[a-f0-9]{32}""}$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int pid)
                && long.TryParse(match.Groups[2].Value, out long started)) {
                try {
                    using (var process = Process.GetProcessById(pid))
                        return process.HasExited || process.StartTime.ToUniversalTime().Ticks != started;
                }
                catch (ArgumentException) { return true; } // PID no longer exists.
                catch (InvalidOperationException) { return true; } // Exited during inspection.
                catch (Exception) { return false; } // Inaccessible ownership is not proof of death.
            }
            // Pre-D2 markers contain only a request count. Protect a possible legacy Player/Editor
            // with the same process name; recover only the exact known legacy format, under the lock.
            if (!Regex.IsMatch(data.Trim(), @"^{s*""reservedRequests""s*:s*2s*}$")) return false;
            try {
                using (var current = Process.GetCurrentProcess()) {
                    foreach (var process in Process.GetProcessesByName(current.ProcessName))
                        using (process) { if (process.Id != current.Id && !process.HasExited) return false; }
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        public void Dispose()
        {
            if (ownershipLock == null) return;
            try {
                if (File.Exists(markerPath)) {
                    if (File.ReadAllText(markerPath) != metadata)
                        throw new IOException("D reservation metadata changed; refusing to delete another owner.");
                    File.Delete(markerPath);
                }
            }
            finally { ownershipLock.Dispose(); ownershipLock = null; }
        }
    }
}
