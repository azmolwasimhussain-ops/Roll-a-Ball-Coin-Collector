using UnityEngine;
using UnityEngine.SceneManagement;

namespace RollABallCoinCollector
{
    /// <summary>Owns the coin count, lives, game state, and scene transitions.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        private enum SessionState
        {
            MainMenu,
            Playing,
            Won,
            GameOver
        }

        public static GameManager Instance { get; private set; }
        public bool IsPlaying => state == SessionState.Playing;

        [SerializeField] private GameHUD gameHud;
        [SerializeField] private GameAudio gameAudio;
        [SerializeField] private int startingLives = 3;
        [SerializeField] private float damageCooldown = 1.1f;

        private SessionState state;
        private int score;
        private int lives;
        private int totalCoins;
        private float nextDamageTime;

        public void Configure(GameHUD hud, GameAudio audioController)
        {
            gameHud = hud;
            gameAudio = audioController;
            startingLives = 3;
        }

        private void Awake()
        {
            Instance = this;
            if (gameHud == null)
                gameHud = FindAnyObjectByType<GameHUD>();
            if (gameAudio == null)
                gameAudio = FindAnyObjectByType<GameAudio>();
        }

        private void Start()
        {
            Time.timeScale = 0f;
            state = SessionState.MainMenu;
            score = 0;
            lives = startingLives;
            totalCoins = FindObjectsByType<CoinPickup>().Length;
            nextDamageTime = 0f;

            if (gameHud != null)
            {
                gameHud.SetScore(score, totalCoins);
                gameHud.SetLives(lives);
                gameHud.ShowMainMenu();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Time.timeScale = 1f;
        }

        public void StartGame()
        {
            if (state != SessionState.MainMenu)
                return;

            state = SessionState.Playing;
            Time.timeScale = 1f;
            if (gameHud != null)
                gameHud.ShowGameplay();
        }

        public bool TryCollectCoin()
        {
            if (state != SessionState.Playing)
                return false;

            score++;
            gameAudio?.PlayCoin();
            gameHud?.SetScore(score, totalCoins);

            if (score >= totalCoins && totalCoins > 0)
                FinishGame(true);

            return true;
        }

        public bool TryLoseLife()
        {
            if (state != SessionState.Playing || Time.unscaledTime < nextDamageTime)
                return false;

            nextDamageTime = Time.unscaledTime + damageCooldown;
            lives = Mathf.Max(0, lives - 1);
            gameAudio?.PlayObstacleHit();
            gameHud?.SetLives(lives);

            if (lives == 0)
                FinishGame(false);

            return true;
        }

        public void RestartGame()
        {
            LoadCurrentScene();
        }

        public void ReturnToMainMenu()
        {
            LoadCurrentScene();
        }

        private void FinishGame(bool won)
        {
            state = won ? SessionState.Won : SessionState.GameOver;
            Time.timeScale = 0f;
            gameHud?.ShowResult(won, score, totalCoins);
        }

        private static void LoadCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else
                SceneManager.LoadScene(activeScene.name);
        }
    }
}
