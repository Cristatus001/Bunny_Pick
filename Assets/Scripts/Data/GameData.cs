using BunnyPick.Gameplay;
using UnityEngine;

namespace BunnyPick.Data
{
    /// <summary>
    /// Static game constants ported directly from the reference HTML prototype's js/data.js,
    /// so the Unity build matches its rules exactly. Hole positions were authored in the
    /// prototype's 1920x1080 canvas space and are converted here to this project's world units
    /// (10 units tall, matching the Main Camera's orthographic size of 5) via CanvasToWorld.
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

        static Vector2 CanvasToWorld(float x, float y) =>
            new Vector2((x - 1920f / 2f) * PxToWorld, (1080f / 2f - y) * PxToWorld);

        // Two staggered rows sitting entirely in the open grass below the fence, replacing the
        // prototype's original scattered layout - that one tucked a hole up under the garden
        // tree's canopy in bg_garden.png. Every pair here is >=350px apart (2x HoleHitRadius is
        // 330px) so tap targets never overlap, the rightmost column stays left of the tree's
        // trunk (canvas x ~1740-1810), and the bottom row's y is capped well short of 981 -
        // the highest a hole can sit before its own art starts clipping below the Main
        // Camera's orthographic view (size 5, i.e. canvas y 1080 maps to the bottom edge).
        public static readonly Vector2[] HolePositions =
        {
            CanvasToWorld(300, 580),
            CanvasToWorld(700, 580),
            CanvasToWorld(1050, 580),
            CanvasToWorld(1400, 580),
            CanvasToWorld(460, 940),
            CanvasToWorld(820, 940),
            CanvasToWorld(1180, 940),
            CanvasToWorld(1540, 940),
        };

        public static readonly float HoleHitRadius = 165f * PxToWorld;

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
