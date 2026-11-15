namespace Ares.Domain
{
    /// <summary>Shared time scale for campaign and life-support calculations.</summary>
    public static class MissionTime
    {
        public const float RealSecondsPerSol = 600;
        public const float MarsSolSeconds = 88775.244f;
        public const float EarthDaySeconds = 86400;
        public const float EarthDaysPerSol = MarsSolSeconds / EarthDaySeconds;
    }
}
