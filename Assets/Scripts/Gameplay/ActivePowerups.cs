namespace BunnyPick.Gameplay
{
    /// <summary>Elapsed-time deadlines for currently active power-up effects (0 = inactive).</summary>
    public class ActivePowerups
    {
        public float SlowTimeUntil;
        public float RevealUntil;
        public float FreezeUntil;
        public int RevealHoleIndex = -1;
    }
}
