using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Circular viewport and bounded digital zoom; never edits the source photograph.</summary>
    internal sealed class ObservatoryLens
    {
        readonly VisualElement frame;
        readonly Image photo;
        Slider zoom;
        Button less, more;
        float diameter = 400;
        public ObservatoryLens(VisualElement parent)
        {
            var glow = Box(parent, "telescope-glow");
            frame = Box(glow, "telescope-lens"); frame.name = "observatory-lens";
            photo = new Image { name = "observatory-photo", scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            photo.AddToClassList("telescope-photo"); frame.Add(photo);
            var crosshair = Box(frame, "telescope-crosshair"); crosshair.pickingMode = PickingMode.Ignore;
            Box(crosshair, "telescope-crosshair-x").pickingMode = PickingMode.Ignore;
            Box(crosshair, "telescope-crosshair-y").pickingMode = PickingMode.Ignore;
        }
        public void BuildControls(VisualElement parent)
        {
            less = Button(parent, "−", () => zoom.value -= .25f, "telescope-zoom-button");
            less.name = "observatory-zoom-out"; less.tooltip = "Diminuir imagem";
            zoom = new Slider(1, 3) { value = 1, name = "observatory-zoom", tooltip = "Ampliar imagem de 1 a 3 vezes" };
            zoom.AddToClassList("telescope-zoom"); parent.Add(zoom);
            more = Button(parent, "+", () => zoom.value += .25f, "telescope-zoom-button");
            more.name = "observatory-zoom-in"; more.tooltip = "Ampliar imagem";
            zoom.RegisterValueChangedCallback(_ => ApplyZoom()); ApplyZoom();
        }
        public void SetImage(Texture2D texture) => photo.image = texture;
        public void ResetZoom() { zoom.value = 1; }
        public void Resize(float size)
        {
            diameter = size; frame.style.width = size; frame.style.height = size; ApplyZoom();
        }
        void ApplyZoom()
        {
            float value = zoom?.value ?? 1;
            float size = Mathf.Max(1, diameter - 4) * value;
            photo.style.width = size; photo.style.height = size;
            photo.style.left = (diameter - 4 - size) * .5f; photo.style.top = (diameter - 4 - size) * .5f;
            less?.SetEnabled(value > 1); more?.SetEnabled(value < 3);
            if (zoom != null) zoom.tooltip = "Ampliação: " + value.ToString("0.00") + "×";
        }
    }
}
