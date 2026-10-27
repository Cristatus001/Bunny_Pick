using BunnyPick.Data;
using UnityEngine;

namespace BunnyPick.Gameplay
{
    /// <summary>
    /// Pop-in/retreat/collect-poof easing, ported directly from the reference prototype's
    /// js/render.js (computePopIn/computeRetreat/computeCollectPoof/bunnyTransform) so bunnies
    /// and teddies move exactly like the reference. Shared by every view that needs to know a
    /// hole occupant's current visual offset/scale/alpha (the sprite itself, its chat bubble,
    /// a teddy's badge chip) so they all stay in sync without re-deriving the math.
    /// </summary>
    public static class PopAnimation
    {
        public struct Sample
        {
            public float offsetY; // world units, positive = sunk down into the hole
            public float scale;
            public float alpha;
        }

        // How far a bunny/teddy sinks below its resting position while hidden in its hole.
        // Scaled down (x0.568, matching bunny_character.png's own scale-down in
        // BunnyPickSceneBuilder) from the original 150px so a bunny still sinks by roughly the
        // same fraction of its own height as before, now that both it and gameplay_bg.png's
        // painted holes are noticeably smaller than the old art.
        public static readonly float PopRiseWorldUnits = 85f * GameData.PxToWorld;

        // How small a bunny/teddy shrinks while tucked down in its hole. The reference only
        // varies scale between ~0.72 and 1.0, which barely reads as a size change in-world;
        // shrinking much closer to 0 makes "growing out of / shrinking into a small hole"
        // actually look like that, instead of just sliding up and down at nearly full size.
        const float MinPopScale = 0.12f;

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        static float EaseInQuad(float t) => t * t;

        static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        // How far above a bunny's resting spot the grabbing hand starts its swoop, and how far
        // it carries the bunny upward afterwards - both in world units, matching the scale of
        // PopRiseWorldUnits above.
        const float HandApproachOffset = 328f * GameData.PxToWorld;
        const float HandLiftDistance = 150f * GameData.PxToWorld;

        // Fraction of FadeDuration spent swooping down to the bunny before the hand closes and
        // lifts it away, and the fraction at which the carried pair starts fading out.
        const float HandDescendFraction = 0.42f;
        const float FadeStartFraction = 0.55f;

        public struct CaughtSample
        {
            public Sample bunny;
            public float handOffsetY;
            public bool handClosed;
            public float handAlpha;
        }

        // On a correct tap the bunny doesn't just fade in place - a hand swoops down, grabs it,
        // and carries it off. Bunny and hand share one timeline here so they always stay in sync.
        public static CaughtSample ComputeCaught(float fadeAge)
        {
            float t = Mathf.Clamp01(fadeAge / GameData.FadeDuration);
            float handOffsetY;
            bool closed;
            float bunnyOffsetY;
            float bunnyScale = 1f;

            if (t < HandDescendFraction)
            {
                float d = EaseOutQuad(t / HandDescendFraction);
                handOffsetY = Mathf.Lerp(-HandApproachOffset, 0f, d);
                closed = d > 0.82f;
                bunnyOffsetY = 0f;
            }
            else
            {
                float c = EaseInQuad((t - HandDescendFraction) / (1f - HandDescendFraction));
                handOffsetY = Mathf.Lerp(0f, -HandLiftDistance, c);
                closed = true;
                bunnyOffsetY = handOffsetY;
                bunnyScale = Mathf.Lerp(1f, 0.55f, c);
            }

            float alpha = 1f;
            if (t > FadeStartFraction)
            {
                alpha = Mathf.Max(0f, 1f - (t - FadeStartFraction) / (1f - FadeStartFraction));
            }

            return new CaughtSample
            {
                bunny = new Sample { offsetY = bunnyOffsetY, scale = bunnyScale, alpha = alpha },
                handOffsetY = handOffsetY,
                handClosed = closed,
                handAlpha = alpha,
            };
        }

        public static bool TryComputeHand(Bunny b, out float offsetY, out bool closed, out float alpha)
        {
            if (b == null || b.State != BunnyState.Caught)
            {
                offsetY = 0f;
                closed = false;
                alpha = 0f;
                return false;
            }
            var c = ComputeCaught(b.FadeAge);
            offsetY = c.handOffsetY;
            closed = c.handClosed;
            alpha = c.handAlpha;
            return true;
        }

        public static Sample ComputePopIn(float age)
        {
            float t = Mathf.Clamp01(age / GameData.PopInDuration);
            float eased = EaseOutBack(t);
            return new Sample
            {
                offsetY = PopRiseWorldUnits * (1f - Mathf.Clamp01(eased)),
                scale = Mathf.Max(MinPopScale, MinPopScale + (1f - MinPopScale) * eased),
                alpha = Mathf.Min(1f, 0.15f + t * 0.85f),
            };
        }

        public static Sample ComputeRetreat(float retreatAge)
        {
            float t = Mathf.Clamp01(retreatAge / GameData.RetreatDuration);
            float eased = EaseInQuad(t);
            return new Sample
            {
                offsetY = PopRiseWorldUnits * eased,
                scale = Mathf.Lerp(1f, MinPopScale, eased),
                alpha = Mathf.Max(0f, 1f - eased),
            };
        }

        public static Sample ComputeCollectPoof(float collectAge)
        {
            float t = Mathf.Clamp01(collectAge / GameData.CollectDuration);
            return new Sample
            {
                offsetY = -t * 28f * GameData.PxToWorld,
                scale = 1f + t * 0.5f,
                alpha = Mathf.Max(0f, 1f - t),
            };
        }

        public static Sample BunnyTransform(Bunny b)
        {
            if (b.State == BunnyState.Caught) return ComputeCaught(b.FadeAge).bunny;
            if (b.State == BunnyState.Retreating) return ComputeRetreat(b.RetreatAge);
            return ComputePopIn(b.Age);
        }

        public static Sample TeddyTransform(Teddy t)
        {
            if (t.State == TeddyState.Collected) return ComputeCollectPoof(t.CollectAge);
            if (t.State == TeddyState.Retreating) return ComputeRetreat(t.RetreatAge);
            return ComputePopIn(t.Age);
        }
    }
}
