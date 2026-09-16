namespace BunnyPick.Gameplay
{
    /// <summary>Plain data - one bunny occupying a hole. Ported from entities.js createBunny.</summary>
    public class Bunny
    {
        public int HoleIndex;
        public string Color;
        public string Prefix;
        public bool IsTarget;
        public float Lifetime;
        public float Age;
        public float FadeAge;
        public float RetreatAge;
        public BunnyState State = BunnyState.Active;
    }
}
