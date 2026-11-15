using System;
using Ares.Data;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Optional, attributed science shelf. Never a gate to playing or a stream of popups.</summary>
    internal static class ScienceLibrary
    {
        public static void Build(VisualElement parent, Func<bool> canInteract)
        {
            var shelf = new Foldout { text = "Explorar ciência real", value = false, name = "science-library" };
            parent.Add(shelf);
            Add(shelf, "O Sol", "Os desafios de explosões e nuvens solares usam registros reais de 10 de maio de 2024. São exemplos do arquivo, não alertas de agora.",
                "NASA · DONKI", "https://ccmc.gsfc.nasa.gov/tools/DONKI/", canInteract);
            var plants = Add(shelf, "Plantas no espaço", "Cientistas estudam plantas para aprender a cultivar longe da Terra.",
                "NASA · OSDR", "https://www.nasa.gov/osdr-api/", canInteract);
            var archive = ScienceArchive.Load();
            if (archive?.studies != null) foreach (var study in archive.studies)
            {
                if (study == null || !OfficialSources.IsOfficial(study.url)) continue;
                string caption = study.id == "GLDS-177" ? "Flores e fungos na estação espacial" : study.id == "GLDS-520" ? "Plantas expostas à radiação" : "Plantas sob pressão baixa";
                var button = Button(plants, caption + " · " + study.id + " ↗", () => { if (canInteract()) Application.OpenURL(study.url); }, "ghost");
                button.tooltip = study.title; // Exact study title stays available without requiring technical English.
            }
            Add(shelf, "Radiação", "RadLab reúne medições de instrumentos no espaço. Uma medição na estação espacial não é a dose de um astronauta em Marte. Nossa barra usa uma escala de jogo.",
                "NASA · RadLab", "https://visualization.osdr.nasa.gov/radlab/gui/data-api/", canInteract);
            Add(shelf, "Nosso escudo é real?", "Escudos magnéticos para astronautas ainda estão em pesquisa. O escudo desta base é uma ideia de ficção científica para o jogo.",
                "NASA · pesquisa em proteção", "https://www.nasa.gov/centers-and-facilities/armstrong/space-and-hypersonics-technologies/", canInteract);
            Add(shelf, "Explorar Marte", "Veja mapas reais de Marte. Nossa base foi desenhada para jogar: não é um lugar medido pelo Mars Trek.",
                "NASA · Mars Trek", "https://trek.nasa.gov/mars/", canInteract);
            Add(shelf, "Comida longe de casa", "Cultivar comida exige cuidar da água, da luz e das plantas. Explore atividades com sua família ou escola.",
                "Agência Espacial Canadense", "https://www.asc-csa.gc.ca/eng/youth-educators/toolkits/food-production/", canInteract);
            Text(shelf, "Fontes oficiais verificadas. Projeto independente, sem endosso da NASA ou da Agência Espacial Canadense.", "ox-muted");
        }
        static VisualElement Add(VisualElement parent, string title, string explanation, string source, string url, Func<bool> canInteract)
        {
            var section = new Foldout { text = title, value = false }; parent.Add(section);
            Text(section, explanation, "ox-body");
            if (OfficialSources.IsOfficial(url)) Button(section, source + " ↗", () => { if (canInteract()) Application.OpenURL(url); }, "ghost");
            return section;
        }
    }
}
