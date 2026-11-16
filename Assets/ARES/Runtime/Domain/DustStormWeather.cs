using System;

namespace Ares.Domain
{
    /// <summary>Simulated gust telemetry, shared by particles and HUD. Independent of graphics quality.</summary>
    public readonly struct DustStormWeather
    {
        public readonly float Strength, SpeedMetersPerSecond;
        public float SpeedKmh => SpeedMetersPerSecond * 3.6f;
        DustStormWeather(float strength,float speed){Strength=strength;SpeedMetersPerSecond=speed;}
        public static DustStormWeather ForCampaign(Campaign campaign)
        {
            if(campaign?.NativeHazards!=true)return new DustStormWeather(campaign?.Demands);
            if(campaign.Phase!=Phase.Tasks || campaign.NativePlan==null)return default;
            foreach(var e in campaign.NativePlan.events)if(e.kind==Hazards.HazardKind.Dust && e.damaging){
                float time=campaign.SolElapsedSeconds-e.startsAt;if(time< -60 || time>e.duration)continue;
                float front=time<0?.15f+.85f*(time+60)/60:Math.Min(1,(e.duration-time)/8);
                float gust=.5f+.3f*(float)Math.Sin(time*1.17f)+.2f*(float)Math.Sin(time*.43f+1.6f);
                return new DustStormWeather(Math.Max(0,Math.Min(1,front)),front*(19+15*gust)*(e.tier==Hazards.HazardTier.High?1.2f:1));
            }return default;
        }
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
