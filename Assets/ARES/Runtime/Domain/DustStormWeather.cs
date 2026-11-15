using System;

namespace Ares.Domain
{
    /// <summary>Simulated gust telemetry, shared by particles and HUD. Independent of graphics quality.</summary>
    public readonly struct DustStormWeather
    {
        public readonly float Strength, SpeedMetersPerSecond;
        public float SpeedKmh => SpeedMetersPerSecond * 3.6f;
        public DustStormWeather(ConflictingDemands demands)
        {
            Strength = SpeedMetersPerSecond = 0;
            if (demands == null || !demands.DustInAir) return;
            float elapsed = demands.Warning ? ConflictingDemands.DustWarningSeconds - demands.RemainingSeconds
                : ConflictingDemands.DustWarningSeconds + ConflictingDemands.ImpactSeconds - demands.RemainingSeconds;
            float front = demands.Warning ? .15f + .85f * elapsed / ConflictingDemands.DustWarningSeconds
                : Math.Min(1, demands.RemainingSeconds / 8f);
            float gust = .5f + .3f * (float)Math.Sin(elapsed * 1.17f) + .2f * (float)Math.Sin(elapsed * .43f + 1.6f);
            Strength = Math.Max(0, Math.Min(1, front));
            SpeedMetersPerSecond = Strength * (19 + 15 * gust);
        }
    }
}
