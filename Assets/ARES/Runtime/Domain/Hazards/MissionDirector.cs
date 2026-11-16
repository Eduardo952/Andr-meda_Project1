using System;
using System.Collections.Generic;
using Ares.Data;
using Ares.Infrastructure.Nasa;

namespace Ares.Domain.Hazards
{
    public static class MissionDirector
    {
        public static Assignment[] Tasks(HazardPlan plan,SolResponse response)=>new[]{
            new Assignment {room="observatory",id=response.facts?.cme!=null?"CmeCountdown":response.facts?.flare!=null?"SortFlares":"PhotoDetective"},
            new Assignment {room="shield",id=plan.Has(HazardKind.Cme)?"WireShield":"AlignCoils"},
            new Assignment {room="lifeSupport",id=response.facts?.weather!=null?"ThermostatDial":"PipeFlow"},
            new Assignment {room="greenhouse",id=plan.Has(HazardKind.Dust)?"IrrigationPlan":"SmartFarmCare"},
            new Assignment {room="solar",id="TiltSun"} // Actual cleaning remains the existing 32-panel tool, never a substitute puzzle.
        };
        public static Allocation Recommend(int pool,Allocation demand,float battery)
        {
            double best=double.MaxValue;Allocation result=default;
            for(int shield=0;shield<=pool;shield+=5)for(int life=0;life<=pool-shield;life+=5){
                var a=new Allocation {shield=shield,life=life,greenhouse=pool-shield-life};float reserve=battery;
                int Missing(int allocated,int needed){int deficit=Rules.Missing(allocated,needed);if(deficit==0)return 0;int draw=(int)Math.Min(deficit,reserve);reserve-=draw;return Rules.Missing(allocated+draw,needed);}
                int s=Missing(a.shield,demand.shield),l=Missing(a.life,demand.life),g=Missing(a.greenhouse,demand.greenhouse);
                double cost=s*2.5+l+g*.5+Math.Abs(a.shield-demand.shield)*.001+Math.Abs(a.life-demand.life)*.0001;
                if(cost<best){best=cost;result=a;}
            }return result;
        }
    }
    public static class ScenarioSolvability
    {
        // Conservative admission estimate; final acceptance is the separate campaign simulator.
        public static bool Accepts(IReadOnlyList<SolResponse> days,HazardBalance settings=null)
        {
            var b=settings??new HazardBalance();float integrity=100,oxygen=100,dose=0,reserve=b.batteryStart;int kits=b.medKits,cans=b.oxygenCanisters;
            foreach(var day in days){
                var p=HazardPlanner.Build(day,b);var d=new Allocation {shield=p.shieldDemand,life=p.lifeDemand,greenhouse=p.greenhouseDemand};
                int pool=EnergyGrid.Pool(p,0);var a=MissionDirector.Recommend(pool,d,reserve);
                var plant=new SolarPlant(new SolarMaintenanceSettings());plant.ConfigureGrid(b.batteryCapacity,reserve);var grid=new EnergyGrid(plant,b);
                var loss=ExposureModel.AllocationPenalty(grid.Effective(a,d,true),d,false);reserve=plant.ReserveKwh;
                integrity=Math.Min(100,integrity+10)-loss.integrity;oxygen=Math.Min(100,oxygen+10)-loss.oxygen;
                dose+=loss.dose+(p.Has(HazardKind.Radiation)?4:0);
                if(dose>b.cautionDose && kits>0){dose=Math.Max(0,dose-b.medKitDose);kits--;}
                if(oxygen<b.cautionOxygen && cans>0){oxygen=Math.Min(100,oxygen+b.canisterOxygen);cans--;}
                if(integrity<=15||oxygen<=15||dose>=90)return false;
            }return true;
        }
    }
}
