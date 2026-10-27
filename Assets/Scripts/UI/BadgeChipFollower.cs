using BunnyPick.Data;
using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    /// <summary>
    /// Screen-space power-up badge chip floating beside a teddy bear, tracking its hole every
    /// frame - lets the player read which effect a bear grants before deciding to tap it.
    /// </summary>
    public class BadgeChipFollower : MonoBehaviour
    {
        public int holeIndex;
        public RectTransform chipRoot;
        public CanvasGroup visibilityGroup;
        public Image badgeImage;
        public Camera worldCamera;
        public Vector3 worldOffset = new Vector3(-0.34f, 0.68f, 0f);

        public Sprite badgeExtraLife;
        public Sprite badgeSlowTime;
        public Sprite badgeReveal;
        public Sprite badgeFreeze;

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || chipRoot == null) return;

            var teddy = gm.Holes[holeIndex].Teddy;
            bool visible = teddy != null && gm.State == GameState.Playing;
            if (visibilityGroup != null) visibilityGroup.alpha = visible ? 1f : 0f;
            if (!visible) return;

            if (badgeImage != null) badgeImage.sprite = LookupBadge(teddy.EffectType);

            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;

            var s = PopAnimation.TeddyTransform(teddy);
            if (visibilityGroup != null) visibilityGroup.alpha = s.alpha;

            Transform anchor = HoleView.GetAnchor(holeIndex);
            Vector3 basePos = anchor != null ? anchor.position : GameData.HolePositions[holeIndex];
            Vector3 worldPos = basePos + new Vector3(0, -s.offsetY, 0) + worldOffset;
            chipRoot.position = worldCamera.WorldToScreenPoint(worldPos);
        }

        Sprite LookupBadge(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.ExtraLife: return badgeExtraLife;
                case PowerupType.SlowTime: return badgeSlowTime;
                case PowerupType.Reveal: return badgeReveal;
                case PowerupType.Freeze: return badgeFreeze;
                default: return badgeExtraLife;
            }
        }
    }
}
