using UnityEngine;

namespace RollABallCoinCollector
{
    /// <summary>Rotates a coin and awards one point when the player enters its trigger.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class CoinPickup : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private GameManager gameManager;
        private bool collected;

        public void Configure(GameManager manager)
        {
            gameManager = manager;
        }

        private void Awake()
        {
            Collider coinCollider = GetComponent<Collider>();
            if (coinCollider != null)
                coinCollider.isTrigger = true;

            if (gameManager == null)
                gameManager = GameManager.Instance;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag("Player") || gameManager == null)
                return;

            if (!gameManager.TryCollectCoin())
                return;

            collected = true;
            Destroy(gameObject);
        }
    }
}
