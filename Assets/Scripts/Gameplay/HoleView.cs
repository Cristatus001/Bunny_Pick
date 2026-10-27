using BunnyPick.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BunnyPick.Gameplay
{
    /// <summary>
    /// Presentation for one fixed hole slot: reads GameManager.Holes[holeIndex] every frame
    /// and drives the bunny/teddy sprite transforms via PopAnimation. Never mutates game
    /// state itself (mirrors render.js's read-only relationship with engine.js) - the click
    /// collider is always enabled and just forwards to GameManager.TapHole, which safely
    /// no-ops on an empty or inactive hole.
    /// </summary>
    public class HoleView : MonoBehaviour
    {
        public int holeIndex;

        public Transform bunnyPivot;
        public SpriteRenderer bunnySprite;
        public Color[] bunnyColorTints; // parallel to GameData.BunnyColors, multiplied onto bunnySprite
        public Sprite[] bunnyCharacterSprites; // the 2 bunny art variants; picked by color index parity

        public Transform teddyPivot;
        public SpriteRenderer teddySprite;

        public Transform handPivot;
        public SpriteRenderer handSprite;
        public Sprite handOpenSprite;
        public Sprite handClosedSprite;

        public SpriteRenderer hintRing;

        string _lastBunnyColor;

        // Lets screen-space followers (chat bubbles, badge chips) find a hole's live transform
        // by index at runtime instead of relying on GameData.HolePositions, so they stay correct
        // even if a hole is repositioned by hand in the Editor after the scene was built.
        static readonly HoleView[] Registry = new HoleView[GameData.HoleCount];

        public static Transform GetAnchor(int holeIndex)
        {
            if (holeIndex < 0 || holeIndex >= Registry.Length) return null;
            var view = Registry[holeIndex];
            return view != null ? view.transform : null;
        }

        void Awake()
        {
            if (holeIndex >= 0 && holeIndex < Registry.Length) Registry[holeIndex] = this;
        }

        void OnDestroy()
        {
            if (holeIndex >= 0 && holeIndex < Registry.Length && Registry[holeIndex] == this) Registry[holeIndex] = null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Holes == null || holeIndex < 0 || holeIndex >= gm.Holes.Length) return;

            var hole = gm.Holes[holeIndex];
            UpdateBunny(hole.Bunny);
            UpdateTeddy(hole.Teddy);
            UpdateHand(hole.Bunny);
            UpdateHintRing(gm, hole);
        }

        void UpdateBunny(Bunny b)
        {
            bool present = b != null;
            if (bunnyPivot.gameObject.activeSelf != present) bunnyPivot.gameObject.SetActive(present);
            if (!present) return;

            if (_lastBunnyColor != b.Color)
            {
                _lastBunnyColor = b.Color;
                Color tint = LookupBunnyTint(b.Color);
                tint.a = bunnySprite.color.a;
                bunnySprite.color = tint;
                bunnySprite.sprite = LookupBunnySprite(b.Color);
            }

            var s = PopAnimation.BunnyTransform(b);
            ApplyPose(bunnyPivot, bunnySprite, s);
        }

        void UpdateTeddy(Teddy t)
        {
            bool present = t != null;
            if (teddyPivot.gameObject.activeSelf != present) teddyPivot.gameObject.SetActive(present);
            if (!present) return;

            var s = PopAnimation.TeddyTransform(t);
            ApplyPose(teddyPivot, teddySprite, s);
        }

        void UpdateHand(Bunny b)
        {
            if (handPivot == null) return;
            bool visible = PopAnimation.TryComputeHand(b, out float offsetY, out bool closed, out float alpha);
            if (handPivot.gameObject.activeSelf != visible) handPivot.gameObject.SetActive(visible);
            if (!visible) return;

            var pos = handPivot.localPosition;
            pos.y = -offsetY;
            handPivot.localPosition = pos;

            if (handSprite != null)
            {
                handSprite.sprite = closed ? handClosedSprite : handOpenSprite;
                var c = handSprite.color;
                c.a = alpha;
                handSprite.color = c;
            }
        }

        static void ApplyPose(Transform pivot, SpriteRenderer sprite, PopAnimation.Sample s)
        {
            var pos = pivot.localPosition;
            pos.y = -s.offsetY;
            pivot.localPosition = pos;
            pivot.localScale = new Vector3(s.scale, s.scale, 1f);
            if (sprite != null)
            {
                var c = sprite.color;
                c.a = s.alpha;
                sprite.color = c;
            }
        }

        void UpdateHintRing(GameManager gm, HoleData hole)
        {
            if (hintRing == null) return;
            bool show = gm.Powerups.RevealHoleIndex == holeIndex
                && hole.Bunny != null && hole.Bunny.IsTarget && hole.Bunny.State == BunnyState.Active;
            if (hintRing.gameObject.activeSelf != show) hintRing.gameObject.SetActive(show);
        }

        Color LookupBunnyTint(string color)
        {
            if (bunnyColorTints == null || bunnyColorTints.Length == 0) return Color.white;
            for (int i = 0; i < GameData.BunnyColors.Length && i < bunnyColorTints.Length; i++)
            {
                if (GameData.BunnyColors[i] == color) return bunnyColorTints[i];
            }
            return bunnyColorTints[0];
        }

        // Alternates the two bunny art variants by each color's position in GameData.BunnyColors
        // (white/skyblue/lavender get one, pink/lemon/mint the other) so both are seen roughly as
        // often, without needing a separate "which sprite" field on Bunny itself.
        Sprite LookupBunnySprite(string color)
        {
            if (bunnyCharacterSprites == null || bunnyCharacterSprites.Length == 0) return null;
            int colorIndex = System.Array.IndexOf(GameData.BunnyColors, color);
            int spriteIndex = colorIndex >= 0 ? colorIndex % bunnyCharacterSprites.Length : 0;
            return bunnyCharacterSprites[spriteIndex];
        }

        void OnMouseDown()
        {
            // OnMouseDown is a physics-collider pick, not a UI one - it fires regardless of
            // what's drawn on top, so without this check a tap would reach straight through
            // any open panel (Settings, Info, Exit-confirm, Game Over) to the hole underneath.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            GameManager.Instance?.TapHole(holeIndex);
        }
    }
}
