using UnityEngine;

namespace RollABallCoinCollector
{
    /// <summary>Soft third-person camera that keeps its target framed while it rolls.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -9f);
        [SerializeField] private float positionSmoothTime = 0.22f;
        [SerializeField] private float lookHeight = 1f;

        private Vector3 positionVelocity;

        public void Configure(Transform followTarget, Vector3 followOffset)
        {
            target = followTarget;
            offset = followOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            Vector3 lookPoint = target.position + Vector3.up * lookHeight;
            Vector3 lookDirection = lookPoint - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }
}
