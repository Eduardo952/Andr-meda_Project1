using System;
using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Only the current objective and the mentor dialogue are visible during onboarding.</summary>
    internal sealed class TutorialView : IDisposable
    {
        readonly TutorialDirector director;
        readonly VisualElement dialogue, card, actions;
        readonly Label speech, objective, instruction, progress;
        readonly Button interact, exit;
        readonly ProgressBar pouring;
        readonly AresMentorPortrait portrait;
        public VisualElement Root { get; }

        public TutorialView(VisualElement parent, ReferenceExplorer explorer, TutorialDirector director)
        {
            this.director = director;
            Root = Box(parent, "tutorial-layer"); Root.name = "tutorial-layer"; Root.pickingMode = PickingMode.Ignore;
            card = Box(Root, "tutorial-objective"); card.name = "tutorial-objective";
            objective = Text(card, "", "tutorial-objective-title"); objective.pickingMode = PickingMode.Ignore;
            instruction = Text(card, "", "tutorial-instruction"); instruction.pickingMode = PickingMode.Ignore;
            progress = Text(card, "", "tutorial-progress"); progress.pickingMode = PickingMode.Ignore;
            pouring = new ProgressBar { name = "tutorial-pour", lowValue = 0, highValue = 1 };
            card.Add(pouring);
            interact = Button(card, "", director.Interact); interact.name = "tutorial-interact";
            exit = Button(Root, "Sair do tutorial", director.Exit, "ghost tutorial-exit"); exit.name = "tutorial-exit";
            dialogue = Box(Root, "tutorial-dialogue"); dialogue.name = "tutorial-dialogue";
            var composition = Box(dialogue, "tutorial-composition");
            var fish = new Image { name = "tutorial-ares", scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            fish.AddToClassList("tutorial-portrait"); composition.Add(fish);
            portrait = new AresMentorPortrait(fish, explorer.GetComponentInChildren<BubbleAnimator>());
            var balloon = Box(composition, "tutorial-balloon"); balloon.name = "tutorial-balloon";
            balloon.style.backgroundImage = Resources.Load<Texture2D>("ARES/UI/Tutorial/ares-speech-v1");
            var content = Box(balloon, "tutorial-balloon-content");
            Text(content, "SEU COMPANHEIRO DE MISSÃO", "tutorial-eyebrow");
            content.Add(new CartoonText("ARES", 46));
            speech = Text(content, "", "tutorial-speech"); speech.name = "tutorial-speech";
            actions = Box(content, "tutorial-actions");
            Root.RegisterCallback<GeometryChangedEvent>(_ => Root.EnableInClassList("tutorial-compact", Root.resolvedStyle.width < 1200));
        }
        public void Show(TutorialStage stage)
        {
            Root.style.display = director.Active ? DisplayStyle.Flex : DisplayStyle.None;
            Root.BringToFront();
            dialogue.style.display = director.Flow.Dialogue ? DisplayStyle.Flex : DisplayStyle.None;
            card.style.display = director.Flow.Dialogue || stage == TutorialStage.AllocateEnergy ? DisplayStyle.None : DisplayStyle.Flex;
            exit.style.display = director.Flow.Dialogue ? DisplayStyle.None : DisplayStyle.Flex;
            portrait.Show(director.Active && director.Flow.Dialogue);
            actions.Clear();
            string caption = "Vamos lá!";
            switch (stage)
            {
                case TutorialStage.Offer:
                    speech.text = "Oi! Eu sou o Ares. Quer aprender comigo? Primeiro vamos dividir a energia da nave no computador. Depois, vamos regar as plantas!";
                    Button(actions, "Fazer tutorial", () => director.Choose(true)).name = "tutorial-accept";
                    Button(actions, "Jogar sem tutorial", () => director.Choose(false), "ghost").name = "tutorial-decline";
                    break;
                case TutorialStage.EnergyIntro:
                    speech.text = "Nossa primeira tarefa é dividir a energia da nave. Siga a trilha dourada até o computador. Use WASD ou as setas para andar e aperte E quando chegar.";
                    caption = "Ir ao computador"; break;
                case TutorialStage.EnergyExplain:
                    speech.text = $"Temos {director.Campaign.EnergyPool} kWh disponíveis. Arraste as três barras para dividir essa energia entre escudo, ar e calor, e estufa. Confira a necessidade de cada sistema e use toda a energia antes de confirmar.";
                    caption = "Minha vez!"; break;
                case TutorialStage.PlantsIntro:
                    speech.text = "Energia dividida! Agora vem a tarefa: regar plantas. Siga a trilha até a estufa e pegue o regador no suporte. Vou mostrar uma coisa de cada vez.";
                    caption = "Ir à estufa"; break;
                case TutorialStage.FillIntro:
                    speech.text = "Regador na mão! Ele leva 510 mL por vez. Vá até o reservatório de água e aperte E para encher. Vamos cuidar das quatro fileiras, uma de cada vez.";
                    caption = "Encher o regador"; break;
                case TutorialStage.WaterIntro:
                    speech.text = "Tudo pronto! Aproxime-se da fileira destacada e clique nas plantas ou aperte E. Fique por perto até a rega terminar. Depois, volte ao reservatório para encher novamente.";
                    caption = "Regar plantas"; break;
                case TutorialStage.Complete:
                    speech.text = "Você conseguiu! A energia está dividida e as quatro fileiras receberam água. O tutorial terminou. Vou continuar ao seu lado enquanto você explora e resolve as próximas tarefas!";
                    caption = "Continuar a missão"; break;
            }
            if (director.Flow.Dialogue && stage != TutorialStage.Offer)
                Button(actions, caption, director.Continue).name = "tutorial-continue";
            actions.Q<Button>()?.Focus();
        }
        public void Tick(GameFrontend frontend, int shelf, bool canInteract, float seconds, bool reducedMotion)
        {
            Root.BringToFront();
            portrait.Tick(seconds, reducedMotion);
            if (director.Flow.Dialogue) return;
            var water = frontend.Campaign.Watering;
            bool energy = director.Flow.Stage == TutorialStage.FindComputer;
            objective.text = energy ? "01 / 02   DIVIDIR A ENERGIA" : "02 / 02   REGAR PLANTAS";
            switch (director.Flow.Stage)
            {
                case TutorialStage.FindComputer:
                    instruction.text = canInteract ? "Você chegou! Use o computador para dividir a energia." : "Siga a trilha dourada até o computador.\nWASD / setas: andar · Arraste o cenário: girar a câmera";
                    interact.text = "Usar computador [E]"; break;
                case TutorialStage.FindCan:
                    instruction.text = "Siga a trilha até o regador no suporte da estufa.";
                    interact.text = "Pegar regador [E]"; break;
                case TutorialStage.FillCan:
                    instruction.text = "Vá ao reservatório indicado e encha o regador.";
                    interact.text = "Encher regador [E]"; break;
                case TutorialStage.WaterPlants:
                    instruction.text = water.ActiveShelf >= 0 ? "Regando… fique perto das plantas até terminar." : $"Siga a trilha até a fileira {shelf + 1}. Clique nas plantas ou aperte E para regar.";
                    interact.text = "Regar fileira [E]"; break;
            }
            progress.style.display = energy ? DisplayStyle.None : DisplayStyle.Flex;
            progress.text = $"{water.WateredShelves} / 4 fileiras cuidadas   ·   Regador: {water.CanMl} / 510 mL";
            interact.style.display = canInteract ? DisplayStyle.Flex : DisplayStyle.None;
            pouring.style.display = water.ActiveShelf >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            pouring.value = water.PourProgress; pouring.title = "Regando as plantas";
        }
        public void Dispose() { portrait.Dispose(); Root.RemoveFromHierarchy(); }
    }
}
