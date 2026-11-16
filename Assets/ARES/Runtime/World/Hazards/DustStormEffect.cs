using Ares.Domain;
using Ares.Domain.Hazards;

namespace Ares.World
{
    /// <summary>The existing MartianWind renderer remains the Medium visual baseline.
    /// This adapter supplies tier and explicitly simulated direction without changing its meshes/shader/fog.</summary>
    public static class DustStormEffect
    {
        public static float Sample(Campaign campaign,out float heading)
        {
            heading=0;if(campaign?.NativePlan==null || campaign.Phase!=Phase.Tasks)return 0;
            foreach(var e in campaign.NativePlan.events)if(e.kind==HazardKind.Dust && e.damaging && campaign.SolElapsedSeconds>=e.startsAt-60 && campaign.SolElapsedSeconds<=e.startsAt+e.duration){heading=e.windHeading;return e.tier==HazardTier.High?1.25f:e.tier==HazardTier.Medium?1:.55f;}
            return 0;
        }
    }
}
