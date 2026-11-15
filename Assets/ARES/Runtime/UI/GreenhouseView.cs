using System;
using Ares.Domain;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Four-shelf teaching activity. Explicit choices only; decisions never run on a timer.</summary>
    internal sealed class GreenhouseView
    {
        readonly GreenhouseSimulation game;
        readonly GreenhouseWatering watering;
        readonly Action returnToWorld;
        readonly VisualElement body, footer;
        readonly Action accept;
        IrrigationLevel? chosen;
        GreenhouseAdjustment adjustment;
        string error;
        bool information;
        public GreenhouseView(VisualElement root, GreenhouseSimulation game, GreenhouseWatering watering, Action accept, Action close)
        {
            this.game = game; this.watering=watering; this.accept = accept; returnToWorld=close;
            var panel = new ChallengePanel(root, new Localization("pt-BR"), "Mestre da Estufa", "greenhouse", null, false,
                () => { information = !information; Render(); }, close);
            root.AddToClassList("greenhouse-view"); root.name = "greenhouse-view";
            body = panel.Body; footer = panel.Footer; Render();
        }
        public void Render()
        {
            body.Clear(); footer.Clear();
            if (game.CompletedCycles == 0) Text(body, GreenhouseNarrator.Welcome, "greenhouse-welcome");
            if (information)
            {
                var note = Box(body,"mini-hint"); Text(note,GreenhouseNarrator.Inspiration); Text(note,GreenhouseNarrator.WaterNote);
                Text(note,"O clima desta atividade é simulado. O ciclo avança depois de regar as quatro prateleiras.");
            }
            var columns = Box(body, "row greenhouse-columns");
            Illustration(Box(columns, "greenhouse-art"));
            var decisions = Box(columns, "greenhouse-decisions");
            if (game.LastResult != null) Text(decisions, GreenhouseNarrator.Result(game.LastResult), "greenhouse-result").name = "greenhouse-result";
            if (game.Finished) { Ending(decisions); return; }
            if(watering.HasPlan)
            {
                Text(decisions,$"Rega preparada: {GreenhouseNarrator.Litres(watering.PlannedMl)} L para as quatro prateleiras.\nJá aplicados: {GreenhouseNarrator.Litres(watering.DeliveredMl)} L · {watering.WateredShelves}/4 prateleiras concluídas.\nPegue o regador, abasteça no ponto ÁGUA e segure o clique nas áreas marcadas. A água aplicada fica registrada ao parar.","greenhouse-cycle");
                Button(footer,"Voltar para regar",returnToWorld).name="greenhouse-return";return;
            }
            Text(decisions,GreenhouseNarrator.CycleBlock(game),"greenhouse-cycle").name="greenhouse-cycle";
            var levels = Box(decisions,"row greenhouse-levels");
            foreach (IrrigationLevel level in Enum.GetValues(typeof(IrrigationLevel)))
            {
                var captured = level;
                var button = Button(levels, GreenhouseNarrator.Level(level) + "\n" + GreenhouseNarrator.Litres(GreenhouseSimulation.ConsumptionMl(level)) + " L",
                    () => { chosen=captured; adjustment=GreenhouseAdjustment.None; error=null; Render(); }, "greenhouse-level");
                button.name="greenhouse-level-"+(int)level; button.EnableInClassList("selected",chosen==level);
            }
            if (chosen.HasValue)
            {
                var options = Box(decisions,"row greenhouse-adjustments");
                Adjustment(options,GreenhouseAdjustment.MoreWater,"Fornecer mais água");
                Adjustment(options,GreenhouseAdjustment.LessWater,"Fornecer menos água");
                Adjustment(options,GreenhouseAdjustment.MoreLight,"Aumentar a luz");
                Adjustment(options,GreenhouseAdjustment.LessLight,"Diminuir a luz");
                if(adjustment!=GreenhouseAdjustment.None) Button(options,"Retirar ajuste",()=>{adjustment=GreenhouseAdjustment.None;Render();},"ghost");
                var final = GreenhouseSimulation.FinalLevel(chosen.Value,adjustment);
                Text(decisions,"Escolha final: "+GreenhouseNarrator.Level(final)+" · "+GreenhouseNarrator.Litres(GreenhouseSimulation.ConsumptionMl(final))+" L para a estufa inteira.","greenhouse-preview").name="greenhouse-preview";
            }
            if(!string.IsNullOrEmpty(error)) Text(decisions,error,"greenhouse-error").name="greenhouse-error";
            Text(footer,"A água é limitada. Você decide com calma.","small");
            var confirm=Button(footer,"Preparar rega manual",Decide); confirm.name="greenhouse-confirm"; confirm.SetEnabled(chosen.HasValue);
        }
        void Illustration(VisualElement parent)
        {
            var image = new Image { image = Resources.Load<Texture2D>("ARES/UI/Greenhouse/veggie-rack"), scaleMode = ScaleMode.ScaleToFit };
            image.AddToClassList("greenhouse-rack"); image.pickingMode=PickingMode.Ignore; parent.Add(image);
            for(int i=0;i<game.Shelves.Count;i++)
            {
                var shelf=game.Shelves[i]; var row=Box(parent,"greenhouse-shelf shelf-"+shelf.Health.ToString().ToLowerInvariant()); row.name="greenhouse-shelf-"+i;
                Text(row,$"{i+1} · {shelf.Crop}","greenhouse-crop"); Text(row,GreenhouseNarrator.Health(shelf.Health),"small");
            }
            Text(parent,"Inspiração: Veggie. Regras adaptadas para o jogo.","greenhouse-caption");
        }
        void Adjustment(VisualElement parent,GreenhouseAdjustment option,string label)
        {
            if(!game.CanAdjust(chosen.Value,option))return;
            var button=Button(parent,label,()=>{adjustment=option;error=null;Render();},"ghost");
            button.name="greenhouse-adjust-"+option; button.EnableInClassList("selected",adjustment==option);
        }
        void Decide()
        {
            if(!chosen.HasValue)return;
            var result=watering.Plan(chosen.Value,adjustment);
            if(result==GreenhouseDecisionError.InsufficientWater)
                error=$"Restam {GreenhouseNarrator.Litres(game.WaterMl)} L. Esse nível precisa de mais água. Escolha um nível menor para cuidar da reserva.";
            else if(result!=GreenhouseDecisionError.None) error="Vamos observar a estufa e escolher de novo? Nenhuma água foi usada.";
            else { chosen=null; adjustment=GreenhouseAdjustment.None; error=null; returnToWorld(); return; }
            Render();
        }
        void Ending(VisualElement parent)
        {
            Text(parent,GreenhouseNarrator.Ending(game),"greenhouse-cycle").name="greenhouse-ending";
            var reflection=new TextField("O que você aprendeu? (opcional)") {multiline=true,maxLength=240,value=game.Reflection};
            reflection.name="greenhouse-reflection"; reflection.AddToClassList("greenhouse-reflection"); parent.Add(reflection);
            reflection.RegisterValueChangedCallback(e=>game.SaveReflection(e.newValue));
            Text(footer,"Observar, escolher e aprender também é fazer ciência!","small");
            Button(footer,"Registrar aprendizado",accept).name="greenhouse-accept";
        }
    }
}
