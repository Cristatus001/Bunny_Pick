using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    public class SettingsButtonUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Button button;
        public SettingsPanelUI settingsPanel;

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
            if (button != null && settingsPanel != null)
            {
                button.onClick.AddListener(() =>
                {
                    GameManager.Instance?.PlayButtonClick();
                    settingsPanel.Toggle();
                });
            }
            SetVisible(true); // title screen by default
        }

        void HandleStateChanged(GameState state)
        {
            SetVisible(state == GameState.MainMenu || state == GameState.Playing);
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
