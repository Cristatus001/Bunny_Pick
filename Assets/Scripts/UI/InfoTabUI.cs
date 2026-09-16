using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    /// <summary>How-to-play + IUPAC prefix legend, merged into one tab. Auto-opens once when
    /// a fresh run starts (closing it is what lets the run actually begin, since opening it
    /// pauses via Time.timeScale like every other overlay).</summary>
    public class InfoTabUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Button closeXButton;
        public Button gotItButton;

        void OnEnable()
        {
            GameManager.OnAutoOpenInfo += HandleAutoOpen;
        }

        void OnDisable()
        {
            GameManager.OnAutoOpenInfo -= HandleAutoOpen;
        }

        void Start()
        {
            if (closeXButton != null) closeXButton.onClick.AddListener(Hide);
            if (gotItButton != null) gotItButton.onClick.AddListener(Hide);
            SetAlpha(0f);
        }

        void HandleAutoOpen() => Show();

        public void Show()
        {
            SetAlpha(1f);
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            GameManager.Instance?.PlayButtonClick();
            SetAlpha(0f);
            Time.timeScale = 1f;
        }

        void SetAlpha(float a)
        {
            if (root == null) return;
            root.alpha = a;
            root.interactable = a > 0.5f;
            root.blocksRaycasts = a > 0.5f;
        }
    }
}
