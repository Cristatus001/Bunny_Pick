using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    /// <summary>"Exit run?" confirmation shown when Exit is tapped mid-run, so a stray tap
    /// can't lose progress by accident.</summary>
    public class ConfirmExitUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Button confirmButton;
        public Button cancelButton;

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
            GameManager.Instance?.ReturnToMenu();
        }

        void OnCancel()
        {
            GameManager.Instance?.PlayButtonClick();
            Hide();
        }

        public void Show()
        {
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
