using Ares.Domain;
using UnityEngine;

namespace Ares.UI
{
    internal static class MissionEventCopy
    {
        public const string ShieldTitle = "Regular Escudo Eletromagnético";
        public static string Status(Campaign campaign)
        {
            var demand = campaign.Demands;
            if (demand.Warning)
            {
                int seconds = Mathf.CeilToInt(demand.RemainingSeconds);
                if (demand.Event == DemandEvent.DustStorm)
                    return $"Tempestade de poeira em {seconds} s. Prepare a vassoura no parque solar!";
                return demand.ShieldRegulated
                    ? $"Erupção solar em {seconds} s. Escudo regulado! A antena ainda poderá sofrer danos."
                    : $"Erupção solar em {seconds} s! Vá ao escudo e regule a proteção para sobreviver.";
            }
            if (campaign.Solar.BuriedPanels > 0)
                return campaign.Solar.ReserveKwh > 0
                    ? $"Painéis soterrados: {campaign.Solar.BuriedPanels}/32. Limpe com a vassoura; a base está usando a bateria."
                    : $"Sem energia! O oxigênio está diminuindo. Limpe os painéis: {campaign.Solar.BuriedPanels}/32 soterrados.";
            if (!campaign.Communications.Connected)
                return "Antena danificada pela erupção solar. Faça as 3 etapas do reparo no computador.";
            if (demand.Stage == DemandStage.Impact && demand.Event == DemandEvent.SolarFlare)
                return "Escudo regulado: proteção ativa durante a erupção solar.";
            return "";
        }
    }
}
