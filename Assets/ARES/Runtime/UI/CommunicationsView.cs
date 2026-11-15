using System;
using Ares.Domain;
using Ares.Domain.Communications;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Shared central-computer and end-of-day communication view. No network calls.</summary>
    internal sealed class CommunicationsView
    {
        readonly Campaign campaign;
        readonly CommunicationsSystem radio;
        readonly VisualElement root, content;
        readonly Func<bool> canInteract;
        readonly Action next;
        readonly Func<string> archiveError;
        readonly Label status, energy, saveStatus;
        readonly Button repair;
        int revision = -1, tab, reportDay;
        public CommunicationsView(VisualElement root, Campaign campaign, Func<bool> canInteract, Action next = null, Func<string> archiveError = null)
        {
            this.root = root; this.campaign = campaign; radio = campaign.Communications; this.canInteract = canInteract; this.next = next; this.archiveError = archiveError;
            tab = next == null ? 0 : 1; reportDay = campaign.SolIndex + 1;
            root.AddToClassList("comms-view"); root.name = "communications-view";
            Text(root, "Ares Base ↔ Terra", "title");
            Text(root, "Equipe da Terra · aventura educativa", "comms-small");
            var connection = Box(root, "comms-connection"); status = Text(connection, "", "heading"); status.name = "communications-status";
            Text(connection, "Na realidade: cerca de 4–24 minutos para cada trecho. Aqui, cada trecho leva 12 segundos de jogo. Enviar não é receber uma resposta na hora.", "comms-small");
            repair = Button(connection, "", () => { if (canInteract()) radio.StartRepairStep(); Refresh(true); }, "ghost"); repair.name = "communications-repair";
            energy = Text(root, "", "comms-small");
            var tabs = Box(root, "row");
            Button(tabs, "Mensagens", () => { tab = 0; Refresh(true); }, "ghost").name = "communications-messages";
            Button(tabs, "Relatórios · 5 dias", () => { tab = 1; Refresh(true); }, "ghost").name = "communications-reports";
            content = Box(root, "comms-content"); saveStatus = Text(root, "", "comms-small");
            var science = new Foldout { text = "Como a NASA se comunica?", value = false }; root.Add(science);
            Text(science, "A Deep Space Network (DSN) é uma rede de grandes antenas na Terra. Ela ajuda a conversar com missões distantes. A luz e os sinais de rádio levam tempo para viajar.", "comms-small");
            Text(science, "Mensagens, falha de antena, bateria do rádio e custos de energia são regras educativas. Nenhuma mensagem é enviada à NASA. Dados do relatório vêm desta missão.", "comms-small");
            Button(science, "Fonte: NASA · comunicação espacial", () => Application.OpenURL("https://www.nasa.gov/centers-and-facilities/goddard/space-communications-7-things-you-need-to-know/"), "ghost");
            root.schedule.Execute(() => Refresh()).Every(200); Refresh(true);
        }
        void Refresh(bool force = false)
        {
            status.text = radio.Connected ? "Conexão ativa" : "Conexão interrompida · falha na antena";
            root.EnableInClassList("comms-offline", !radio.Connected);
            repair.style.display = radio.Connected ? DisplayStyle.None : DisplayStyle.Flex;
            string[] steps = { "1 · Verificar cabo", "2 · Alinhar antena", "3 · Testar sinal" };
            repair.text = radio.Repairing ? "Verificando… aguarde 2 segundos" : steps[Math.Min(2, radio.RepairStep)];
            repair.SetEnabled(canInteract() && !radio.Repairing);
            energy.text = $"Bateria da base: {campaign.Solar.ReserveKwh:0.0} kWh · Reserva só para relatórios: {radio.ReportBatteryKwh:0.0} kWh";
            saveStatus.text = archiveError?.Invoke() ?? "Histórico guardado nesta missão e em arquivo local. Comunicação simulada.";
            // Update button availability without rebuilding or losing the scroll position every frame.
            content.Query<Button>(className: "comms-reply").ForEach(b => {
                var tag = (ReplyTag)b.userData; b.SetEnabled(canInteract() && radio.CanReply(tag.id) && campaign.Solar.ReserveKwh >= CommunicationsSystem.ReplyCost(tag.kind)); });
            if (!force && revision == radio.Revision) return;
            revision = radio.Revision; content.Clear();
            if (tab == 0) Messages(); else Reports();
        }
        sealed class ReplyTag { public int id; public ReplyKind kind; }
        void Messages()
        {
            Text(content, "Pedir uma dica gasta mais bateria. Ela também mantém nosso ar.", "comms-small");
            foreach (var message in radio.Messages)
            {
                if (message.Direction == MessageDirection.FromEarth && message.Delivery != DeliveryState.Delivered) continue;
                var card = Box(content, message.Direction == MessageDirection.FromEarth ? "comms-message incoming" : "comms-message outgoing");
                Text(card, $"Dia {message.Day} · " + (message.Direction == MessageDirection.FromEarth ? "Equipe da Terra" : "Você → Terra"), "comms-message-title");
                Text(card, message.Body);
                Text(card, message.Delivery == DeliveryState.InTransit ? "Sinal viajando…" : message.Direction == MessageDirection.ToEarth ? "Recebida na Terra" : "Recebida em Marte", "comms-small");
                if (message.Direction == MessageDirection.FromEarth && !message.Replied)
                { var actions = Box(card, "row"); Reply(actions, message.Id, ReplyKind.Acknowledge, "Recebido · 0,2 kWh"); Reply(actions, message.Id, ReplyKind.AskForHelp, "Pedir dica · 0,5 kWh"); }
            }
            Text(content, "Novas mensagens aparecem quando o sinal chega e a antena está conectada.", "comms-small");
        }
        void Reply(VisualElement parent, int id, ReplyKind kind, string label)
        {
            var button = Button(parent, label, () => { if (canInteract()) campaign.ReplyToEarth(id, kind); Refresh(true); }, "ghost comms-reply");
            button.name = "communications-reply-" + id + "-" + (int)kind; button.userData = new ReplyTag { id = id, kind = kind };
            button.SetEnabled(canInteract() && radio.CanReply(id) && campaign.Solar.ReserveKwh >= CommunicationsSystem.ReplyCost(kind));
        }
        void Reports()
        {
            var days = Box(content, "row");
            for (int day = 1; day <= CommunicationsRules.MissionDays; day++)
            {
                int selected = day; var report = radio.ReportForDay(day);
                var b = Button(days, "Dia " + day + (report?.Sent == true ? " · enviado" : ""), () => { reportDay = selected; Refresh(true); }, "ghost");
                b.name = "communications-day-" + day; b.SetEnabled(report != null); b.EnableInClassList("selected", reportDay == day);
            }
            var current = radio.ReportForDay(reportDay);
            if (current == null) { Text(content, "Seu relatório será preparado automaticamente no fim do dia. Cuide da base e volte para revisar."); return; }
            Text(content, "Relatório do dia " + reportDay + " · dados no fechamento do dia", "heading");
            if (current.Snapshot.ScenarioUsesSampleData) Text(content, "Cenário educativo: clima espacial e ambiente de Marte usam dados de exemplo.", "comms-small");
            DailyReportView.Build(content, current.Snapshot);
            if (!current.Sent)
            {
                Text(content, "Revise os dados acima. O envio usa 0,2 kWh da bateria reservada para relatórios.", "comms-small");
                var send = Button(content, radio.Connected ? "Enviar relatório" : "Restabeleça a antena para enviar", () => { if (canInteract()) campaign.SendDailyReport(); Refresh(true); });
                send.name = "communications-send-report"; send.SetEnabled(canInteract() && radio.Connected && campaign.Phase == Phase.Summary && reportDay == campaign.SolIndex + 1);
            }
            else Text(content, current.Delivery == DeliveryState.Delivered ? "Relatório recebido na Terra." : "Relatório enviado. O sinal está viajando até a Terra.", "comms-success");
            if (next != null)
            {
                var advance = Button(content, campaign.SolIndex == 4 ? "Concluir missão" : "Avançar para o próximo dia", () => { if (canInteract() && campaign.CanAdvanceDay) next(); });
                advance.name = "communications-next-day"; advance.SetEnabled(canInteract() && campaign.CanAdvanceDay);
            }
        }
    }
}
