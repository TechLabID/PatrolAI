using System.Collections;
using UnityEngine;

namespace VisionGuard.Phase3
{
    [DisallowMultipleComponent]
    public sealed class VGMobileInspectorMovement : MonoBehaviour
    {
        [SerializeField] private Vector3 homePosition = new Vector3(0f, 2.2f, -0.6f);
        [SerializeField] private Vector3 homeEuler = new Vector3(0f, -90f, 0f);
        [SerializeField] private Vector3 inspectionPosition = new Vector3(-1f, 2.2f, 5.27f);
        [SerializeField] private Vector3 inspectionEuler = new Vector3(0f, -90f, 0f);
        [SerializeField, Min(0.25f)] private float movementDuration = 2.5f;

        public bool IsMoving { get; private set; }
        public bool IsAtInspectionPose =>
            Vector3.Distance(transform.position, inspectionPosition) < 0.001f &&
            Quaternion.Angle(transform.rotation, Quaternion.Euler(inspectionEuler)) < 0.1f;

        private void Awake()
        {
            transform.SetPositionAndRotation(homePosition, Quaternion.Euler(homeEuler));
        }

        public IEnumerator MoveHomeIfNeeded()
        {
            if (Vector3.Distance(transform.position, homePosition) > 0.001f ||
                Quaternion.Angle(transform.rotation, Quaternion.Euler(homeEuler)) > 0.1f)
                yield return Move(homePosition, Quaternion.Euler(homeEuler));
        }

        public IEnumerator MoveToInspectionPose()
        {
            yield return Move(inspectionPosition, Quaternion.Euler(inspectionEuler));
        }

        private IEnumerator Move(Vector3 destination, Quaternion destinationRotation)
        {
            IsMoving = true;
            Vector3 start = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            while (elapsed < movementDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / movementDuration);
                t = t * t * (3f - 2f * t);
                transform.SetPositionAndRotation(Vector3.Lerp(start, destination, t),
                    Quaternion.Slerp(startRotation, destinationRotation, t));
                yield return null;
            }
            transform.SetPositionAndRotation(destination, destinationRotation);
            IsMoving = false;
        }
    }
}
