using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Shared illustrated gauge. Text and value remain native and accessible.</summary>
    internal static class ResourceGauge
    {
        public static ProgressBar Add(VisualElement parent, string title, string kind)
        {
            var cell = Box(parent, "resource resource-gauge");
            cell.name = "gauge-" + kind;
            var icon = new Image { image = Resources.Load<Texture2D>("ARES/UI/HudIcons/" + kind + "-v1"),
                scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("resource-icon"); cell.Add(icon);
            var content = Box(cell, "resource-content");
            Text(content, title, "small");
            var bar = new ProgressBar { lowValue = 0, highValue = 100, title = "" };
            bar.AddToClassList(kind); content.Add(bar);
            cell.tooltip = kind == "dose" ? "Radiação recebida: quanto menos, melhor. É uma escala do jogo."
                : kind == "oxygen" ? "Ar guardado para a equipe respirar."
                : kind == "energy" ? "Quanto a bateria ainda tem de energia." : "Estado da base: cuide dos equipamentos.";
            return bar;
        }
    }
}
