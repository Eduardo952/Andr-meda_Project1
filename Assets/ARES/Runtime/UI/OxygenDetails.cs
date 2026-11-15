using System;
using Ares.Domain;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Optional learning and telemetry. Keeps scientific detail out of the main decision flow.</summary>
    internal sealed class OxygenDetails
    {
        readonly Campaign campaign;
        readonly OxygenPlant plant;
        readonly Label rates, totals;
        public OxygenDetails(VisualElement parent, Campaign campaign, Func<bool> canInteract)
        {
            this.campaign = campaign; plant = campaign.OxygenSystem;
            var more = new Foldout { text = "Saiba mais", value = false, name = "oxygen-science" };
            more.AddToClassList("ox-more"); parent.Add(more);
            var lesson = Box(more, "ox-lesson");
            Text(lesson, "De onde vem o oxigênio?", "ox-section");
            Text(lesson, "O gerador usa água e energia para fazer oxigênio. A equipe respira esse oxigênio e solta CO₂. O filtro retira o CO₂ do ar.", "ox-body");
            Text(lesson, "Reforçar produz mais, mas gasta mais água e energia. Poupar gasta menos energia, mas pode baixar a reserva. Compare as barras e observe o tanque depois de escolher.", "ox-body");
            Text(lesson, "A porcentagem mostra quanto há no tanque. Ela não é a quantidade de oxigênio misturada no ar.", "ox-muted");
            var numbers = new Foldout { text = "Ver os números da base", value = false, name = "oxygen-numbers" };
            more.Add(numbers); rates = Text(numbers, "", "ox-body"); totals = Text(numbers, "", "ox-body"); totals.name = "oxygen-details";
            Text(numbers, "g = gramas. 1.000 g = 1 kg. kWh mede energia. As taxas por minuto usam o tempo do jogo.", "ox-muted");
            var science = new Foldout { text = "A ciência por trás do jogo", value = false, name = "oxygen-sources" };
            more.Add(science);
            Text(science, "Na estação espacial, eletricidade separa água em oxigênio e hidrogênio. Outro aparelho retira o CO₂. A água do gerador desta base é separada da água da estufa.", "ox-body");
            Text(science, "A referência da NASA usa cerca de 820 g de oxigênio por pessoa em um dia terrestre com exercícios. Um dia de Marte é um pouco mais longo. Aqui, o dia dura 10 minutos; os tamanhos e gastos dos aparelhos foram escolhidos para o jogo.", "ox-body");
            Text(science, "O MOXIE testou outra maneira: tirar oxigênio do ar de Marte. Ele não é o gerador de água desta base.", "ox-body");
            Source(science, "NASA · Suporte à vida", "https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/", canInteract);
            Source(science, "NASA · Respiração e CO₂", "https://www.nasa.gov/ochmo-tb-004-carbon-dioxide-2/", canInteract);
            Source(science, "NASA · MOXIE em Marte", "https://www.nasa.gov/solar-system/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/", canInteract);
            Text(science, "Projeto educativo independente. A NASA não endossa este jogo.", "ox-muted");
            ScienceLibrary.Build(more, canInteract);
        }
        static void Source(VisualElement parent, string title, string url, Func<bool> canInteract)
        { Button(parent, title + " ↗", () => { if (canInteract()) Application.OpenURL(url); }, "ghost ox-source"); }
        public void Refresh()
        {
            float perMinute = 1000 * 60 / plant.CycleSeconds;
            rates.text = $"AGORA\nGuardado: {plant.ReserveKg:0.00} de {plant.CapacityKg:0.0} kg\n"
                + $"Produção: {plant.Production * perMinute:0} g/min · uso: {plant.Consumption * perMinute:0} g/min\n"
                + $"Equipe ({plant.CrewCount} pessoas): {plant.CrewConsumption * perMinute:0} g/min · pequenas perdas: {plant.HabitatConsumption * perMinute:0} g/min · experimentos: {plant.LaboratoryConsumption * perMinute:0} g/min\n"
                + $"Saldo: {plant.Balance * perMinute:+0;-0;0} g/min · água restante: {plant.WaterKg:0.00} kg\n"
                + $"CO₂ aguardando o filtro: {plant.CarbonDioxideKg * 1000:0.0} g\n"
                + (float.IsPositiveInfinity(plant.RemainingSeconds) ? "A reserva não está diminuindo.\n" : $"Neste ritmo, a reserva dura cerca de {Mathf.CeilToInt(plant.RemainingSeconds / 60)} min de jogo.\n")
                + $"Energia pedida: {plant.EnergyPerCycle * 60 / plant.CycleSeconds:0.0} kWh/min · recebida: {plant.PowerFraction * 100:0}%\nBateria da base: {campaign.Solar.ReserveKwh:0.0} kWh";
            totals.text = $"DESDE O INÍCIO DA MISSÃO\nOxigênio produzido: {plant.TotalProduced:0.000} kg · usado: {plant.TotalConsumed:0.000} kg\n"
                + $"Liberado com tanque cheio: {plant.TotalVented:0.000} kg · necessidade não atendida: {plant.TotalUnserved:0.000} kg\n"
                + $"Água usada: {plant.TotalWaterUsed:0.000} kg · hidrogênio separado: {plant.TotalHydrogenProduced:0.000} kg\n"
                + $"CO₂ produzido: {plant.TotalCo2Produced:0.000} kg · retirado: {plant.TotalCo2Removed:0.000} kg\nEnergia usada: {plant.TotalEnergyKwh:0.00} kWh";
        }
    }
}
