using UnityEngine;

namespace RollABallCoinCollector
{
    /// <summary>Costs one life when the player collides with this obstacle.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class DangerousObstacle : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        public void Configure(GameManager manager)
        {
            gameManager = manager;
        }

        private void Awake()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (gameManager == null || collision.collider == null || !collision.collider.CompareTag("Player"))
                return;

            gameManager.TryLoseLife();
        }
    }
}
