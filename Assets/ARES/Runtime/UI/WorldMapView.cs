using System;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Fullscreen map presentation. Frontend owns navigation/pause; capture owns world rendering.</summary>
    internal sealed class WorldMapView : IDisposable
    {
        readonly VisualElement screen, viewport;
        readonly Image panorama;
        readonly MapCapture capture;
        readonly MapMarker marker;
        readonly ReferenceExplorer explorer;
        readonly MapViewportInput input;
        readonly Button zoomIn, zoomOut;
        readonly Label zoomValue, locationState;
        MapLabelLayout.Place roverPlace;
        readonly Localization locale;
        readonly MapLabelLayout labels = new MapLabelLayout();
        bool disposed;

        public WorldMapView(VisualElement parent, ReferenceExplorer explorer, Localization locale, Action close)
        {
            this.explorer = explorer;
            this.locale = locale;
            capture = new MapCapture(explorer);
            screen = Box(parent, "world-map"); screen.name = "world-map";
            var header = Box(screen, "map-header");
            var icon = new ChallengeIcon("map", MissionVisualStyle.Hex("#282139"), 36);
            icon.AddToClassList("map-header-icon"); header.Add(icon);
            var heading = Box(header, "map-heading");
            Text(heading, "ARES BASE", "map-eyebrow");
            Text(heading, locale.T("map.title"), "map-title");
            var back = Button(header, locale.T("map.close"), close, "map-close"); back.name = "map-close";
            viewport = Box(screen, "map-viewport"); viewport.name = "map-viewport";
            viewport.focusable = true; viewport.tabIndex = 0;
            panorama = new Image { name = "map-panorama", scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            panorama.AddToClassList("map-panorama"); viewport.Add(panorama);
            // Labels share the exact projection used to frame the orthographic camera.
            foreach (var room in explorer.definition.layout.rooms)
            {
                var label = Text(viewport, locale.T("rooms." + room.id), "map-place");
                label.AddToClassList("map-place-" + room.id);
                label.name = "map-place-" + room.id; label.pickingMode = PickingMode.Ignore;
                labels.Add(label, room.Center);
            }
            if(explorer.definition.layout.landmarks!=null)foreach(var landmark in explorer.definition.layout.landmarks)
            {
                var label=Text(viewport,locale.T("map.landmark."+landmark.id),"map-place");
                label.AddToClassList("map-landmark");label.name="map-landmark-"+landmark.id;label.pickingMode=PickingMode.Ignore;
                var place = labels.Add(label, landmark.Center);
                if (landmark.id == "rover") roverPlace = place;
            }
            marker = new MapMarker(); viewport.Add(marker);
            var north = Text(viewport, "N\n↑", "map-north"); north.pickingMode = PickingMode.Ignore;
            var controls = Box(screen, "map-controls"); controls.name = "map-controls";
            zoomOut = Button(controls, "−", () => Zoom(.8f, new Vector2(.5f,.5f)), "map-zoom-button");
            zoomOut.name = "map-zoom-out"; zoomOut.tooltip = locale.T("map.zoomOut");
            zoomValue = Text(controls, "100%", "map-zoom-value"); zoomValue.name = "map-zoom-value";
            zoomIn = Button(controls, "+", () => Zoom(1.25f, new Vector2(.5f,.5f)), "map-zoom-button");
            zoomIn.name = "map-zoom-in"; zoomIn.tooltip = locale.T("map.zoomIn");
            Button(controls, locale.T("map.base"), () => { capture.FocusBase(); RefreshFrame(); }, "map-base").name = "map-base";
            Button(controls, locale.T("map.locate"), () => { capture.Locate(explorer.ControlledPosition); RefreshFrame(); }, "map-locate").name = "map-locate";
            Button(controls, locale.T("map.reset"), () => { capture.ResetView(); RefreshFrame(); }, "ghost").name = "map-reset";
            locationState = Text(viewport, "", "map-location-state"); locationState.pickingMode = PickingMode.Ignore;
            input = new MapViewportInput(Zoom, delta => { capture.Pan(delta); RefreshFrame(); });
            viewport.AddManipulator(input);
            var footer = Box(screen, "map-footer");
            Text(footer, locale.T(explorer.IsDrivingRover ? "map.rover" : "map.player"), "map-legend");
            Text(footer, locale.T("map.hint"), "map-hint");
            viewport.RegisterCallback<GeometryChangedEvent>(Resize);
            // Underlying HUD controls are disabled by GameFrontend; native Tab navigation stays in this modal.
            screen.RegisterCallback<KeyDownEvent>(KeyDown);
            viewport.RegisterCallback<NavigationMoveEvent>(Navigate);
            screen.RegisterCallback<NavigationCancelEvent>(e => { close(); e.StopPropagation(); });
            back.Focus();
        }

        void Resize(GeometryChangedEvent evt)
        {
            if (disposed) return;
            input.CancelDrag();
            capture.Resize(viewport.contentRect.width, viewport.contentRect.height);
            panorama.image = capture.Texture;
            RefreshFrame();
        }
        void Zoom(float factor, Vector2 anchor) { capture.ZoomAt(factor, anchor); RefreshFrame(); }
        void RefreshFrame()
        {
            zoomValue.text = Mathf.RoundToInt(capture.Zoom * 100) + "%";
            zoomOut.SetEnabled(capture.Zoom > 1.001f);
            zoomIn.SetEnabled(capture.Zoom < capture.MaxZoom - .001f);
            Update();
        }
        void KeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Plus: case KeyCode.Equals: case KeyCode.KeypadPlus: Zoom(1.25f, new Vector2(.5f,.5f)); break;
                case KeyCode.Minus: case KeyCode.KeypadMinus: Zoom(.8f, new Vector2(.5f,.5f)); break;
                case KeyCode.Home: capture.ResetView(); RefreshFrame(); break;
                default: return;
            }
            evt.StopPropagation(); screen.panel?.focusController.IgnoreEvent(evt);
        }
        void Navigate(NavigationMoveEvent evt)
        {
            // Runtime arrows/gamepad use navigation events; consume them so map focus does not jump to a button.
            if (evt.target != viewport) return;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left: capture.Pan(new Vector2(.1f,0)); break;
                case NavigationMoveEvent.Direction.Right: capture.Pan(new Vector2(-.1f,0)); break;
                case NavigationMoveEvent.Direction.Up: capture.Pan(new Vector2(0,.1f)); break;
                case NavigationMoveEvent.Direction.Down: capture.Pan(new Vector2(0,-.1f)); break;
                default: return;
            }
            RefreshFrame(); evt.StopPropagation(); viewport.panel?.focusController.IgnoreEvent(evt);
        }
        void Position(VisualElement element, Vector3 world)
        {
            var point = capture.Projection.Normalized(world);
            element.style.left = Length.Percent(point.x * 100);
            element.style.top = Length.Percent(point.y * 100);
            element.style.display = point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1 ? DisplayStyle.None : DisplayStyle.Flex;
        }
        public void Update()
        {
            if (disposed || capture.Texture == null) return;
            Position(marker, explorer.ControlledPosition);
            if (explorer.Rover != null) labels.Move(roverPlace, explorer.Rover.transform.position);
            labels.Arrange(capture.Projection, viewport.contentRect.size);
            marker.SetForward(explorer.ControlledForward);
            bool outside = marker.style.display.value == DisplayStyle.None;
            locationState.style.display = outside ? DisplayStyle.Flex : DisplayStyle.None;
            if (outside) locationState.text = locale.T("map.offscreen");
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            viewport.UnregisterCallback<GeometryChangedEvent>(Resize);
            viewport.RemoveManipulator(input);
            viewport.UnregisterCallback<NavigationMoveEvent>(Navigate);
            screen.UnregisterCallback<KeyDownEvent>(KeyDown);
            panorama.image = null;
            capture.Dispose(); screen.RemoveFromHierarchy();
        }
    }
}
