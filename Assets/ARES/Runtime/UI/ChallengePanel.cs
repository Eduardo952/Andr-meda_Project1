using System;
using UnityEngine;
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
            var header = Box(root, "row mini-header");
            header.style.backgroundColor = Color.Lerp(MissionVisualStyle.Sector(sector), MissionVisualStyle.Cream, .55f);
            var emblem = Box(header, "mini-activity-emblem"); emblem.Add(MissionVisualStyle.Icon(sector));
            var titles = Box(header, "mini-header-title");
            Text(titles, locale.T("rooms." + sector), "mini-sector"); Text(titles, title, "title");
            var badges = Box(titles, "row mini-badges");
            Text(badges, locale.T(sample ? "report.sample" : source == null ? "report.gameRule" : "report.realData"),
                sample ? "mini-source sample" : source == null ? "mini-source rule" : "mini-source real");
            var actions = Box(header, "row mini-header-actions");
            Button(actions, "i", information, "ghost").tooltip = locale.T("info.show");
            Button(actions, "×", () => MissionVisualStyle.Close(root, close), "ghost").tooltip = locale.T("settings.close");
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("mini-scroll"); root.Add(scroll);
            Body = scroll.contentContainer; Footer = Box(root, "row mini-footer");
            MissionVisualStyle.Open(root);
        }
    }
}
