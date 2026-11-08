using UnityEngine;

namespace RollABallCoinCollector
{
    /// <summary>Respawns the ball and costs a life if it somehow leaves the arena.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FallGuard : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Vector3 spawnPosition;
        [SerializeField] private Vector2 xBounds = new Vector2(-9.1f, 9.1f);
        [SerializeField] private Vector2 zBounds = new Vector2(-12.1f, 12.1f);
        [SerializeField] private float fallHeight = -3f;

        private Rigidbody playerBody;

        public void Configure(GameManager manager, Vector3 spawn, Vector2 xRange, Vector2 zRange)
        {
            gameManager = manager;
            spawnPosition = spawn;
            xBounds = xRange;
            zBounds = zRange;
        }

        private void Awake()
        {
            playerBody = GetComponent<Rigidbody>();
            if (gameManager == null)
                gameManager = GameManager.Instance;
        }

        private void FixedUpdate()
        {
            if (gameManager == null || !gameManager.IsPlaying)
                return;

            bool outsideArena = transform.position.x < xBounds.x || transform.position.x > xBounds.y ||
                                transform.position.z < zBounds.x || transform.position.z > zBounds.y ||
                                transform.position.y < fallHeight;
            if (!outsideArena)
                return;

            gameManager.TryLoseLife();
            playerBody.position = spawnPosition;
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }
    }
}
