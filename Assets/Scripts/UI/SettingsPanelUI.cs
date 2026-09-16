using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    /// <summary>
    /// Small settings popover anchored to the corner icon: a music on/off toggle and an exit
    /// row. Opening it pauses the run (Time.timeScale = 0) without covering the screen.
    /// Exiting mid-run routes through the confirm-exit dialog; on the title screen it's a
    /// no-op, matching the reference.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Toggle soundToggle;
        public Image musicToggleIcon;
        public Sprite soundOnSprite;
        public Sprite soundOffSprite;
        public Button exitButton;
        public ConfirmExitUI confirmExit;

        const string SoundPrefKey = "BunnyPick.SoundOn";
        bool _isOpen;

        void Start()
        {
            bool soundOn = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
            AudioListener.volume = soundOn ? 1f : 0f;
            UpdateMusicIcon(soundOn);

            if (soundToggle != null)
            {
                soundToggle.isOn = soundOn;
                soundToggle.onValueChanged.AddListener(OnSoundToggle);
            }
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            SetAlpha(0f);
        }

        void OnSoundToggle(bool on)
        {
            AudioListener.volume = on ? 1f : 0f;
            PlayerPrefs.SetInt(SoundPrefKey, on ? 1 : 0);
            UpdateMusicIcon(on);
            GameManager.Instance?.PlayButtonClick();
        }

        void UpdateMusicIcon(bool on)
        {
            if (musicToggleIcon != null) musicToggleIcon.sprite = on ? soundOnSprite : soundOffSprite;
        }

        void OnExitClicked()
        {
            GameManager.Instance?.PlayButtonClick();
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                Hide();
                confirmExit?.Show();
            }
            // Title screen: exit intentionally does nothing (matches the reference).
        }

        public void Toggle()
        {
            if (_isOpen) Hide(); else Show();
        }

        public void Show()
        {
            _isOpen = true;
            SetAlpha(1f);
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            _isOpen = false;
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
