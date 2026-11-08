using UnityEngine;

namespace RollABallCoinCollector
{
    /// <summary>Moves a kinematic obstacle smoothly between two points.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovingObstacle : MonoBehaviour
    {
        [SerializeField] private Vector3 travelAxis = Vector3.right;
        [SerializeField] private float travelDistance = 3f;
        [SerializeField] private float movementSpeed = 1.4f;

        private Rigidbody obstacleBody;
        private Vector3 startPosition;

        public void Configure(Vector3 axis, float distance, float speed)
        {
            travelAxis = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.right;
            travelDistance = Mathf.Max(0f, distance);
            movementSpeed = Mathf.Max(0f, speed);
        }

        private void Awake()
        {
            obstacleBody = GetComponent<Rigidbody>();
            startPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (obstacleBody == null)
                return;

            float offset = Mathf.Sin(Time.time * movementSpeed) * travelDistance;
            obstacleBody.MovePosition(startPosition + travelAxis * offset);
        }
    }
}
