using System;
using Ares.Data;

namespace Ares.Domain.Hazards
{
    /// <summary>Uses SolarPlant's existing battery. Budget allocation remains in 5-kWh steps.</summary>
    public sealed class EnergyGrid
    {
        readonly SolarPlant battery;readonly HazardBalance settings;
        public float BrownoutRemaining {get;private set;}
        public string OfflineSystem {get;private set;}
        public int ShocksAbsorbed {get;private set;}
        public EnergyGrid(SolarPlant battery,HazardBalance settings){this.battery=battery;this.settings=settings;}
        public static int Pool(HazardPlan plan,float dustPercent)=>Rules.RoundTo5(Math.Max(0,100*(plan?.solarFactor??1)-Math.Min(20,dustPercent*.2f)));
        public Allocation Effective(Allocation allocation,Allocation demand,bool commit)
        {
            float reserve=battery.ReserveKwh;float used=0;
            int Cover(int given,int needed) {
                int missing=needed-given;if(missing<=5)return given;
                int supplied=(int)Math.Min(reserve,missing);reserve-=supplied;used+=supplied;return given+supplied;
            }
            var result=new Allocation {shield=Cover(allocation.shield,demand.shield),life=Cover(allocation.life,demand.life),greenhouse=Cover(allocation.greenhouse,demand.greenhouse)};
            if(commit)battery.DrawEnergy(used);return result;
        }
        public void StoreSurplus(Allocation a,Allocation d)=>battery.StoreEnergy(Math.Max(0,a.shield-d.shield-5)+Math.Max(0,a.life-d.life-5)+Math.Max(0,a.greenhouse-d.greenhouse-5));
        public bool Shock(HazardEvent source,Allocation a,Allocation d)
        {
            if(a.shield>=d.shield){ShocksAbsorbed++;return true;}
            float load=source.kind==HazardKind.Cme?Math.Min(settings.cmeShockMax,source.intensity*settings.cmeShockMax):source.tier==HazardTier.High?settings.surgeHigh:settings.surgeMedium;
            if(battery.DrawEnergy(load)+.001f>=load){ShocksAbsorbed++;return true;}
            OfflineSystem=a.greenhouse>0?"greenhouse":"life";BrownoutRemaining=source.duration;return false;
        }
        public void Tick(float dt){BrownoutRemaining=Math.Max(0,BrownoutRemaining-dt);if(BrownoutRemaining==0)OfflineSystem=null;}
        public void Repair(){BrownoutRemaining=0;OfflineSystem=null;}
    }
    public struct ResourcePenalty
    {
        public float integrity,oxygen,dose;
        public static ResourcePenalty operator +(ResourcePenalty a,ResourcePenalty b)=>new ResourcePenalty {integrity=a.integrity+b.integrity,oxygen=a.oxygen+b.oxygen,dose=a.dose+b.dose};
        public static ResourcePenalty operator -(ResourcePenalty a,ResourcePenalty b)=>new ResourcePenalty {integrity=a.integrity-b.integrity,oxygen=a.oxygen-b.oxygen,dose=a.dose-b.dose};
        public ResourcePenalty Scale(float f)=>new ResourcePenalty {integrity=integrity*f,oxygen=oxygen*f,dose=dose*f};
    }
    /// <summary>Final allocation is authoritative. Signed settlement refunds early exposure after successful reallocation.</summary>
    public sealed class ExposureModel
    {
        public ResourcePenalty Applied {get;private set;}
        public ResourcePenalty Additional {get;private set;}
        public static ResourcePenalty AllocationPenalty(Allocation a,Allocation d,bool shelterMissed)=>new ResourcePenalty {
            integrity=Rules.Missing(a.shield,d.shield),oxygen=Rules.Missing(a.life,d.life)+.5f*Rules.Missing(a.greenhouse,d.greenhouse),
            dose=.8f*Rules.Missing(a.shield,d.shield)+(shelterMissed?35:0)};
        public ResourcePenalty Advance(ResourcePenalty total,float fraction,float immediateDose=0)
        {
            var target=total.Scale(Math.Max(0,Math.Min(1,fraction)));target.dose+=immediateDose;var delta=target-Applied;Applied=target;return delta;
        }
        public ResourcePenalty Settle(ResourcePenalty total){var remaining=total-Applied;Applied=total;return remaining;}
        public ResourcePenalty Extra(float oxygen,float integrity,float dose,HazardBalance b)
        {
            var value=new ResourcePenalty {oxygen=Math.Min(Math.Max(0,b.blackoutOxygenCap-Additional.oxygen),oxygen),integrity=Math.Min(Math.Max(0,b.blackoutIntegrityCap+b.coldIntegrityCap-Additional.integrity),integrity),dose=Math.Min(Math.Max(0,b.outdoorDoseCap-Additional.dose),dose)};
            Additional+=value;return value;
        }
    }
}
