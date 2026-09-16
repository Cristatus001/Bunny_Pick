namespace BunnyPick.Gameplay
{
    public enum TeddyState
    {
        Active,     // up, waiting to be tapped
        Collected,  // tapped, playing a brief "poof" pickup animation
        Retreating, // expired un-tapped, sinking back into its hole
    }
}
