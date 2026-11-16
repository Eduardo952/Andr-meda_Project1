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
        /// <summary>Least recently viewed first, with no image in its previous mission's slot.
        /// Backtracking is bounded by the five-card offline collection (at most 120 permutations).</summary>
        public static int[] FreshOrder(int count,int seed,int[] previous,int[] seen)
        {
            var candidates=Shuffle(count,seed);var rank=new int[count];var tie=new int[count];
            for(int i=0;i<count;i++)tie[candidates[i]]=i;
            if(seen!=null)for(int i=0;i<seen.Length;i++)if(seen[i]>=0&&seen[i]<count)rank[seen[i]]=i+1;
            Array.Sort(candidates,(a,b)=>rank[a]!=rank[b]?rank[a].CompareTo(rank[b]):tie[a].CompareTo(tie[b]));
            var result=new int[count];var used=new bool[count];
            bool Place(int slot){if(slot==count)return true;foreach(int value in candidates){if(used[value] || count>1&&previous!=null&&slot<previous.Length&&previous[slot]==value)continue;used[value]=true;result[slot]=value;if(Place(slot+1))return true;used[value]=false;}return false;}
            return Place(0)?result:Shuffle(count,seed);
        }
    }
}
