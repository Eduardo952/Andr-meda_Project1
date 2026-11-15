using System;

namespace Ares.Domain
{
    /// <summary>Fisher-Yates draw without replacement. A seed makes regression checks reproducible.</summary>
    public static class DiscoveryDeck
    {
        public static int[] Shuffle(int count, int seed)
        {
            var result = new int[count]; for (int i = 0; i < count; i++) result[i] = i;
            var random = new Random(seed);
            for (int i = count - 1; i > 0; i--)
            { int j = random.Next(i + 1); int value = result[i]; result[i] = result[j]; result[j] = value; }
            return result;
        }
    }
}
