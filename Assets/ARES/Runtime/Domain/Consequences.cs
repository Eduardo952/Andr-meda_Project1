using System;
using System.Collections.Generic;
using Ares.Data;

namespace Ares.Domain
{
    [Serializable] public sealed class DustStorm { public bool active, simulated = true, hasWindSpeed; public string source; public float windSpeed; }
    [Serializable] public sealed class SystemResult { public int allocated, demand, missing; public string status; }
    [Serializable] public sealed class SolOutcome
    {
        public SystemResult shield, life, greenhouse;
        public float integrityDelta, oxygenDelta;
    }
    [Serializable] public sealed class ConsequenceCause
    {
        public bool hasAllocation, hasCmeSpeed, hasAirTempMean, hasWindSpeed, hasDust, hasAlarmPenalty;
        public float allocated, demand, missing, cmeSpeed, airTempMean, windSpeed, dust, energyLossPct, alarmPenalty;
        public string flareClass;
    }
    [Serializable] public sealed class ConsequenceEffects { public float integrity, oxygen, dose, nextEnergyLossPct; public int bonusStars; }
    [Serializable] public sealed class ConsequenceEvent
    {
        public string id, resource, severity;
        public bool simulated;
        public ConsequenceCause cause = new ConsequenceCause();
        public ConsequenceEffects effects = new ConsequenceEffects();
    }

