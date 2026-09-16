using System.Collections;
using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    public class HudUI : MonoBehaviour
    {
        public CanvasGroup root;
        public Image[] lifeIcons;
        public Sprite heartFullSprite;
        public Sprite heartEmptySprite;
        public Text digitText;
        public Text questionCounterText;

        [System.Serializable]
        public class PowerupChip
        {
            public GameObject root;
            public PowerupType type;
        }
        public PowerupChip[] powerupChips;

        Coroutine _digitPunch;

        void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
            GameManager.OnLivesChanged += HandleLivesChanged;
            GameManager.OnDigitChanged += HandleDigitChanged;
            GameManager.OnQuestionProgress += HandleQuestionProgress;
            GameManager.OnPowerupsChanged += HandlePowerupsChanged;
        }

        void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
            GameManager.OnLivesChanged -= HandleLivesChanged;
            GameManager.OnDigitChanged -= HandleDigitChanged;
            GameManager.OnQuestionProgress -= HandleQuestionProgress;
            GameManager.OnPowerupsChanged -= HandlePowerupsChanged;
        }

        void HandleStateChanged(GameState state)
        {
            SetVisible(state == GameState.Playing);
        }

        void SetVisible(bool visible)
        {
            if (root == null) return;
            root.alpha = visible ? 1f : 0f;
            root.interactable = visible;
            root.blocksRaycasts = visible;
        }

        void HandleLivesChanged(int lives)
        {
            if (lifeIcons == null) return;
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null) lifeIcons[i].sprite = i < lives ? heartFullSprite : heartEmptySprite;
            }
        }

        void HandleDigitChanged(int digit)
        {
            if (digitText != null) digitText.text = digit.ToString();
            if (digitText != null)
            {
                if (_digitPunch != null) StopCoroutine(_digitPunch);
                _digitPunch = StartCoroutine(PunchScale(digitText.transform));
            }
        }

        void HandleQuestionProgress(int current, int total)
        {
            if (questionCounterText != null) questionCounterText.text = $"Question {current} / {total}";
        }

        void HandlePowerupsChanged()
        {
            if (powerupChips == null) return;
            var p = GameManager.Instance.Powerups;
            foreach (var chip in powerupChips)
            {
                if (chip?.root == null) continue;
                bool active = chip.type switch
                {
                    PowerupType.SlowTime => p.SlowTimeUntil > 0f,
                    PowerupType.Reveal => p.RevealUntil > 0f,
                    PowerupType.Freeze => p.FreezeUntil > 0f,
                    _ => false,
                };
                chip.root.SetActive(active);
            }
        }

        static IEnumerator PunchScale(Transform t)
        {
            const float duration = 0.2f;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = time / duration;
                float scale = 1f + 0.3f * (1f - k) * Mathf.Sin(k * Mathf.PI);
                t.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}
