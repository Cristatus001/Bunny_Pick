using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    public class LoadingScreenUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Text factText;

        void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
            GameManager.OnLoadingFactChanged += HandleFactChanged;
        }

        void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
            GameManager.OnLoadingFactChanged -= HandleFactChanged;
        }

        void Start()
        {
            SetVisible(false);
        }

        void HandleStateChanged(GameState state)
        {
            SetVisible(state == GameState.Loading);
        }

        void HandleFactChanged(string fact)
        {
            if (factText != null) factText.text = "Did you know? " + fact;
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
