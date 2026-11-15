using System;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Shared light station shell: fixed header/footer and a separately scrolling body.</summary>
    internal sealed class ChallengePanel
    {
        public VisualElement Body { get; }
        public VisualElement Footer { get; }
        public ChallengePanel(VisualElement root, Localization locale, string title, string sector, string source,
            bool sample, Action information, Action close)
        {
            root.AddToClassList("mini-overlay"); root.AddToClassList("region-" + sector);
            var header = Box(root, "row mini-header"); var titles = Box(header, "mini-header-title");
            Text(titles, locale.T("rooms." + sector), "mini-sector"); Text(titles, title, "title");
            var badges = Box(titles, "row mini-badges");
            Text(badges, locale.T(source == null ? "report.gameRule" : "report.realData"), source == null ? "mini-source rule" : "mini-source real");
            if (sample) Text(badges, locale.T("report.sample"), "mini-source sample");
            var actions = Box(header, "row mini-header-actions");
            Button(actions, "i", information, "ghost").tooltip = locale.T("info.show");
            Button(actions, "×", close, "ghost").tooltip = locale.T("settings.close");
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("mini-scroll"); root.Add(scroll);
            Body = scroll.contentContainer; Footer = Box(root, "row mini-footer");
        }
    }
}
