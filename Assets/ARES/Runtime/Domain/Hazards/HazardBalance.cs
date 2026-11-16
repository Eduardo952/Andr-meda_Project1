using System;

namespace Ares.Domain.Hazards
{
    /// <summary>New gameplay constants are PROVISIONAL. Existing GDD demand formula/steps remain in Rules.</summary>
    [Serializable] public sealed class HazardBalance
    {
        public float solarFloor=.8f, coldCaution=-65, coldWarning=-80, coldEmergency=-95;
        public int greenhouseDustMedium=5, greenhouseDustHigh=10;
        public float warningGrace=15, shelterDeadline=60, eventDuration=90, commMediumSeconds=45, commHighSeconds=75;
        public float batteryCapacity=30, batteryStart=10, surgeMedium=4, surgeHigh=8, cmeShockMax=10;
        public float blackoutOxygenCap=12, blackoutIntegrityCap=8, outdoorDoseCap=20, coldIntegrityCap=6;
        public float cautionDose=50, warningDose=75, cautionOxygen=40, warningOxygen=25;
        public float warningSpeed=.75f, treatedSpeed=.85f, treatedDose=60, treatedOxygen=60, treatedIntegrity=45;
        public int medKits=2, oxygenCanisters=2, maxMedicalEvacuations=3;
        public float medKitDose=25, canisterOxygen=25, restSeconds=60, restDose=25, restOxygen=20, restEnergy=5, rescueSeconds=75;
        public float collapseAnimationSeconds=4, rescueAnimationSeconds=5, pulseHz=1, dustDepositMedium=.35f, dustDepositHigh=.55f;
        public float lowCmeSpeed=500, highCmeSpeed=1000, radiationDosePerMinute=8, coldLedgerSeconds=300;
        public int candidatesPerSlot=10, maxRepicksPerSlot=20, catalogShortlistPerYear=6, catalogRequestConcurrency=3;
        public float minEventSeconds=60, latestEventSeconds=450, tierDurationJitter=.15f;
        public float scenarioWaitSeconds=60;
    }
}
