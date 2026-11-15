using System;
using UnityEngine.UIElements;

namespace Ares.UI
{
    internal static class UiElements
    {
        public static VisualElement Box(VisualElement parent, string classes = "")
        {
            var element = new VisualElement(); foreach (var c in classes.Split(' ')) if (c.Length > 0) element.AddToClassList(c);
            parent.Add(element); return element;
        }
        public static Label Text(VisualElement parent, string text, string classes = "")
        {
            var label = new Label(text); foreach (var c in classes.Split(' ')) if (c.Length > 0) label.AddToClassList(c);
            parent.Add(label); return label;
        }
        public static Button Button(VisualElement parent, string text, Action action, string classes = "")
        {
            var button = new Button(action) { text = text }; foreach (var c in classes.Split(' ')) if (c.Length > 0) button.AddToClassList(c);
            parent.Add(button); return button;
        }
        public static SliderInt Slider(VisualElement parent, string title, int value, int max, int step, Action<int> changed)
        {
            var label = Text(parent, title + " · " + value);
            var slider = new SliderInt(0, max) { value = value }; parent.Add(slider);
            slider.tooltip = title; label.AddToClassList("slider-value");
            slider.RegisterValueChangedCallback(e => { int v = UnityEngine.Mathf.RoundToInt((float)e.newValue / step) * step; slider.SetValueWithoutNotify(v); label.text = title + " · " + v; changed(v); });
            return slider;
        }
    }
}
