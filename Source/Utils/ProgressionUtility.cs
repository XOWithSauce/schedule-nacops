namespace NACopsV1
{

    #region Utils for progression
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
    public static class ThresholdMappings
    {
        // Days total
        public static readonly List<MinMaxThreshold> LethalCopFreq = new()
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
        public static readonly List<MinMaxThreshold> LethalCopRange = new()
            {
                new(0,      2f, 6f),
                new(8000,   3f, 8f),
                new(30000,  4f, 9f),
                new(100000, 6f, 10f),
                new(300000, 7f, 12f),
                new(600000, 9f, 14f),
                new(1000000, 10f, 15f),

            };

        // Days total
        public static readonly List<MinMaxThreshold> CrazyCopsFreq = new()
            {
                new(0,   300f, 450f),
                new(5,   300f, 400f),
                new(10,  200f, 350f),
                new(20,  150f, 350f),
                new(30,  150f, 300f),
                new(40,  100f, 300f),
                new(50,  100f, 250f),
            };
        // Networth - min distance max distance rand range
        public static readonly List<MinMaxThreshold> CrazyCopsRange = new()
            {
                new(0,      10f, 30f),
                new(8000,   15f, 30f),
                new(30000,  20f, 35f),
                new(100000, 25f, 35f),
                new(300000, 30f, 40f),
                new(500000, 30f, 40f),
                new(1000000, 30f, 45f),
                new(3000000, 30f, 50f),
            };

        // Days total
        public static readonly List<MinMaxThreshold> NearbyCrazThres = new()
            {
                new(0,   400f, 650f),
                new(5,   300f, 600f),
                new(10,  120f, 500f),
                new(20,  120f, 500f),
                new(30,  120f, 400f),
                new(40,  120f, 350f),
                new(50,  120f, 300f),
            };
        // Networth - min distance max distance rand range
        public static readonly List<MinMaxThreshold> NearbyCrazRange = new()
            {
                new(0,      10f, 15f),
                new(8000,   10f, 20f),
                new(30000,  10f, 25f),
                new(100000, 20f, 35f),
                new(300000, 20f, 40f),
                new(500000, 25f, 40f),

            };

        // Networth
        public static readonly List<MinMaxThreshold> PIThres = new()
            {
                new(0,        880f, 1800f),
                new(9000,     700f, 1600f),
                new(30000,    600f, 1300f),
                new(50000,    450f, 1100f),
                new(80000,    450f, 1000f),
                new(300000,   400f, 900f),
                new(500000,   420f, 700f),
                new(900000,   420f, 650f),
                new(1500000,  400f, 600f),
                new(8000000,  350f, 500f),
            };
        // Days total - probability range of toggling attn ( result > 0.5 = true )
        public static readonly List<MinMaxThreshold> PICurfewAttn = new()
            {
                new(0,   0f, 0.5f),
                new(5,   0f, 0.5f),
                new(10,  0f, 0.55f),
                new(20,  0f, 0.55f),
                new(30,  0f, 0.55f),
                new(40,  0f, 0.60f),
                new(50,  0f, 0.62f),
                new(60,  0f, 0.64f),
                new(70,  0f, 0.66f),
                new(80,  0f, 0.68f),
                new(90,  0f, 0.70f),
            };

        // Days total - probability range of snitching a sample ( result > 0.8 = true )
        public static readonly List<MinMaxThreshold> SnitchProbability = new()
            {
                new(0,   0f, 0.8f),
                new(5,   0f, 0.85f),
                new(10,  0f, 0.88f),
                new(20,  0f, 0.9f),
                new(30,  0f, 0.93f),
                new(40,  0f, 0.95f),
                new(50,  0.05f, 1f),
                new(60,  0.1f, 1f),
                new(70,  0.15f, 1f),
                new(80,  0.2f, 1f),
                new(90,  0.25f, 1f),
            };

        // Customer relation delta*10 (0-50) - probability range of being a drug bust ( result > 0.5 = true )
        public static readonly List<MinMaxThreshold> BuyBustProbability = new()
            {
                new(0,   0.1f, 1f),
                new(5,   0.05f, 0.9f),
                new(10,  0f, 0.8f),
                new(15,  0f, 0.75f),
                new(20,  0f, 0.65f),
                new(30,  0f, 0.6f),
                new(40,  0f, 0.55f),
            };
    }

    #endregion

}

