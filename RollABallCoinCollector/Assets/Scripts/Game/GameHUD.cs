using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollABallCoinCollector
{
    /// <summary>Refreshes the HUD and routes menu buttons to the game manager.</summary>
    public sealed class GameHUD : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDescriptionText;
        [SerializeField] private GameObject gameplayHud;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button resultMainMenuButton;

        private bool listenersRegistered;

        public void Configure(
            GameManager manager,
            TMP_Text coins,
            TMP_Text lives,
            TMP_Text resultTitle,
            TMP_Text resultDescription,
            GameObject hudRoot,
            GameObject menuRoot,
            GameObject resultsRoot,
            Button play,
            Button restart,
            Button menu,
            Button resultsMenu)
        {
            gameManager = manager;
            scoreText = coins;
            livesText = lives;
            resultTitleText = resultTitle;
            resultDescriptionText = resultDescription;
            gameplayHud = hudRoot;
            mainMenuPanel = menuRoot;
            resultPanel = resultsRoot;
            playButton = play;
            restartButton = restart;
            mainMenuButton = menu;
            resultMainMenuButton = resultsMenu;
            RegisterListeners();
        }

        private void Awake()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;
            RegisterListeners();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
        }

        public void SetScore(int collected, int total)
        {
            if (scoreText != null)
                scoreText.text = $"COINS   {collected:00} / {total:00}";
        }

        public void SetLives(int lives)
        {
            if (livesText != null)
                livesText.text = $"LIVES   {lives}";
        }

        public void ShowMainMenu()
        {
            SetActive(gameplayHud, false);
            SetActive(resultPanel, false);
            SetActive(mainMenuPanel, true);
        }

        public void ShowGameplay()
        {
            SetActive(mainMenuPanel, false);
            SetActive(resultPanel, false);
            SetActive(gameplayHud, true);
        }

        public void ShowResult(bool won, int score, int total)
        {
            SetActive(mainMenuPanel, false);
            SetActive(gameplayHud, true);
            SetActive(resultPanel, true);

            if (resultTitleText != null)
                resultTitleText.text = won ? "YOU WIN!" : "GAME OVER";
            if (resultDescriptionText != null)
                resultDescriptionText.text = won
                    ? $"Every coin collected!\nFinal score: {score} / {total}"
                    : $"You collected {score} of {total} coins.\nGive it another roll!";
        }

        private void RegisterListeners()
        {
            // Configure() can run after Awake() when the Editor scene builder wires
            // the runtime references. Remove any earlier subscriptions first so
            // the buttons never end up with duplicate callbacks.
            UnregisterListeners();

            if (playButton != null)
                playButton.onClick.AddListener(HandlePlay);
            if (restartButton != null)
                restartButton.onClick.AddListener(HandleRestart);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(HandleMainMenu);
            if (resultMainMenuButton != null)
                resultMainMenuButton.onClick.AddListener(HandleMainMenu);

            listenersRegistered = true;
        }

        private void UnregisterListeners()
        {
            if (!listenersRegistered)
                return;

            playButton?.onClick.RemoveListener(HandlePlay);
            restartButton?.onClick.RemoveListener(HandleRestart);
            mainMenuButton?.onClick.RemoveListener(HandleMainMenu);
            resultMainMenuButton?.onClick.RemoveListener(HandleMainMenu);
            listenersRegistered = false;
        }

        private void HandlePlay()
        {
            gameManager?.StartGame();
        }

        private void HandleRestart()
        {
            gameManager?.RestartGame();
        }

        private void HandleMainMenu()
        {
            gameManager?.ReturnToMainMenu();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
