using System.Globalization;
using Ares.Domain;

namespace Ares.UI
{
    /// <summary>Child-friendly Portuguese copy. Science attribution is distinct from educational game rules.</summary>
    public static class GreenhouseNarrator
    {
        static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");
        public const string Welcome = "Faça a água durar os 5 dias. Olhe o solo antes de regar.";
        public const string Inspiration = "Cultivo inspirado no Veggie da NASA e nos experimentos VEG-04A e VEG-04B. Frutas, legumes e verduras fazem parte da nossa estufa educativa; o VEG-04 estudou a verdura mizuna.";
        public const string WaterNote = "No VEG-04A, um dos tratamentos usou em média 51 mL por recipiente por dia. Aqui, 10 × 51 = 510 mL por prateleira. O regador, os estados do solo, as doses e os 11 L são regras do jogo, não recomendações de irrigação da NASA.";
        public const string EnvironmentNote = "A estufa é fechada e climatizada. Mudanças de solo e luz vêm de situações internas ou falhas nos equipamentos. O clima de Marte não muda diretamente essas condições.";
        public static string Litres(int ml) => (ml / 1000m).ToString("0.##", PresentationLocalizer.Language=="en"?CultureInfo.InvariantCulture:Portuguese);
        public static string Soil(SoilMoisture value) => value == SoilMoisture.Dry ? "Seco" : value == SoilMoisture.Wet ? "Muito úmido" : "Moderado";
        public static string Light(GrowLight value) => value == GrowLight.Low ? "Pouca" : value == GrowLight.Excessive ? "Excessiva" : "Moderada";
        public static string Health(ShelfHealth value) => value == ShelfHealth.Healthy ? "saudável" : value == ShelfHealth.Problem ? "precisa de cuidado" : "em risco";
        public static string WaterHint(GreenhouseShelf shelf)
        {
            if (shelf.RequiredDoses == 0) return "Não regue. As raízes precisam de ar!";
            int remaining = shelf.RequiredDoses - shelf.DeliveredDoses;
            return remaining == 0 ? "Rega de hoje concluída!" : remaining == 2 ? "Regue 2 vezes, reabastecendo entre elas." : "Falta 1 rega de 510 mL.";
        }
        public static string LightHint(GrowLight light) => light == GrowLight.Low ? "Aumente a luz para ajudar as plantas a crescer." : light == GrowLight.Excessive ? "Diminua a luz para evitar estresse nas plantas." : "A luz está boa. Mantenha assim!";
        public static string Result(GreenhouseResult result) => result == null ? "" : $"Dia {result.Day}: {result.HealthyShelves}/4 prateleiras em boas condições. Água usada: {Litres(result.UsedMl)} L.";
    }
}
