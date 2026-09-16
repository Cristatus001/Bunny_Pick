using BunnyPick.Data;
using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Button playButton;
        public Text statsText;

        void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
            GameManager.OnPersonalBestChanged += HandlePersonalBestChanged;
        }

        void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
            GameManager.OnPersonalBestChanged -= HandlePersonalBestChanged;
        }

        void Start()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(() =>
                {
                    GameManager.Instance.PlayButtonClick();
                    GameManager.Instance.StartGame(showInfoIntro: true);
                });
            }
            SetVisible(true);
        }

        void HandleStateChanged(GameState state)
        {
            SetVisible(state == GameState.MainMenu);
        }

        void HandlePersonalBestChanged(PersonalBestData best)
        {
            if (statsText == null) return;
            if (best.BestProgress <= 0 && best.Wins <= 0)
            {
                statsText.text = "No runs yet - tap Play to start!";
                return;
            }
            string winsPart = best.Wins > 0 ? $"  ({best.Wins} win{(best.Wins == 1 ? "" : "s")})" : "";
            statsText.text = $"Personal Best: {best.BestProgress} / {GameData.TotalQuestions} ({best.BestLives} lives left){winsPart}";
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
