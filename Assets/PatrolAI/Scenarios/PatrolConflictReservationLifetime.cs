using System;
using UnityEngine;

namespace PatrolAI.Scenarios
{
    // Survives runner disable/destruction while an uncancellable inherited task/workflow settles.
    public sealed class PatrolConflictReservationLifetime : MonoBehaviour
    {
        private PatrolConflictRunReservation reservation;
        private Func<bool> settled;
        private Action released;
        private bool terminal;

        public static PatrolConflictReservationLifetime Hold(PatrolConflictRunReservation reservation,
            Func<bool> settled, Action released)
        {
            var owner = new GameObject("PatrolAI D reservation lifetime");
            DontDestroyOnLoad(owner);
            var lifetime = owner.AddComponent<PatrolConflictReservationLifetime>();
            lifetime.reservation = reservation;
            lifetime.settled = settled;
            lifetime.released = released;
            return lifetime;
        }

        public void Complete() { terminal = true; Poll(); }
        private void Update() { Poll(); }
        public void Poll()
        {
            if (reservation == null || !terminal || !settled()) return;
            Release();
        }
        private void OnApplicationQuit() { Release(); }
        private void Release()
        {
            if (reservation == null) return;
            try { reservation.Dispose(); Debug.Log("[PATROLAI-D-CONFLICT] RESERVATION_RELEASED"); }
            catch (Exception exception) {
                Debug.LogWarning("[PATROLAI-D-CONFLICT] RESERVATION_RELEASE_FAILED type="
                    + exception.GetType().Name);
            }
            finally { reservation = null; released?.Invoke(); Destroy(gameObject); }
        }
    }
}
