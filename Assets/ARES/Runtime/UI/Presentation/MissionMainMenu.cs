using System;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Illustrated invitation to the mission; all navigation callbacks remain owned by the frontend.</summary>
    internal sealed class MissionMainMenu : IDisposable
    {
        readonly VisualElement root;
        readonly Image art;
        float time;
        public MissionMainMenu(VisualElement parent, Localization locale, Action play, Action help, Action settings, Action languages, Action exit)
        {
            root = Box(parent, "mission-menu"); root.name = "mission-main-menu";
            art = new Image { image = Resources.Load<Texture2D>("ARES/UI/Presentation/mission-mars-title-v1"), scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            art.AddToClassList("mission-menu-art"); root.Add(art);
            Box(root, "mission-menu-shade").pickingMode = PickingMode.Ignore;
            var content = Box(root, "mission-menu-content");
            Text(content, "EXPLORAR  /  APRENDER  /  CUIDAR", "mission-menu-kicker");
            Text(content, "ARES", "mission-menu-title");
            Text(content, "BASE", "mission-menu-title mission-menu-title-accent");
            Text(content, "Um novo mundo.\nUma grande aventura com Ares.", "mission-menu-description");
            var launch = Button(content, "Iniciar missão  ›", play, "mission-launch"); launch.name = "title-play";
            var links = Box(content, "mission-menu-links");
            Button(links, locale.T("title.howTo"), help, "ghost").name = "title-help";
            Button(links, locale.T("settings.title"), settings, "ghost").name = "title-settings";
            Button(links, locale.T("title.language"), languages, "ghost").name = "title-language";
            Button(content, locale.T("title.exit"), exit, "ghost").name = "title-exit";
            Text(root, "Construa descobertas. Cuide da sua equipe. Explore Marte.", "mission-menu-footer");
            Text(root, "ARES BASE", "mission-menu-version");
            MissionVisualStyle.Open(content); launch.Focus();
        }
        public void Tick(float seconds, bool reduced)
        {
            if (reduced) { art.style.scale = new Scale(Vector3.one); return; }
            time += seconds;
            art.style.scale = new Scale(Vector3.one * (1.015f + Mathf.Sin(time * .12f) * .006f));
        }
        public void Dispose() { root.RemoveFromHierarchy(); }
    }
}
