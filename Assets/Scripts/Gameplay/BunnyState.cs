namespace BunnyPick.Gameplay
{
    public enum BunnyState
    {
        Active,     // up, waiting to be tapped
        Caught,     // correctly tapped, fading out
        Retreating, // missed or wrong-tapped, sinking back into its hole
    }
}
