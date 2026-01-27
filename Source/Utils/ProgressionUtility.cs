namespace NACopsV1
{
    #region Utils for progression
    [Serializable]
    public class MinMaxThreshold
    {
        public int MinOf { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        public MinMaxThreshold(int minOf, float min, float max)
        {
            MinOf = minOf;
            Min = min;
            Max = max;
        }
    }
    public static class ThresholdUtils
    {
        public static (float min, float max) Evaluate(List<MinMaxThreshold> thresholds, int value)
        {
            MinMaxThreshold bestMatch = thresholds[0];
            foreach (var threshold in thresholds)
            {
                if (value >= threshold.MinOf)
                    bestMatch = threshold;
                else
                    break;
            }

            return (bestMatch.Min, bestMatch.Max);
        }
    }

    [Serializable]
    public class ThresholdMappings
    {
        // Days total
        public List<MinMaxThreshold> LethalCopFrequency = new()
            {
                new(0,   30f, 60f),
                new(5,   20f, 60f),
                new(10,  20f, 50f),
                new(20,  15f, 40f),
                new(30,  10f, 30f),
                new(40,  10f, 20f),
                new(50,  8f, 18f),
            };
        // Networth - min distance max distance rand range
        public List<MinMaxThreshold> LethalCopRange = new()
            {
                new(0,      1f, 3f),
                new(8000,   1f, 5f),
                new(30000,  2f, 6f),
                new(100000, 3f, 8f),
                new(300000, 4f, 10f),
                new(600000, 9f, 14f),
                new(1000000, 10f, 15f),

            };

        // Days total
        public List<MinMaxThreshold> NearbyCrazyFrequency = new()
            {
                new(0,   120f, 400f),
                new(5,   120f, 350f),
                new(10,  100f, 200f),
                new(20,  80f,  100f),
                new(30,  60f,  100f),
                new(40,  50f,  80f),
                new(50,  30f,  80f),
            };
        // Networth - min distance max distance rand range
        public List<MinMaxThreshold> NearbyCrazyRange = new()
            {
                new(0,      10f, 20f),
                new(8000,   10f, 25f),
                new(30000,  10f, 30f),
                new(100000, 20f, 35f),
                new(300000, 20f, 40f),
                new(500000, 25f, 40f),

            };

        // Networth
        public List<MinMaxThreshold> PIFrequency = new()
            {
                new(0,        600f, 1200f),
                new(9000,     500f, 1000f),
                new(30000,    450f, 800f),
                new(50000,    400f, 800f),
                new(80000,    300f, 600f),
                new(300000,   300f, 550f),
                new(500000,   300f, 550f),
                new(900000,   300f, 500f),
                new(1500000,  300f, 500f),
                new(8000000,  300f, 400f),
            };

        // Days total - probability range of snitching a sample ( result > 0.5 = true )
        public List<MinMaxThreshold> SnitchProbability = new()
            {
                new(0,   0f, 0.53f),
                new(5,   0f, 0.65f),
                new(10,  0f, 0.70f),
                new(20,  0f, 0.75f),
                new(30,  0f, 0.85f),
                new(40,  0f, 0.90f),
                new(50,  0.05f, 0.95f),
                new(60,  0.1f, 1f),
                new(70,  0.15f, 1f),
                new(80,  0.2f, 1f),
                new(90,  0.25f, 1f)
            };

        // Customer relation delta*10 (0-50) - probability range of being a drug bust ( result > 0.5 = true )
        public List<MinMaxThreshold> BuyBustProbability = new()
            {
                new(0,   0f, 1f), // worst possible relations 50% of buy bust
                new(5,   0f, 0.9f),
                new(10,  0f, 0.8f),
                new(15,  0f, 0.75f),
                new(20,  0f, 0.65f),
                new(25,  0f, 0.75f),
                new(30,  0f, 0.6f),
                new(40,  0f, 0.55f),
                new(50,  0f, 0.49f) // best possible relations, never a buy bust
            };
    }

    #endregion

}

