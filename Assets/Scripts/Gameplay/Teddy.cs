namespace BunnyPick.Gameplay
{
    /// <summary>Plain data - one teddy bear power-up pickup occupying a hole. Ported from
    /// entities.js createTeddyBear.</summary>
    public class Teddy
    {
        public int HoleIndex;
        public PowerupType EffectType;
        public float Lifetime;
        public float Age;
        public float RetreatAge;
        public float CollectAge;
        public TeddyState State = TeddyState.Active;
    }
}
