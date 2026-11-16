using System;
using Ares.Data;
using Ares.Domain.Hazards;
using Ares.Infrastructure.Nasa;

namespace Ares.Domain
{
    public sealed partial class Campaign
    {
        public bool NativeHazards {get;private set;}
        public bool NativeDataReady {get;private set;}
        public HazardPlan NativePlan {get;private set;}
        public SolResponse NativeResponse {get;private set;}
        public HazardBalance HazardSettings {get;private set;}=new HazardBalance();
        public EnergyGrid Grid {get;private set;}
        public ExposureModel Exposure {get;private set;}=new ExposureModel();
        public PlayerConditionModel Condition {get;private set;}
        public int MedKits {get;private set;} public int OxygenCanisters {get;private set;}
        public bool RadioBlackout {get;private set;} public bool ShelterReached {get;private set;}
        public float ColdExposure {get;private set;} public float DoseRate {get;private set;}
        public bool RestUsed {get;private set;} public int RescueVersion {get;private set;}
        public float TreatmentEnergySpent {get;private set;}
        public float RestProgress {get;private set;}
        public string LastHazard {get;private set;}
        public bool NeedsPanelCleaning=>NativeHazards && impacts.Contains("dust") && Solar.DirtyPanels>0;
        bool inside=true,sheltered,remedyVisible,resting;float integrityRemainder,doseRemainder;
        readonly System.Collections.Generic.HashSet<string> impacts=new System.Collections.Generic.HashSet<string>();
        public void SetExposureContext(bool interior,bool shelter,bool warningVisible,bool inBed){inside=interior;sheltered=shelter;remedyVisible=warningVisible;resting=inBed;if(shelter)ShelterReached=true;}
        public void EnableNativeHazards(HazardBalance settings=null)
        {
            NativeHazards=true;NativeDataReady=false;HazardSettings=settings??new HazardBalance();
            Solar.ConfigureGrid(HazardSettings.batteryCapacity,HazardSettings.batteryStart);Grid=new EnergyGrid(Solar,HazardSettings);
            Condition=new PlayerConditionModel(HazardSettings);MedKits=HazardSettings.medKits;OxygenCanisters=HazardSettings.oxygenCanisters;
            Demands.SyncNative(null,0);Changed?.Invoke();
        }
        public bool ApplyNativeSol(SolResponse response,string provenance,string mode)
        {
            if(!NativeHazards || response==null || response.sol!=SolIndex+1 || Phase!=Phase.Report && Phase!=Phase.Plan)return false;
            NativeResponse=response;NativePlan=HazardPlanner.Build(response,HazardSettings,provenance,mode);
            var current=Current;current.date=response.earth_date;current.raw=NasaJson.Write(response);current.native=true;current.sample=provenance=="sample";
            current.demand=new Allocation {shield=NativePlan.shieldDemand,life=NativePlan.lifeDemand,greenhouse=NativePlan.greenhouseDemand};
            var f=response.facts;current.facts=new ConsequenceFacts {flareClass=f?.flare==null?null:f.flare.@class+f.flare.magnitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                hasCmeSpeed=f?.cme!=null,cmeSpeed=(float)(f?.cme?.speed_kms??0),hasAirTempMean=f?.weather!=null,airTempMean=(float)(f?.weather?.temp_avg_c??0),hasWindSpeed=false};
            current.thermalMissing=f?.weather==null;current.alarm=null;
            current.tasks=MissionDirector.Tasks(NativePlan,response);
            Allocation=default;NativeDataReady=true;Demands.SyncNative(NativePlan,0);Changed?.Invoke();return true;
        }
        public bool UseMedKit()
        {
            if(!NativeHazards || MedKits<=0 || Condition.Stage==CrewCondition.Incapacitated || Dose<=0)return false;
            MedKits--;Dose=Rules.ApplyDelta(Dose,-HazardSettings.medKitDose);Condition.Tick(0,Dose,Oxygen,Integrity,ColdExposure,false);Changed?.Invoke();return true;
        }
        public bool UseOxygenCanister()
        {
            if(!NativeHazards || OxygenCanisters<=0 || Condition.Stage==CrewCondition.Incapacitated || Oxygen>=100)return false;
            OxygenCanisters--;OxygenSystem.ApplyPercentDelta(HazardSettings.canisterOxygen);UpdateOxygenDisplay();Changed?.Invoke();return true;
        }
        public bool RepairGrid(){if(Grid?.OfflineSystem==null)return false;Grid.Repair();Changed?.Invoke();return true;}
        Allocation TreatmentAllocation=>new Allocation {shield=Allocation.shield,life=Math.Max(0,Allocation.life-(int)TreatmentEnergySpent),greenhouse=Allocation.greenhouse};
        void Charge(ResourcePenalty loss)
        {
            integrityRemainder-=loss.integrity;doseRemainder+=loss.dose;
            int di=(int)Math.Truncate(integrityRemainder),dd=(int)Math.Truncate(doseRemainder);integrityRemainder-=di;doseRemainder-=dd;
            Integrity=Rules.ApplyDelta(Integrity,di);Dose=Rules.ApplyDelta(Dose,dd);OxygenSystem.ApplyPercentDelta(-loss.oxygen);UpdateOxygenDisplay();
        }
        void TickNative(float seconds)
        {
            while(seconds>0 && Phase==Phase.Tasks) {
                float dt=Math.Min(1,seconds);seconds-=dt;
                if(Condition.Stage==CrewCondition.Incapacitated) {
                    Condition.Tick(dt,Dose,Oxygen,Integrity,ColdExposure,remedyVisible);
                    if(Condition.Abort){FailureCause=Condition.Cause.ToString().ToLowerInvariant();Phase=Phase.GameOver;Changed?.Invoke();return;}
                    if(Condition.CollapseSeconds>=HazardSettings.collapseAnimationSeconds+HazardSettings.rescueAnimationSeconds)RecoverCrew();
                    continue;
                }
                if(SolElapsedSeconds<MissionTime.RealSecondsPerSol) {
                    SolElapsedSeconds=Math.Min(MissionTime.RealSecondsPerSol,SolElapsedSeconds+dt);Grid.Tick(dt);Demands.SyncNative(NativePlan,SolElapsedSeconds);
                    RadioBlackout=false;DoseRate=0;bool cold=false;
                    foreach(var hazard in NativePlan.events) {
                        if(!hazard.damaging || SolElapsedSeconds<hazard.startsAt || SolElapsedSeconds>hazard.startsAt+hazard.duration)continue;
                        LastHazard=hazard.id;
                        if(impacts.Add(hazard.id)) {
                            if((hazard.kind==HazardKind.Flare || hazard.kind==HazardKind.Cme) && !Demands.ShieldRegulated)Grid.Shock(hazard,Allocation,Current.demand);
                            if(hazard.kind==HazardKind.Dust)Solar.Deposit(hazard.tier==HazardTier.High?HazardSettings.dustDepositHigh:HazardSettings.dustDepositMedium);
                            if(hazard.kind==HazardKind.Radiation && !ShelterReached)ShelterMissed=true;
                        }
                        if(hazard.kind==HazardKind.Flare)RadioBlackout=SolElapsedSeconds<hazard.startsAt+(hazard.tier==HazardTier.High?HazardSettings.commHighSeconds:HazardSettings.commMediumSeconds);
                        if(hazard.kind==HazardKind.Radiation) {
                            float protection=sheltered?.9f:inside?(Allocation.shield>=Current.demand.shield?.6f:.2f):0;
                            DoseRate+=HazardSettings.radiationDosePerMinute*(int)hazard.tier*hazard.intensity*(1-protection);
                        }
                        cold|=hazard.kind==HazardKind.Cold;
                    }
                    Communications.TransientBlackout=RadioBlackout;
                    // Drop the lowest-priority allocation if real deposition reduced current generation.
                    int excess=Math.Max(0,Allocation.Total-EnergyPool);var a=Allocation;
                    int shed=Math.Min(a.greenhouse,excess);a.greenhouse-=shed;excess-=shed;shed=Math.Min(a.life,excess);a.life-=shed;excess-=shed;a.shield=Math.Max(0,a.shield-excess);Allocation=a;
                    float beforeProduced=OxygenSystem.TotalProduced;
                    OxygenSystem.Tick(dt,Grid.OfflineSystem=="life"?0:OxygenSystem.EnergyPerCycle*dt/OxygenSystem.CycleSeconds);
                    float oxygenGap=Grid.OfflineSystem=="life"?OxygenSystem.NominalProduction*dt/OxygenSystem.CycleSeconds/OxygenSystem.CapacityKg*100:0;
                    var extra=Exposure.Extra(oxygenGap,Grid.OfflineSystem!=null?HazardSettings.blackoutIntegrityCap/HazardSettings.eventDuration*dt:0,DoseRate/60*dt,HazardSettings);
                    // Loss of production already changed the same oxygen tank; never charge it twice.
                    if(oxygenGap>extra.oxygen){OxygenSystem.ApplyPercentDelta(oxygenGap-extra.oxygen);Grid.Repair();}
                    extra.oxygen=0;Charge(extra);
                    if(cold && Allocation.life<Current.demand.life-5)ColdExposure+=dt*(Current.demand.life-Allocation.life)/Math.Max(1,Current.demand.life);
                    else ColdExposure=Math.Max(0,ColdExposure-dt*.5f);
                    var total=ExposureModel.AllocationPenalty(Grid.Effective(TreatmentAllocation,Current.demand,false),Current.demand,false);
                    Charge(Exposure.Advance(total,SolElapsedSeconds/MissionTime.RealSecondsPerSol,ShelterMissed?35:0));
                    if(resting && !RestUsed && Allocation.life-TreatmentEnergySpent>=HazardSettings.restEnergy) {
                        RestProgress+=dt;
                        if(RestProgress>=HazardSettings.restSeconds){RestUsed=true;TreatmentEnergySpent+=HazardSettings.restEnergy;Dose=Rules.ApplyDelta(Dose,-HazardSettings.restDose);OxygenSystem.ApplyPercentDelta(HazardSettings.restOxygen);ColdExposure=0;UpdateOxygenDisplay();Changed?.Invoke();}
                    }else if(!resting)RestProgress=0;
                    SolFraction=(rules.startSolFraction+SolElapsedSeconds/MissionTime.RealSecondsPerSol)%1;
                    Alarm=Demands.SolarActivity?"solar-flare":Demands.DustInAir?"dust-storm":null;
                }else EndedByClock=true;
                var previous=Condition.Stage;Condition.Tick(dt,Dose,Oxygen,Integrity,ColdExposure,remedyVisible);
                if(previous!=Condition.Stage){if(Condition.Stage==CrewCondition.Incapacitated){Watering.Stop();OpenTask=null;BonusStars=Math.Max(0,BonusStars-rules.consequences.perfectBonusStars);}Changed?.Invoke();}
            }
        }
        void RecoverCrew()
        {
            Dose=(int)HazardSettings.treatedDose;Integrity=Math.Max(Integrity,(int)HazardSettings.treatedIntegrity);
            OxygenSystem.SetReservePercent(HazardSettings.treatedOxygen);UpdateOxygenDisplay();ColdExposure=0;Grid.Repair();
            SolElapsedSeconds=Math.Min(MissionTime.RealSecondsPerSol,SolElapsedSeconds+HazardSettings.rescueSeconds);
            Condition.Recover();RescueVersion++;Changed?.Invoke();
        }
        SolOutcome SettleNative()
        {
            var effective=Grid.Effective(TreatmentAllocation,Current.demand,true);
            var total=ExposureModel.AllocationPenalty(effective,Current.demand,ShelterMissed);Charge(Exposure.Settle(total));
            var outcome=consequences.EvaluateSol(effective,Current.demand);Grid.StoreSurplus(Allocation,Current.demand);
            return outcome;
        }
        void NextNativeSol()
        {
            NativeDataReady=false;NativePlan=null;NativeResponse=null;Exposure=new ExposureModel();impacts.Clear();
            ShelterReached=ShelterMissed=RestUsed=RadioBlackout=false;Communications.TransientBlackout=false;ColdExposure=RestProgress=TreatmentEnergySpent=0;Grid.Repair();Condition.NextSol();
        }
        static CampaignData CloneCampaign(CampaignData source)
        {
            var copy=new CampaignData {sols=new SolData[source.sols.Length]};
            for(int i=0;i<copy.sols.Length;i++) {
                var s=source.sols[i];var tasks=new Assignment[s.tasks.Length];for(int t=0;t<tasks.Length;t++)tasks[t]=new Assignment {id=s.tasks[t].id,room=s.tasks[t].room};
                copy.sols[i]=new SolData {sol=s.sol,date=s.date,capturedAt=s.capturedAt,alarm=s.alarm,raw=s.raw,sample=s.sample,thermalMissing=s.thermalMissing,demand=s.demand,tasks=tasks,facts=s.facts};
            }
            return copy;
        }
    }
}
