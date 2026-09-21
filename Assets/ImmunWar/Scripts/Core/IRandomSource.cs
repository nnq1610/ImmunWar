namespace ImmunWar.Core
{
    public interface IRandomSource
    {
        int Range(int minimumInclusive, int maximumExclusive);
        float Value();
    }
}

