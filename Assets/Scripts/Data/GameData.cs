using BunnyPick.Gameplay;
using UnityEngine;

namespace BunnyPick.Data
{
    /// <summary>
    /// Static game constants ported directly from the reference HTML prototype's js/data.js,
    /// so the Unity build matches its rules exactly (gameplay rules only - hole positions below
    /// are this project's own, measured from its real background art rather than the prototype).
    /// </summary>
    public static class GameData
    {
        public const int TotalQuestions = 20;
        public const int MaxLives = 3;
        public const int HoleCount = 8;

        public static readonly string[] BunnyColors =
        {
            "white", "pink", "skyblue", "lemon", "lavender", "mint"
        };

        // digit (0-9) -> IUPAC numerical placeholder prefix.
        static readonly string[] Prefixes =
        {
            "nil", "un", "bi", "tri", "quad", "pent", "hex", "sept", "oct", "enn"
        };

        public static string GetPrefixForDigit(int digit) => Prefixes[digit];

        const float CanvasHeight = 1080f;
        const float WorldHeight = 10f;
        public const float PxToWorld = WorldHeight / CanvasHeight;

        // gameplay_bg.png (2120x1280, imported at 119 px/world-unit - see BunnyPickSceneBuilder.
        // ImportRealArt) already has 8 dirt holes hand-painted into the hillside; these are THAT
        // art's own holes converted to world space (worldX = (pixelX-1060)/119, worldY =
        // (640-pixelY)/119), not an abstract design grid, so bunnies pop up exactly where the
        // background already shows a hole. Source pixel positions, for re-measuring if the art
        // ever changes: (314,1122) (1887,1138) (1926,785) (676,1196) (1469,1072) (1021,1026)
        // (1523,857) (535,1006).
        public static readonly Vector2[] HolePositions =
        {
            new Vector2(-6.270f, -4.046f),
            new Vector2(6.945f, -4.187f),
            new Vector2(7.280f, -1.218f),
            new Vector2(-3.227f, -4.674f),
            new Vector2(3.433f, -3.632f),
            new Vector2(-0.329f, -3.247f),
            new Vector2(3.890f, -1.821f),
            new Vector2(-4.408f, -3.074f),
        };

        // Bunny sprites stand ~2.4 world units tall from their near-ground pivot, so a single
        // small radius centered at ground level only ever covered their feet - taps on the face
        // or ears (where players actually tap) missed. Each hole gets its own radius instead,
        // capped by HALF its distance to its single nearest neighbour (with a safety margin) so
        // tap zones can never overlap, no matter which pair of holes ends up closest; isolated
        // holes (far from every other hole) get a much more forgiving zone than tightly-clustered
        // ones. Provably safe for every pair (i, j): radius_i <= d(i,j)/2*factor and
        // radius_j <= d(i,j)/2*factor (each hole's own nearest-neighbour distance is <= d(i,j) by
        // definition), so radius_i + radius_j <= d(i,j)*factor <= d(i,j).
        const float HoleHitRadiusSafetyFactor = 0.92f;
        const float HoleHitRadiusMax = 1.1f;
        public static readonly float[] HoleHitRadii = ComputeHoleHitRadii();

        static float[] ComputeHoleHitRadii()
        {
            var radii = new float[HolePositions.Length];
            for (int i = 0; i < HolePositions.Length; i++)
            {
                float minDist = float.MaxValue;
                for (int j = 0; j < HolePositions.Length; j++)
                {
                    if (i == j) continue;
                    float d = Vector2.Distance(HolePositions[i], HolePositions[j]);
                    if (d < minDist) minDist = d;
                }
                radii[i] = Mathf.Min(HoleHitRadiusMax, minDist * 0.5f * HoleHitRadiusSafetyFactor);
            }
            return radii;
        }

        // Centers the tap collider on the bunny's torso/face instead of its feet - a uniform
        // vertical shift is always safe regardless of neighbouring holes (it moves every hole's
        // zone by the same amount, so pairwise distances, and therefore the radii above, are
        // unaffected).
        public static readonly float HoleHitOffsetY = 1.1f;

        public struct SpeedParams
        {
            public float spawnInterval;
            public float bunnyLifetime;
            public int activeCount;
            public string tierLabel;
        }

        static readonly SpeedParams SpeedStart = new SpeedParams { spawnInterval = 2.6f, bunnyLifetime = 2.6f, activeCount = 3 };
        static readonly SpeedParams SpeedEnd = new SpeedParams { spawnInterval = 1.05f, bunnyLifetime = 1.15f, activeCount = 7 };

        public static SpeedParams GetSpeedParams(int progress, int totalQuestions)
        {
            float t = totalQuestions > 1 ? (float)progress / (totalQuestions - 1) : 0f;
            t = Mathf.Clamp01(t);
            string tier = progress < 5 ? "slow" : progress < 10 ? "moderate" : progress < 15 ? "fast" : "very fast";
            return new SpeedParams
            {
                spawnInterval = Mathf.Lerp(SpeedStart.spawnInterval, SpeedEnd.spawnInterval, t),
                bunnyLifetime = Mathf.Lerp(SpeedStart.bunnyLifetime, SpeedEnd.bunnyLifetime, t),
                activeCount = Mathf.RoundToInt(Mathf.Lerp(SpeedStart.activeCount, SpeedEnd.activeCount, t)),
                tierLabel = tier,
            };
        }

        public static readonly PowerupType[] PowerupTypes =
        {
            PowerupType.ExtraLife, PowerupType.SlowTime, PowerupType.Reveal, PowerupType.Freeze
        };

        public const float SlowTimeDuration = 4.0f;
        public const float RevealDuration = 3.5f;
        public const float FreezeDuration = 3.0f;
        public const float SlowTimeFactor = 0.35f;
        public const float TeddyLifetime = 3.5f;
        public const float TeddySpawnChance = 0.35f;
        public const float FadeDuration = 0.6f;

        public const float PopInDuration = 0.22f;
        public const float RetreatDuration = 0.26f;
        public const float CollectDuration = 0.3f;

        public static readonly string[] ChemistryFacts =
        {
            "IUPAC prefixes like 'tri' and 'tetra' tell you how many of something (e.g. carbon atoms) are in a molecule.",
            "IUPAC stands for the International Union of Pure and Applied Chemistry.",
            "'Mono' (1) is often dropped in names when there's no risk of confusion - CO is 'carbon monoxide', not 'monocarbon monoxide'.",
            "The prefix 'oct' (8) is also why an octopus has eight arms!",
            "'Deca' means 10 - decane is a molecule with ten carbon atoms in a row.",
            "Multiplying prefixes (di, tri, tetra...) are different from locants, the numbers that say WHERE a group is attached.",
            "Systematic IUPAC names let any chemist in the world know exactly which molecule you mean, in any language.",
        };
    }
}
