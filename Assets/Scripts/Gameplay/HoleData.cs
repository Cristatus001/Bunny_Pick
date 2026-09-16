namespace BunnyPick.Gameplay
{
    /// <summary>Plain data - one of the 8 fixed hole slots. Holds at most a bunny OR a teddy
    /// at a time (never both, by construction of the spawn logic).</summary>
    public class HoleData
    {
        public int Index;
        public Bunny Bunny;
        public Teddy Teddy;
    }
}
