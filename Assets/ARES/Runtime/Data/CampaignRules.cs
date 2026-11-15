using System;

namespace Ares.Data
{
    // Serialized directly from reference config/balance.ts; no second balance table.
    [Serializable] public sealed class CampaignRules
    {
        public int initialIntegrity, initialOxygen, energyPool, sliderStep, margin, campaignSols;
        public float realSecondsPerSol, startSolFraction, shelterSeconds, taskSeconds;
        public bool marsWeatherModifier;
        public WeatherRules weather;
        public ConsequenceRules consequences;
        public CleaningRules cleaning;
        public DamageRules damage;
        public RewardRules taskReward;
        public MiniGameRules miniGames;
    }
    [Serializable] public sealed class WeatherRules { public float windStormThreshold; public int[] scriptedStormSols; public float[] dustGain; }
    [Serializable] public sealed class ConsequenceRules
    {
        public float minorMissingRatio, moderateMissingRatio, dosePerShieldKwh, shelterMissedDose, severeDose;
        public int perfectBonusStars, goldResourceMin, silverResourceMin, goldDoseMax, silverDoseMax;
    }
    [Serializable] public sealed class CleaningRules { public float lightMax, heavyMax, animationSeconds; public int lightPasses, heavyPasses, buriedPasses; }
    [Serializable] public sealed class DamageRules { public float integrityPerShieldKwh, oxygenPerLifeKwh, oxygenPerGreenhouseKwh; }
    [Serializable] public sealed class RewardRules { public int integrity, oxygen; }
    [Serializable] public sealed class MiniGameRules
    {
        public float threeStarSeconds, twoStarSeconds, weatherDustCap;
        public int twoStarMistakes, dialCount, dialSteps, wireCount, pipeCount, beds, apples, lightTarget, dustPatches, tiltStepDegrees, speedStep, speedTolerance;
        public int[] dialTargets, coilMarks;
        public float[] coilMotionSeconds;
    }
    [Serializable] public sealed class ConsequenceFacts
    {
        public string flareClass;
        public bool hasCmeSpeed, hasAirTempMean, hasWindSpeed;
        public float cmeSpeed, airTempMean, windSpeed;
    }
}
