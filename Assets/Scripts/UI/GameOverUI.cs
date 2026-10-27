using BunnyPick.Data;
using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    public class GameOverUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Image bannerImage;
        public Sprite winBanner;
        public Sprite loseBanner;
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
            if (bannerImage != null) bannerImage.sprite = won ? winBanner : loseBanner;
            if (starsText != null)
            {
                int stars = ComputeStars(won, progress, lives);
                string filled = new string('*', stars);
                string empty = new string('*', MaxStars - stars);
                starsText.text = empty.Length > 0 ? $"{filled}<color=#B8B8B8>{empty}</color>" : filled;
            }
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

        const int MaxStars = 3;

        // A win was previously the only case that showed any stars at all (as a flat lives
        // count), and a loss always showed none - neither actually reflected "player
        // performance" the way a star rating implies. This rates every run 0-3: a win by how
        // many lives were left (a flawless run keeps all 3 and earns all 3 stars), a loss by how
        // far through the question set the player got before running out.
        static int ComputeStars(bool won, int progress, int lives)
        {
            if (won)
            {
                float lifeFraction = (float)lives / GameData.MaxLives;
                if (lifeFraction >= 0.66f) return 3;
                if (lifeFraction >= 0.33f) return 2;
                return 1;
            }
            float progressFraction = GameData.TotalQuestions > 0 ? (float)progress / GameData.TotalQuestions : 0f;
            if (progressFraction >= 0.66f) return 2;
            if (progressFraction >= 0.33f) return 1;
            return 0;
        }
    }
}
