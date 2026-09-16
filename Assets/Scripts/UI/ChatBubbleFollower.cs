using BunnyPick.Data;
using BunnyPick.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyPick.UI
{
    /// <summary>
    /// Screen-space chat bubble showing a bunny's prefix, tracking its hole every frame.
    /// Lives on the main (Screen Space Overlay) Canvas, which is proven to render correctly
    /// under this project's URP 2D Renderer - unlike a World Space Canvas attached directly
    /// to the bunny (see the earlier invisible-label bug).
    /// </summary>
    public class ChatBubbleFollower : MonoBehaviour
    {
        public int holeIndex;
        public RectTransform bubbleRoot;
        public CanvasGroup visibilityGroup;
        public Image bodyImage;
        public Text text;
        public Camera worldCamera;
        public Vector3 worldOffset = new Vector3(0.44f, 1.6f, 0f);

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || bubbleRoot == null) return;

            var bunny = gm.Holes[holeIndex].Bunny;
            bool visible = bunny != null && gm.State == GameState.Playing;
            if (visibilityGroup != null) visibilityGroup.alpha = visible ? 1f : 0f;
            if (!visible) return;

            if (text != null) text.text = bunny.Prefix;

            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;

            var s = PopAnimation.BunnyTransform(bunny);
            if (visibilityGroup != null) visibilityGroup.alpha = s.alpha;

            Transform anchor = HoleView.GetAnchor(holeIndex);
            Vector3 basePos = anchor != null ? anchor.position : GameData.HolePositions[holeIndex];
            Vector3 worldPos = basePos + new Vector3(0, -s.offsetY, 0) + worldOffset;
            bubbleRoot.position = worldCamera.WorldToScreenPoint(worldPos);
        }
    }
}
