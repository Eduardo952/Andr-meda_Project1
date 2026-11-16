using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Shared presentation vocabulary. Never writes mission state.</summary>
    internal static class MissionVisualStyle
    {
        public static readonly Color Ink = Hex("#26314d"), Cream = Hex("#fff4db"), Teal = Hex("#4fcfc1"), Gold = Hex("#ffcc54"), Coral = Hex("#f18168");
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var color); return color; }
        public static Color Sector(string id)
        {
            switch (id)
            {
                case "greenhouse": return Hex("#62b779");
                case "shield": return Hex("#60ade8");
                case "lifeSupport": return Teal;
                case "observatory": return Hex("#ab88df");
                case "solar": return Gold;
                default: return Coral;
            }
        }
        public static VisualElement Icon(string sector, int size = 72)
        {
            string icon=sector=="greenhouse"?"sprout":sector=="observatory"?"telescope":sector=="lifeSupport"?"oxygen":sector=="shield"?"shield":"sun";
            return new ChallengeIcon(icon,Hex("#282139"),size);
        }
        public static bool Reduced => PlayerPrefs.GetInt("ARES.ReducedMotion", 0) == 1;
        public static void Open(VisualElement element)
        {
            if (Reduced) return;
            element.AddToClassList("mission-opening");
            element.schedule.Execute(() => element.RemoveFromClassList("mission-opening")).StartingIn(24);
        }
        public static void Close(VisualElement element, Action close)
        {
            if (Reduced || element.panel == null) { close(); return; }
            if (element.ClassListContains("mission-closing")) return;
            element.SetEnabled(false); element.AddToClassList("mission-closing");
            element.schedule.Execute(close).StartingIn(160);
        }
    }
}
