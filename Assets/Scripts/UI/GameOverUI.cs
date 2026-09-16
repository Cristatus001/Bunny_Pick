using BunnyPick.Data;
using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    public class GameOverUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Text titleText;
        public Text starsText;
        public Text detailText;
        public Text factText;
        public Button retryButton;
        public Button homeButton;

        void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
            GameManager.OnRunEnded += HandleRunEnded;
        }

        void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
            GameManager.OnRunEnded -= HandleRunEnded;
        }

        void Start()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(() =>
                {
                    GameManager.Instance.PlayButtonClick();
                    GameManager.Instance.StartGame();
                });
            }
            if (homeButton != null)
            {
                homeButton.onClick.AddListener(() =>
                {
                    GameManager.Instance.PlayButtonClick();
                    GameManager.Instance.ReturnToMenu();
                });
            }
            SetVisible(false);
        }

        void HandleStateChanged(GameState state)
        {
            bool visible = state == GameState.GameOver || state == GameState.Complete;
            SetVisible(visible);
        }

        void HandleRunEnded(bool won, int progress, int lives)
        {
            if (titleText != null) titleText.text = won ? "Run Complete!" : "Run Over";
            if (starsText != null) starsText.text = won ? new string('*', Mathf.Max(1, lives)) : "Keep hopping!";
            if (detailText != null)
            {
                detailText.text = won
                    ? $"You matched all {GameData.TotalQuestions} IUPAC prefixes with {lives} / {GameData.MaxLives} lives left!"
                    : $"You matched {progress} / {GameData.TotalQuestions} prefixes before running out of lives. Try again!";
            }
            if (factText != null)
            {
                factText.text = GameData.ChemistryFacts[UnityEngine.Random.Range(0, GameData.ChemistryFacts.Length)];
            }
        }

        void SetVisible(bool visible)
        {
            if (root == null) return;
            root.alpha = visible ? 1f : 0f;
            root.interactable = visible;
            root.blocksRaycasts = visible;
        }
    }
}
