using Ares.World;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal sealed class GreenhouseCareHud
    {
        readonly GreenhouseCare care;
        readonly VisualElement card;
        readonly Label water,climate,instruction,result;
        readonly Label[] shelves=new Label[4];
        readonly ProgressBar can,progress;
        int resultCycle;
        float resultUntil;
        public GreenhouseCareHud(VisualElement root,GreenhouseCare care)
        {
            this.care=care;card=Box(root,"panel greenhouse-care-hud");card.name="greenhouse-care-hud";
            card.Add(new CartoonText("ESTUFA",29));
            water=Text(card,"","small");can=new ProgressBar {lowValue=0,highValue=Ares.Domain.GreenhouseWatering.CapacityMl};card.Add(can);
            climate=Text(card,"","small");
            for(int i=0;i<4;i++)shelves[i]=Text(card,"","greenhouse-care-shelf");
            instruction=Text(card,"","greenhouse-care-instruction");
            progress=new ProgressBar {lowValue=0,highValue=1};card.Add(progress);
            result=Text(card,"","small");Ignore(card);
        }
        static void Ignore(VisualElement element) {element.pickingMode=PickingMode.Ignore;foreach(var child in element.Children())Ignore(child);}
        public void Update()
        {
            card.style.display=care.Inside && care.Watering!=null?DisplayStyle.Flex:DisplayStyle.None;
            var w=care.Watering;var g=care.Simulation;if(w==null)return;
            water.text=$"Reservatório: {GreenhouseNarrator.Litres(w.ReservoirMl)} L · Total disponível: {GreenhouseNarrator.Litres(w.AvailableMl)} L";
            can.value=w.CanMl;can.title=w.HasCan?$"Regador: {GreenhouseNarrator.Litres(w.CanMl)} / 2,32 L":"Regador ainda não coletado";
            climate.text=$"Ciclo {g.Cycle} · {GreenhouseNarrator.Weather(g.Weather)}\nSolo: {GreenhouseNarrator.Soil(g.Soil)} · Luz: {GreenhouseNarrator.Light(g.Light)}";
            for(int i=0;i<4;i++) shelves[i].text=$"{i+1} · {g.Shelves[i].Crop}: {GreenhouseNarrator.Health(w.PreviewHealth(i))}"+(w.HasPlan?$" · {w.Progress(i)*100:F0}%":"");
            instruction.text=care.InteractionPrompt!=null?care.InteractionPrompt:g.Finished?"Cultivo encerrado. [E] Abra o painel e registre o aprendizado.":care.CanPickUp?"[E] Pegar regador":!w.HasCan?"Pegue o regador na entrada da estufa.":!w.HasPlan?"[E] Planeje a quantidade de água e a luz.":w.CanMl==0?"Leve o regador ao ponto ÁGUA e aperte E.":care.AtRefill?"[E] Abastecer. Depois, vá às prateleiras.":care.NearbyShelf<0?$"Regue as quatro áreas marcadas · {w.WateredShelves}/4 concluídas.":w.IsWatered(care.NearbyShelf)?"Esta prateleira já recebeu sua dose. Vá à próxima.":"Segure o clique esquerdo para regar. Solte para parar.";
            progress.style.display=w.HasPlan && care.NearbyShelf>=0?DisplayStyle.Flex:DisplayStyle.None;
            progress.value=care.NearbyShelf>=0?w.Progress(care.NearbyShelf):0;progress.title="Rega da prateleira";
            if(g.CompletedCycles!=resultCycle){resultCycle=g.CompletedCycles;resultUntil=UnityEngine.Time.unscaledTime+9;}
            result.style.display=g.LastResult!=null && UnityEngine.Time.unscaledTime<resultUntil?DisplayStyle.Flex:DisplayStyle.None;
            result.text=g.LastResult==null?"":GreenhouseNarrator.Result(g.LastResult);
        }
    }
}