    /// <summary>Pure port of engine/consequences.ts. Weather is explicitly simulated; readings keep their provenance.</summary>
    public sealed class Consequences
    {
        readonly CampaignRules rules;
        public Consequences(CampaignRules rules) { this.rules = rules ?? throw new ArgumentNullException(nameof(rules)); }
        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        static float Round(float value) => (float)Math.Floor(value + .5);
        public DustStorm StormForSol(int solIndex, float? windSpeed)
        {
            if (windSpeed.HasValue && !float.IsNaN(windSpeed.Value) && !float.IsInfinity(windSpeed.Value))
                return new DustStorm { active = rules.marsWeatherModifier && windSpeed.Value > rules.weather.windStormThreshold, source = "insight", hasWindSpeed = true, windSpeed = windSpeed.Value };
            bool active = rules.marsWeatherModifier && Array.IndexOf(rules.weather.scriptedStormSols, solIndex + 1) >= 0;
            return new DustStorm { active = active, source = active ? "training" : "none" };
        }
        public float[] PanelDustAfterStorm(IReadOnlyList<float> current, DustStorm storm)
        {
            var dust = new float[4];
            for (int i = 0; i < 4; i++) dust[i] = Clamp((i < current.Count ? current[i] : 0) + (storm.active && i < rules.weather.dustGain.Length ? rules.weather.dustGain[i] : 0), 0, 100);
            return dust;
        }
        public float[] PanelDustAfterCleaning(IReadOnlyList<float> current, DustStorm storm, bool cleaned) => cleaned ? new float[4] : PanelDustAfterStorm(current, storm);
        public int CleaningPasses(float dust) => float.IsNaN(dust) || float.IsInfinity(dust) || dust <= 10 ? 0 : dust <= rules.cleaning.lightMax ? rules.cleaning.lightPasses : dust <= rules.cleaning.heavyMax ? rules.cleaning.heavyPasses : rules.cleaning.buriedPasses;
        public float EnergyLossPct(IReadOnlyList<float> dust)
        {
            float sum = 0; for (int i = 0; i < Math.Min(4, dust.Count); i++) sum += Clamp(dust[i], 0, 100);
            return Math.Min(rules.miniGames.weatherDustCap, Round(sum / 4 * rules.miniGames.weatherDustCap) / 100);
        }
        public int EffectiveEnergyPool(IReadOnlyList<float> dust) => Math.Max(rules.energyPool - (int)rules.miniGames.weatherDustCap, Rules.RoundTo5(rules.energyPool - EnergyLossPct(dust)));
        public string MissingSeverity(int missing, int demand)
        {
            float ratio = demand > 0 ? (float)missing / demand : 0;
            return ratio >= rules.consequences.moderateMissingRatio ? "severe" : ratio >= rules.consequences.minorMissingRatio ? "moderate" : "minor";
        }
        public float DoseGain(int missing, bool shelterMissed) => Math.Max(0, missing) * rules.consequences.dosePerShieldKwh + (shelterMissed ? rules.consequences.shelterMissedDose : 0);
        public string SuccessTier(int integrity, int oxygen, int dose)
        {
            var c = rules.consequences;
            if (integrity >= c.goldResourceMin && oxygen >= c.goldResourceMin && dose < c.goldDoseMax) return "gold";
            if (integrity >= c.silverResourceMin && oxygen >= c.silverResourceMin && dose < c.silverDoseMax) return "silver";
            return "bronze";
        }
        public SystemResult Classify(int allocated, int demand)
        {
            int diff = allocated - demand;
            string status = Math.Abs(diff) <= rules.margin ? "ideal" : diff < 0 ? "deficit" : "surplus";
            return new SystemResult { allocated = allocated, demand = demand, status = status, missing = status == "deficit" ? demand - allocated : 0 };
        }
        public SolOutcome EvaluateSol(Allocation allocation, Allocation demand)
        {
            var result = new SolOutcome { shield = Classify(allocation.shield, demand.shield), life = Classify(allocation.life, demand.life), greenhouse = Classify(allocation.greenhouse, demand.greenhouse) };
            result.integrityDelta = -result.shield.missing * rules.damage.integrityPerShieldKwh;
            result.oxygenDelta = -(result.life.missing * rules.damage.oxygenPerLifeKwh + result.greenhouse.missing * rules.damage.oxygenPerGreenhouseKwh);
            return result;
        }
        static ConsequenceCause AllocationCause(SystemResult system) => new ConsequenceCause { hasAllocation = true, allocated = system.allocated, demand = system.demand, missing = system.missing };
        public ConsequenceEvent[] Evaluate(SolOutcome outcome, ConsequenceFacts facts, bool shelterMissed, int currentDose, IReadOnlyList<float> panelDust, DustStorm storm, bool cleaned)
        {
            var events = new List<ConsequenceEvent>();
            var shield = outcome.shield; var life = outcome.life; var garden = outcome.greenhouse;
            float dose = DoseGain(shield.missing, shelterMissed);
            if (shield.missing > 0 || shelterMissed)
            {
                var cause = AllocationCause(shield);
                cause.flareClass = facts.flareClass; cause.hasCmeSpeed = facts.hasCmeSpeed; cause.cmeSpeed = facts.cmeSpeed;
                cause.hasAlarmPenalty = shelterMissed; cause.alarmPenalty = shelterMissed ? rules.consequences.shelterMissedDose : 0;
                events.Add(new ConsequenceEvent { id = "shield-deficit", resource = "shield", severity = currentDose + dose >= rules.consequences.severeDose ? "severe" : MissingSeverity(shield.missing, shield.demand), cause = cause, effects = new ConsequenceEffects { integrity = outcome.integrityDelta, dose = dose } });
            }
            if (life.missing > 0)
            {
                var cause = AllocationCause(life); cause.hasAirTempMean = facts.hasAirTempMean; cause.airTempMean = facts.airTempMean;
                events.Add(new ConsequenceEvent { id = "life-deficit", resource = "lifeSupport", severity = MissingSeverity(life.missing, life.demand), cause = cause, effects = new ConsequenceEffects { oxygen = -life.missing * rules.damage.oxygenPerLifeKwh } });
            }
            var nextDust = PanelDustAfterCleaning(panelDust, storm, cleaned); float nextLoss = EnergyLossPct(nextDust);
            if (storm.active || cleaned)
            {
                float sum = 0; foreach (float value in nextDust) sum += value;
                events.Add(new ConsequenceEvent { id = cleaned ? "solar-cleaned" : "solar-dust", resource = "solar", severity = cleaned ? "positive" : nextLoss >= 15 ? "severe" : nextLoss >= 8 ? "moderate" : "minor", simulated = true,
                    cause = new ConsequenceCause { hasDust = true, dust = Round(sum / 4), energyLossPct = nextLoss, hasWindSpeed = storm.hasWindSpeed, windSpeed = storm.windSpeed }, effects = new ConsequenceEffects { nextEnergyLossPct = nextLoss } });
            }
            if (garden.missing > 0) events.Add(new ConsequenceEvent { id = "greenhouse-deficit", resource = "greenhouse", severity = MissingSeverity(garden.missing, garden.demand), cause = AllocationCause(garden), effects = new ConsequenceEffects { oxygen = -garden.missing * rules.damage.oxygenPerGreenhouseKwh } });
            if (shield.status == "surplus" || life.status == "surplus" || garden.status == "surplus") events.Add(new ConsequenceEvent { id = "energy-surplus", resource = "observatory", severity = "info" });
            if (shield.status == "ideal" && life.status == "ideal" && garden.status == "ideal") events.Add(new ConsequenceEvent { id = "perfect-sol", resource = "observatory", severity = "positive", effects = new ConsequenceEffects { integrity = rules.taskReward.integrity, oxygen = rules.taskReward.oxygen, bonusStars = rules.consequences.perfectBonusStars } });
            return events.ToArray();
        }
        public static ConsequenceEffects Totals(IEnumerable<ConsequenceEvent> events)
        {
            var total = new ConsequenceEffects();
            foreach (var item in events)
            {
                if (item.id == "perfect-sol") { total.integrity += item.effects.integrity; total.oxygen += item.effects.oxygen; }
                total.dose += item.effects.dose; total.bonusStars += item.effects.bonusStars;
            }
            return total;
        }
    }
}
