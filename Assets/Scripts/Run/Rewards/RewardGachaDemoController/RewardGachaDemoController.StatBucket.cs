// Tracks rarity counts observed by the reward gacha demo for one encounter mode.
public sealed partial class RewardGachaDemoController
{
    private sealed class StatBucket
    {
        public int total;
        public int common;
        public int uncommon;
        public int rare;
        public int special;

        // Clears all rarity counters for this mode.
        public void Reset()
        {
            total = 0;
            common = 0;
            uncommon = 0;
            rare = 0;
            special = 0;
        }

        // Adds one observed card rarity to this bucket.
        public void Add(RewardGachaRarity rarity)
        {
            total++;
            switch (rarity)
            {
                case RewardGachaRarity.Uncommon:
                    uncommon++;
                    break;
                case RewardGachaRarity.Rare:
                    rare++;
                    break;
                case RewardGachaRarity.Special:
                    special++;
                    break;
                default:
                    common++;
                    break;
            }
        }
    }
}
