using System;
using Ares.Domain;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Accessible text over generated button art; all choices mutate only the existing domain API.</summary>
    internal sealed class OxygenControls
    {
        readonly OxygenPlant plant;
        readonly Button[] modes = new Button[3];
        readonly Label[] states = new Label[3];
        readonly Button laboratory;
        readonly Label experimentState;
        readonly Label choiceHint;
        public OxygenControls(VisualElement parent, OxygenPlant plant, Func<bool> canInteract, Action refresh)
        {
            this.plant = plant;
            Text(parent, "Escolha o ritmo", "ox-section");
            var choices = Box(parent, "ox-choices");
            var skin = Resources.Load<Texture2D>("ARES/UI/Oxygen/button-cream-v1");
            string[] names = { "Poupar", "Equilibrar", "Reforçar" };
            string[] hints = { "Gasta menos energia", "Ritmo do dia a dia", "Produz mais oxigênio" };
            string[] explanations = { "Produz menos oxigênio e pode usar a reserva.", "Equilibra produção e gasto de energia.", "Produz mais, mas gasta mais água e energia." };
            for (int i = 0; i < 3; i++)
            {
                var mode = (OxygenMode)i;
                modes[i] = Button(choices, "", () => { if (!canInteract()) return; plant.SetMode(mode); refresh(); }, "ox-mode");
                modes[i].name = "oxygen-mode-" + i; modes[i].tooltip = explanations[i];
                if (skin != null) modes[i].style.backgroundImage = new StyleBackground(skin);
                Text(modes[i], names[i], "ox-mode-title"); Text(modes[i], hints[i], "ox-mode-hint");
                states[i] = Text(modes[i], "", "ox-mode-state");
            }
            choiceHint = Text(parent, "", "ox-choice-hint");
            var experiments = Box(parent, "ox-experiments");
            var copy = Box(experiments, "ox-experiment-copy");
            experimentState = Text(copy, "", "ox-experiment-title");
            Text(copy, "Pausar poupa um pouco de oxigênio.", "ox-muted");
            laboratory = Button(experiments, "", () => { if (!canInteract()) return; plant.SetLaboratory(!plant.LaboratoryEnabled); refresh(); }, "ox-experiment-button");
            laboratory.name = "oxygen-laboratory";
            Refresh();
        }
        public void Refresh()
        {
            for (int i = 0; i < modes.Length; i++)
            { bool active = i == (int)plant.Mode; modes[i].EnableInClassList("selected", active); states[i].text = active ? "EM USO" : ""; }
            experimentState.text = plant.LaboratoryEnabled ? "Experimentos ligados" : "Experimentos pausados";
            laboratory.text = plant.LaboratoryEnabled ? "Pausar" : "Ligar";
            choiceHint.text = plant.Mode == OxygenMode.Economy ? "Poupar gasta menos energia, mas pode baixar a reserva."
                : plant.Mode == OxygenMode.Boost ? "Reforçar faz mais oxigênio e gasta mais água e energia."
                : "Equilibrar busca repor o oxigênio que a equipe usa.";
        }
    }
}
