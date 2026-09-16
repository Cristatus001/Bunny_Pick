using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    /// <summary>Corner info icon - only shown mid-run, matching the reference.</summary>
    public class InfoButtonUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Button button;
        public InfoTabUI infoTab;

        void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        void Start()
        {
            if (button != null && infoTab != null)
            {
                button.onClick.AddListener(() =>
                {
                    GameManager.Instance?.PlayButtonClick();
                    infoTab.Show();
                });
            }
            SetVisible(false);
        }

        void HandleStateChanged(GameState state) => SetVisible(state == GameState.Playing);

        void SetVisible(bool visible)
        {
            if (root == null) return;
            root.alpha = visible ? 1f : 0f;
            root.interactable = visible;
            root.blocksRaycasts = visible;
        }
    }
}
