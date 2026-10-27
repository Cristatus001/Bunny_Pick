using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    /// <summary>Shared exit-confirmation dialog for two different contexts: mid-run (Exit ->
    /// back to the Main Menu, with a progress-will-be-lost warning) and the Main Menu itself
    /// (Exit -> quit the app entirely). Which one applies is read from GameManager.State at the
    /// moment it's shown, so a stray tap can't lose progress - or close the app - by accident.
    /// </summary>
    public class ConfirmExitUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Text titleText;
        public Text detailText;
        public Button confirmButton;
        public Button cancelButton;

        bool _quitOnConfirm;

        void Start()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
            SetAlpha(0f);
        }

        void OnConfirm()
        {
            GameManager.Instance?.PlayButtonClick();
            Time.timeScale = 1f;
            SetAlpha(0f);
            if (_quitOnConfirm)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            else
            {
                GameManager.Instance?.ReturnToMenu();
            }
        }

        void OnCancel()
        {
            GameManager.Instance?.PlayButtonClick();
            Hide();
        }

        public void Show()
        {
            _quitOnConfirm = GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu;
            if (titleText != null) titleText.text = _quitOnConfirm ? "Quit game?" : "Exit run?";
            if (detailText != null)
            {
                detailText.text = _quitOnConfirm
                    ? "This will close the app."
                    : "Your progress in this run will be lost.";
            }
            SetAlpha(1f);
            Time.timeScale = 0f;
        }

        public void Hide()
        {
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
