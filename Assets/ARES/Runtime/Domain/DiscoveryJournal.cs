using System;

namespace Ares.Domain
{
    /// <summary>Five mission days; observing never advances the campaign or awards task resources.</summary>
    public sealed class DiscoveryJournal
    {
        public const int Days = 5;
        readonly bool[] observed = new bool[Days];
        public bool CanView(int day, int currentDay) => day >= 0 && day < Days && day <= currentDay;
        public bool Observed(int day) => day >= 0 && day < Days && observed[day];
        public bool Record(int day, int currentDay)
        {
            if (!CanView(day, currentDay)) return false;
            observed[day] = true;
            return true;
        }
    }
}
