using System;

namespace Ares.Domain.Hazards
{
    public enum CrewCondition { Nominal, Caution, Warning, Incapacitated }
    public enum ConditionCause { Radiation, Hypoxia, Cold, Power, Structure }
    public sealed class PlayerConditionModel
    {
        readonly HazardBalance settings;
        public CrewCondition Stage {get;private set;}
        public ConditionCause Cause {get;private set;}
        public float WarningShownSeconds {get;private set;}
        public float CollapseSeconds {get;private set;}
        public int CollapseCount {get;private set;}
        public bool TreatedThisSol {get;private set;}
        public bool Abort {get;private set;}
        public float SpeedFactor=>Stage==CrewCondition.Incapacitated?0:Stage==CrewCondition.Warning?settings.warningSpeed:TreatedThisSol?settings.treatedSpeed:1;
        public PlayerConditionModel(HazardBalance settings){this.settings=settings;}
        public void Tick(float dt,float dose,float oxygen,float integrity,float cold,bool remedyShown)
        {
            if(Stage==CrewCondition.Incapacitated){CollapseSeconds+=dt;return;}
            bool severe=dose>=100||oxygen<=0||integrity<=0||cold>=settings.coldLedgerSeconds;
            Cause=dose>=settings.cautionDose?ConditionCause.Radiation:oxygen<=settings.cautionOxygen?ConditionCause.Hypoxia:cold>0?ConditionCause.Cold:ConditionCause.Structure;
            CrewCondition next=severe || dose>=settings.warningDose || oxygen<=settings.warningOxygen || integrity<=25?CrewCondition.Warning:
                dose>=settings.cautionDose||oxygen<=settings.cautionOxygen||cold>0||integrity<=50?CrewCondition.Caution:CrewCondition.Nominal;
            if(next==CrewCondition.Warning && remedyShown)WarningShownSeconds+=dt;
            else if(next<CrewCondition.Warning)WarningShownSeconds=0;
            Stage=next;
            if(severe && WarningShownSeconds>=settings.warningGrace) {
                Stage=CrewCondition.Incapacitated;CollapseSeconds=0;CollapseCount++;
                Abort=CollapseCount>=settings.maxMedicalEvacuations;
            }
        }
        public void Recover(){Stage=CrewCondition.Caution;WarningShownSeconds=CollapseSeconds=0;TreatedThisSol=true;}
        public void NextSol(){TreatedThisSol=false;WarningShownSeconds=0;}
    }
}
