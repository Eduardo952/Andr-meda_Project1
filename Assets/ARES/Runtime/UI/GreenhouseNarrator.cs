using System.Collections.Generic;
using System.Globalization;
using Ares.Domain;

namespace Ares.UI
{
    /// <summary>Curated PT-BR teaching copy; no external AI, live weather or unverified scientific claims.</summary>
    public static class GreenhouseNarrator
    {
        static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");
        public const string Welcome = "Olá, novo cuidador da estufa de Marte! Eu sou o Mestre da Estufa.\nSua missão é cuidar de quatro prateleiras de hortaliças. Use com cuidado os 11 L do reservatório, pois a água em Marte é limitada.";
        public const string Inspiration = "Cultivo inspirado no Veggie, usado pela NASA no espaço. Nesta atividade, cuidamos do solo e da luz de uma estufa imaginária em Marte.";
        public const string WaterNote = "Os níveis de 1,36 L, 2,04 L e 2,32 L são uma adaptação para este jogo. A NASA não definiu esses três níveis para uma estufa de quatro prateleiras.";
        public static string Litres(int ml) => (ml / 1000m).ToString("0.##", Portuguese);
        public static string Soil(SoilMoisture value) => value == SoilMoisture.Dry ? "Seco" : value == SoilMoisture.Wet ? "Úmido" : "Moderado";
        public static string Light(GrowLight value) => value == GrowLight.Low ? "Pouca" : value == GrowLight.Excessive ? "Excessiva" : "Moderada";
        public static string Level(IrrigationLevel value) => value == IrrigationLevel.Low ? "Baixa" : value == IrrigationLevel.High ? "Alta" : "Mediana";
        public static string Health(ShelfHealth value) => value == ShelfHealth.Healthy ? "saudável" : value == ShelfHealth.Problem ? "com problema" : "em risco";
        public static string Weather(GreenhouseWeather value)
            => value == GreenhouseWeather.DustStorm ? "tempestade de poeira" : value == GreenhouseWeather.StrongSun ? "sol forte" : value == GreenhouseWeather.ClearSky ? "céu limpo após a mudança do tempo" : "estável";
        public static string Hint(GreenhouseSimulation game)
        {
            if (game.Soil == SoilMoisture.Dry) return "Hmm, o solo está sequinho... como será que as raízes estão se sentindo?";
            if (game.Soil == SoilMoisture.Wet) return "As raízes também precisam de ar. Como está a água no solo?";
            if (game.Light == GrowLight.Low) return "A poeira escondeu parte da luz. O que ajuda uma planta a crescer?";
            if (game.Light == GrowLight.Excessive) return "Hoje há muita luz! Como será que as folhas estão se sentindo?";
            return game.Cycle % 2 == 0 ? "Cada escolha usa parte da reserva. Como cuidar das plantas e do próximo ciclo?" : "Água e luz ajudam as plantas a crescer. Observe antes de escolher!";
        }
        public static string CycleBlock(GreenhouseSimulation game)
            => $"Ciclo {game.Cycle}\nClima em Marte: {Weather(game.Weather)}\nReservatório: {Litres(game.WaterMl)} L\nPrateleiras: 1 {Health(game.Shelves[0].Health)} | 2 {Health(game.Shelves[1].Health)} | 3 {Health(game.Shelves[2].Health)} | 4 {Health(game.Shelves[3].Health)}\nSolo: {Soil(game.Soil)} | ☀ Luz: {Light(game.Light)}\nDica: {Hint(game)}\nQual nível de irrigação você escolhe: Baixa, Mediana ou Alta? Quer fazer algum ajuste?";
        public static string Result(GreenhouseResult result)
        {
            if (result == null) return "";
            var reasons = new List<string>();
            if(result.Soil == SoilMoisture.Dry) reasons.Add("o solo seco traz risco de desidratação");
            if(result.Soil == SoilMoisture.Wet) reasons.Add(result.Waterlogged ? "o excesso de água piorou e pode prejudicar as raízes" : "o solo muito úmido pode prejudicar as raízes");
            if(result.Light == GrowLight.Low) reasons.Add("pouca luz prejudica o crescimento");
            if(result.Light == GrowLight.Excessive) reasons.Add("luz excessiva traz risco de estresse nas plantas");
            string explanation = reasons.Count == 0 ? "O solo e a luz ficaram no ponto ideal para crescer. A água que sobrou poderá ajudar nos próximos ciclos." : "Neste ciclo, " + string.Join(" e ",reasons) + ". Observar o resultado ajuda na próxima escolha!";
            string outcome = result.Health == ShelfHealth.Healthy ? "As quatro prateleiras estão saudáveis!" : "As quatro prateleiras ficaram " + Health(result.Health) + ". Vamos aprender com essa escolha?";
            return "Resultado: " + outcome + "\nExplicação: " + explanation + $"\nÁgua atualizada: {Litres(result.BeforeMl)} − {Litres(result.UsedMl)} = {Litres(result.RemainingMl)} L.";
        }
        public static string Ending(GreenhouseSimulation game)
            => (game.WaterMl == 0 ? "A água acabou. Nosso cultivo chegou ao fim." : $"Restam {Litres(game.WaterMl)} L. É menos que os 1,36 L do menor nível, então este cultivo chegou ao fim.")
            + $"\nVocê cuidou da estufa por {game.CompletedCycles} ciclos. Em {game.HealthyCycles}, solo e luz ficaram ideais.\nA água é limitada. Pensar no próximo ciclo ajuda a cuidar dela.\nO que você percebeu sobre a água e a luz? Qual escolha tentaria de outro jeito?";
    }
}
