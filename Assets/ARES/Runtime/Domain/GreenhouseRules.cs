using System;

namespace Ares.Domain
{
    /// <summary>Educational constants, not NASA irrigation requirements. All accounting uses integer mL.</summary>
    public static class GreenhouseRules
    {
        public const int MissionDays = 5, ShelfCount = 4, ContainersPerShelf = 10;
        public const int ReferenceMlPerContainer = 51;
        public const int DoseMl = ReferenceMlPerContainer * ContainersPerShelf;
        public const int InitialWaterMl = 11000;
        public static int Doses(SoilMoisture soil) => soil == SoilMoisture.Dry ? 2 : soil == SoilMoisture.Moderate ? 1 : 0;
        public static int DailyMl(SoilMoisture soil) => ShelfCount * Doses(soil) * DoseMl;
    }

    /// <summary>Internal habitat events only. External Martian weather never sets greenhouse soil or light.</summary>
    public sealed class GreenhouseDayConditions
    {
        public string Description { get; }
        public GrowLight Light { get; }
        readonly SoilMoisture[] soil;
        public SoilMoisture Soil(int shelf) => soil[shelf];
        public GreenhouseDayConditions(string description, GrowLight light, params SoilMoisture[] soil)
        {
            if (soil == null || soil.Length != GreenhouseRules.ShelfCount) throw new ArgumentException("Four shelf conditions required.");
            Description = description; Light = light; this.soil = (SoilMoisture[])soil.Clone();
        }
        public static GreenhouseDayConditions ForDay(int day)
        {
            var m = SoilMoisture.Moderate; var d = SoilMoisture.Dry; var w = SoilMoisture.Wet;
            switch (day)
            {
                case 2: return new GreenhouseDayConditions("Ventilação desigual e uma falha nas lâmpadas.", GrowLight.Low, d, w, m, m);
                case 3: return new GreenhouseDayConditions("O controlador deixou as lâmpadas fortes demais.", GrowLight.Excessive, m, m, m, m);
                case 4: return new GreenhouseDayConditions("A circulação interna mudou a umidade das prateleiras.", GrowLight.Moderate, w, d, d, w);
                default: return new GreenhouseDayConditions("Ambiente interno estável.", GrowLight.Moderate, m, m, m, m);
            }
        }
    }
}
