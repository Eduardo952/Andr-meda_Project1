using System;
using Ares.Domain;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Hand-authored oxygen dashboard shared by the physical monitor and the interactive full-screen panel.</summary>
    internal sealed class OxygenView
    {
        readonly OxygenPlant plant;
        readonly Campaign campaign;
        readonly Label reserve, litres, production, consumption, balance, status, autonomy, accumulated, electricity, laboratoryValue, modeDetail;
        readonly OxygenTankArt tank;
        readonly Button[] modes=new Button[3];
        readonly Button laboratory;
        readonly VisualElement statusBox;
        readonly VisualElement[] sectorFills=new VisualElement[3];
        public OxygenView(VisualElement parent, Campaign campaign, Func<bool> canInteract)
        {
            this.campaign=campaign;plant=campaign.OxygenSystem;
            parent.AddToClassList("oxygen-view");
            var top=Box(parent,"oxygen-intro row");
            var titles=Box(top,"oxygen-title-block");Text(titles,"SUPORTE À VIDA / SALA CENTRAL","oxygen-eyebrow");
            Text(titles,"Cada respiro conta.","oxygen-title");
            Text(titles,"Equilibre a produção e o consumo de oxigênio da base.","oxygen-subtitle");
            Text(top,$"AO VIVO  •  CICLO DE {plant.CycleSeconds:0} s","oxygen-live");
            var columns=Box(parent,"oxygen-columns");
            var storage=Box(columns,"oxygen-storage");
            Text(storage,"RESERVA DE O₂","oxygen-eyebrow");
            tank=new OxygenTankArt();storage.Add(tank);
            reserve=Text(storage,"","oxygen-reserve-number");litres=Text(storage,"","oxygen-storage-caption");
            autonomy=Text(storage,"","oxygen-autonomy");
            var right=Box(columns,"oxygen-details");
            var metrics=Box(right,"oxygen-metrics");
            production=Metric(metrics,"PRODUÇÃO","oxygen-production","Saída do gerador");
            consumption=Metric(metrics,"CONSUMO ATUAL","oxygen-consumption","Demanda dos setores");
            statusBox=Box(right,"oxygen-balance");balance=Text(statusBox,"","oxygen-balance-number");status=Text(statusBox,"","oxygen-balance-caption");
            var sectors=Box(right,"oxygen-sectors");Text(sectors,"Para onde vai o oxigênio?","oxygen-section-title");
            Sector(sectors,"Tripulação",plant.CrewConsumption,"Respiração e atividades da equipe",0);
            Sector(sectors,"Habitat",plant.HabitatConsumption,"Renovação do ar dos módulos",1);
            laboratoryValue=Sector(sectors,"Laboratório",plant.LaboratoryConsumption,"Circuito dos experimentos",2);
            var totals=Box(right,"oxygen-totals");accumulated=Text(totals,"","oxygen-total");electricity=Text(totals,"","oxygen-total");
            var controls=Box(parent,"oxygen-controls");
            var controlTitle=Box(controls,"row oxygen-control-title");Text(controlTitle,"Ritmo do gerador","oxygen-section-title");Text(controlTitle,"Escolha o equilíbrio entre ar e energia.","small");
            var choices=Box(controls,"oxygen-modes");
            string[] names={"Econômico","Equilibrado","Reforçado"};
            string[] hints={"Menos energia · usa a reserva","Produção estável para a base","Recuperação mais rápida"};
            for(int i=0;i<3;i++)
            {
                var mode=(OxygenMode)i;
                modes[i]=Button(choices,"",()=>{if(!canInteract())return;plant.SetMode(mode);Refresh();},"oxygen-mode");modes[i].name="oxygen-mode-"+i;
                Text(modes[i],names[i],"oxygen-mode-title");Text(modes[i],hints[i],"oxygen-mode-hint");
            }
            var bottom=Box(controls,"oxygen-control-bottom");modeDetail=Text(bottom,"","oxygen-mode-detail");
            laboratory=Button(bottom,"",()=>{if(!canInteract())return;plant.SetLaboratory(!plant.LaboratoryEnabled);Refresh();},"ghost oxygen-laboratory");laboratory.name="oxygen-laboratory";
            var note=Box(parent,"oxygen-note");Text(note,"SIMULAÇÃO DA BASE","oxygen-eyebrow");
            Text(note,"As taxas são por ciclo de jogo. O consumo essencial da tripulação e do habitat permanece ativo.","small");
            Refresh();
        }
        static Label Metric(VisualElement parent,string title,string color,string detail)
        {var card=Box(parent,"oxygen-metric "+color);Text(card,title,"oxygen-eyebrow");var value=Text(card,"","oxygen-metric-value");Text(card,detail,"oxygen-metric-caption");return value;}
        Label Sector(VisualElement parent,string title,float value,string description,int index)
        {
            var row=Box(parent,"oxygen-sector");var names=Box(row,"oxygen-sector-name");Text(names,title,"oxygen-sector-title");Text(names,description,"oxygen-sector-description");
            var bar=Box(row,"oxygen-sector-bar");sectorFills[index]=Box(bar,"oxygen-sector-fill");
            return Text(row,value.ToString("0.0")+" L / ciclo","oxygen-sector-value");
        }
        public void Refresh()
        {
            reserve.text=plant.Percent.ToString("0")+"%";litres.text=$"{plant.ReserveLitres:0.0} / {plant.CapacityLitres:0} L disponíveis";
            production.text=$"{plant.Production:0.0} L / ciclo";consumption.text=$"{plant.Consumption:0.0} L / ciclo";
            balance.text=$"{plant.Balance:+0.0;-0.0;0.0} L / ciclo  •  saldo da reserva";
            bool shortage=plant.Balance<-.01f;
            status.text=plant.PowerFraction<.99f?"Energia insuficiente · o gerador perdeu produção.":plant.Critical?"Reserva baixa · aumente a produção ou reduza o consumo.":shortage?"A base está usando a reserva para completar o abastecimento.":"Abastecimento equilibrado · a produção cobre o consumo.";
            statusBox.EnableInClassList("oxygen-warning",shortage||plant.Critical||plant.PowerFraction<.99f);
            autonomy.text=float.IsPositiveInfinity(plant.RemainingSeconds)?"Reserva estável":$"Autonomia no ritmo atual: {Mathf.CeilToInt(plant.RemainingSeconds/60)} min";
            accumulated.text=$"Já consumido nesta missão\n{plant.TotalConsumed:0.0} L";
            electricity.text=$"Energia do gerador\n{plant.EnergyPerCycle:0.0} kWh / ciclo · {plant.TotalEnergyKwh:0.00} kWh gastos";
            laboratoryValue.text=$"{plant.LaboratoryConsumption:0.0} L / ciclo";
            sectorFills[0].style.width=Length.Percent(plant.CrewConsumption/plant.Consumption*100);
            sectorFills[1].style.width=Length.Percent(plant.HabitatConsumption/plant.Consumption*100);
            sectorFills[2].style.width=Length.Percent(plant.LaboratoryConsumption/plant.Consumption*100);
            laboratory.text=plant.LaboratoryEnabled?"Pausar circuito do laboratório":"Retomar circuito do laboratório";
            modeDetail.text=$"Gerados: {plant.TotalProduced:0.0} L  ·  Excedente: {plant.TotalVented:0.0} L\nBateria da base: {campaign.Solar.ReserveKwh:0.0} kWh";
            for(int i=0;i<3;i++)modes[i].EnableInClassList("selected",i==(int)plant.Mode);
            tank.Fill=plant.Percent/100;tank.Critical=plant.Critical;tank.MarkDirtyRepaint();
        }
    }
}
