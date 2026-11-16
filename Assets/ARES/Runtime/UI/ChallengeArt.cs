using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Vector equivalents of the reference SVGs. Drawing never changes task state.</summary>
    internal abstract class ChallengeArt : VisualElement
    {
        protected ChallengeArt(float width, float height)
        {
            style.width = width; style.height = height; style.maxWidth = Length.Percent(100);
            style.flexShrink = 0; pickingMode = PickingMode.Ignore;
            generateVisualContent += context => Draw(context.painter2D);
        }
        protected abstract void Draw(Painter2D p);
        internal static void Line(Painter2D p, Color color, float width, params Vector2[] points)
        {
            if (points.Length < 2) return;
            p.strokeColor = color; p.lineWidth = width; p.lineCap = LineCap.Round;
            p.BeginPath(); p.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++) p.LineTo(points[i]);
            p.Stroke();
        }
        internal static void Circle(Painter2D p, Vector2 center, float radius, Color fill, Color stroke, float width = 2)
        {
            p.fillColor = fill; p.strokeColor = stroke; p.lineWidth = width;
            p.BeginPath(); p.Arc(center, radius, 0, 360); p.ClosePath(); p.Fill(); if (width > 0) p.Stroke();
        }
        internal static void Polygon(Painter2D p, Color fill, Color stroke, float width, params Vector2[] points)
        {
            p.fillColor = fill; p.strokeColor = stroke; p.lineWidth = width;
            p.BeginPath(); p.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++) p.LineTo(points[i]);
            p.ClosePath(); p.Fill(); if (width > 0) p.Stroke();
        }
        internal static Vector2 Rotate(Vector2 point, Vector2 center, float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad; var delta = point - center;
            return center + new Vector2(delta.x * Mathf.Cos(angle) - delta.y * Mathf.Sin(angle), delta.x * Mathf.Sin(angle) + delta.y * Mathf.Cos(angle));
        }
    }

    internal sealed class SolarTiltArt : ChallengeArt
    {
        public int Angle { get; private set; }
        public int Target { get; }
        public SolarTiltArt(int angle, int target) : base(400, 240)
        { name = "solar-tilt-art"; Angle = angle; Target = target; }
        public void SetAngle(int angle) { Angle = angle; MarkDirtyRepaint(); }
        protected override void Draw(Painter2D p)
        {
            float scale = Mathf.Min(contentRect.width / 240, contentRect.height / 180);
            Vector2 offset = new Vector2((contentRect.width - 240 * scale) / 2, 0);
            Vector2 Map(float x, float y) => offset + new Vector2(x, y) * scale;
            Vector2 Turn(float x, float y, float angle) => offset + Rotate(new Vector2(x, y), new Vector2(120, 90), -angle) * scale;
            Line(p, ChallengePalette.Ink, 5 * scale, Map(25, 150), Map(215, 150));
            Line(p, ChallengePalette.Ink, 5 * scale, Map(120, 150), Map(120, 75));
            // Dashed target is drawn behind the panel, exactly as in TiltSun.tsx.
            for (float x = 55; x < 185; x += 14)
            {
                p.strokeColor = ChallengePalette.Solar; p.lineWidth = 12 * scale; p.lineCap = LineCap.Butt;
                p.BeginPath(); p.MoveTo(Turn(x, 90, Target)); p.LineTo(Turn(Mathf.Min(x + 8, 185), 90, Target)); p.Stroke();
            }
            Polygon(p, ChallengePalette.Muted, ChallengePalette.Ink, 3 * scale,
                Turn(55, 75, Angle), Turn(185, 75, Angle), Turn(185, 105, Angle), Turn(55, 105, Angle));
            for (int x = 75; x <= 175; x += 25)
                Line(p, ChallengePalette.Ink, 2 * scale, Turn(x, 75, Angle), Turn(x, 105, Angle));
        }
    }

    internal sealed class CoilArt : ChallengeArt
    {
        readonly int target, steps, marks;
        readonly float motionSeconds, started;
        int value;
        public CoilArt(int value, int target, int steps, int marks, float motionSeconds) : base(150, 150)
        {
            this.value = value; this.target = target; this.steps = steps; this.marks = marks;
            this.motionSeconds = motionSeconds; started = Time.unscaledTime;
            if (motionSeconds > 0) schedule.Execute(MarkDirtyRepaint).Every(33);
        }
        public void SetValue(int next) { value = next; MarkDirtyRepaint(); }
        protected override void Draw(Painter2D p)
        {
            var c = new Vector2(75, 75); float s = 1.25f;
            Vector2 At(float x, float y, float degrees) => Rotate(new Vector2(x * s, y * s), c, degrees);
            Circle(p, c, 48 * s, ChallengePalette.Muted, ChallengePalette.Ink, 3);
            for (int i = 0; i < marks; i++) Line(p, ChallengePalette.Ink, 3, At(60, 13, i * 360f / marks), At(60, 21, i * 360f / marks));
            float targetAngle = target * 360f / steps, angle = value * 360f / steps;
            Polygon(p, ChallengePalette.Shield, ChallengePalette.Ink, 2, At(54, 10, targetAngle), At(66, 10, targetAngle), At(60, 25, targetAngle));
            Line(p, ChallengePalette.Ink, 5, At(60, 65, angle), At(60, 27, angle));
            Line(p, ChallengePalette.Ink, 5, At(50, 65, angle), At(70, 65, angle));
            Circle(p, c, 8, ChallengePalette.Shield, ChallengePalette.Ink);
            if (motionSeconds > 0) Circle(p, At(60, 12, (Time.unscaledTime - started) * 360 / motionSeconds), 3,
                ChallengePalette.Shield, ChallengePalette.Ink, 1);
        }
    }

    internal sealed class PipeArt : ChallengeArt
    {
        readonly int value, target, steps;
        public PipeArt(int value, int target, int steps) : base(82, 82)
        { this.value = value; this.target = target; this.steps = steps; }
        protected override void Draw(Painter2D p)
        {
            var c = new Vector2(41, 41);
            Vector2 At(float x, float y, int v) => Rotate(new Vector2(x, y) * .82f, c, v * 360f / steps);
            if (value != target)
            {
                var faded = new Color(.4f, .38f, .44f, .35f);
                for (int d = 8; d < 50; d += 11) Line(p, faded, 12, At(50, d, target), At(50, Mathf.Min(d + 4, 50), target));
                for (int d = 50; d < 92; d += 11) Line(p, faded, 12, At(d, 50, target), At(Mathf.Min(d + 4, 92), 50, target));
            }
            var color = value == target ? ChallengePalette.Green : ChallengePalette.Life;
            Line(p, color, 8, At(50, 8, value), c, At(92, 50, value));
            Circle(p, c, 6, ChallengePalette.Paper, color, 4);
        }
    }

    internal sealed class ChallengeIcon : ChallengeArt
    {
        readonly string kind; readonly Color color;
        public ChallengeIcon(string kind, Color color, float size = 64) : base(size, size)
        { this.kind = kind; this.color = color; }
        protected override void Draw(Painter2D p)
        {
            float w = contentRect.width, h = contentRect.height;
            Vector2 P(float x, float y) => new Vector2(x * w, y * h);
            if (kind == "roof")
            {
                Line(p,color,2,P(.1f,.5f),P(.5f,.15f),P(.9f,.5f));
                Line(p,color,2,P(.2f,.5f),P(.2f,.85f),P(.8f,.85f),P(.8f,.5f));
            }
            else if (kind == "focus")
            {
                Circle(p,P(.5f,.5f),w*.23f,Color.clear,color,2);
                Line(p,color,2,P(.1f,.35f),P(.1f,.1f),P(.35f,.1f)); Line(p,color,2,P(.65f,.1f),P(.9f,.1f),P(.9f,.35f));
                Line(p,color,2,P(.1f,.65f),P(.1f,.9f),P(.35f,.9f)); Line(p,color,2,P(.65f,.9f),P(.9f,.9f),P(.9f,.65f));
            }
            else if (kind == "camera")
            {
                Polygon(p,Color.clear,color,2,P(.1f,.3f),P(.3f,.3f),P(.4f,.15f),P(.65f,.15f),P(.75f,.3f),P(.9f,.3f),P(.9f,.85f),P(.1f,.85f));
                Circle(p,P(.5f,.55f),w*.16f,Color.clear,color,2);
            }
            else if (kind == "report")
            {
                Polygon(p,Color.clear,color,2,P(.2f,.2f),P(.8f,.2f),P(.8f,.9f),P(.2f,.9f));
                Line(p,color,3,P(.4f,.15f),P(.6f,.15f));
                for(int i=0;i<3;i++) Line(p,color,2,P(.35f,.4f+i*.16f),P(.65f,.4f+i*.16f));
            }
            else if (kind == "settings")
            {
                Circle(p,P(.5f,.5f),w*.28f,Color.clear,color,2); Circle(p,P(.5f,.5f),w*.1f,Color.clear,color,2);
                for(int i=0;i<8;i++) {float a=i*Mathf.PI/4;var v=new Vector2(Mathf.Sin(a),Mathf.Cos(a));Line(p,color,3,P(.5f,.5f)+v*w*.28f,P(.5f,.5f)+v*w*.4f);}
            }
            else if(kind=="telescope") {
                Line(p,color,7,P(.18f,.48f),P(.77f,.22f));Line(p,color,3,P(.5f,.42f),P(.5f,.7f),P(.25f,.9f));Line(p,color,3,P(.5f,.7f),P(.75f,.9f));
            }
            else if(kind=="shield") {
                Polygon(p,Color.clear,color,3,P(.16f,.18f),P(.84f,.18f),P(.78f,.65f),P(.5f,.9f),P(.22f,.65f));
            }
            else if(kind=="oxygen") {
                Line(p,color,5,P(.5f,.12f),P(.5f,.88f));Line(p,color,5,P(.12f,.5f),P(.88f,.5f));
            }
            else if(kind=="map") {
                Polygon(p,Color.clear,color,2,P(.1f,.2f),P(.36f,.1f),P(.64f,.25f),P(.9f,.1f),P(.9f,.8f),P(.64f,.9f),P(.36f,.75f),P(.1f,.9f));
                Line(p,color,2,P(.36f,.1f),P(.36f,.75f));Line(p,color,2,P(.64f,.25f),P(.64f,.9f));
            }
            else if (kind == "sun")
            {
                Circle(p, P(.5f,.5f), w * .2f, Color.clear, color, 3);
                for (int i=0;i<8;i++) { float a=i*Mathf.PI/4; var v=new Vector2(Mathf.Sin(a),Mathf.Cos(a)); Line(p,color,3,P(.5f,.5f)+v*w*.32f,P(.5f,.5f)+v*w*.44f); }
            }
            else if (kind == "sprout" || kind == "apple")
            {
                Line(p,color,3,P(.5f,.85f),P(.5f,.35f));
                if(kind=="apple") { Circle(p,P(.37f,.58f),w*.23f,color,color); Circle(p,P(.64f,.58f),w*.23f,color,color); }
                else { Polygon(p,color,color,2,P(.5f,.55f),P(.12f,.22f),P(.12f,.48f)); Polygon(p,color,color,2,P(.5f,.45f),P(.88f,.12f),P(.88f,.38f)); }
            }
            else if (kind == "drop")
                Polygon(p,color,color,2,P(.5f,.1f),P(.2f,.6f),P(.23f,.8f),P(.5f,.9f),P(.77f,.8f),P(.8f,.6f));
            else if (kind == "thermometer")
            { Line(p,color,12,P(.5f,.15f),P(.5f,.7f)); Circle(p,P(.5f,.76f),w*.16f,ChallengePalette.Paper,color,3); Line(p,color,4,P(.5f,.3f),P(.5f,.75f)); }
            else
            { Circle(p,P(.5f,.5f),w*.4f,Color.clear,color,3); Polygon(p,color,color,2,P(.68f,.25f),P(.58f,.59f),P(.27f,.72f),P(.42f,.41f)); }
        }
    }

    internal sealed class ChallengeGrid : VisualElement
    {
        public ChallengeGrid()
        {
            AddToClassList("mini-play");
            generateVisualContent += context => {
                var p = context.painter2D; var color = new Color(.3f,.75f,.7f,.10f);
                for(float x=0;x<contentRect.width;x+=28) ChallengeArt.Line(p,color,1,new Vector2(x,0),new Vector2(x,contentRect.height));
                for(float y=0;y<contentRect.height;y+=28) ChallengeArt.Line(p,color,1,new Vector2(0,y),new Vector2(contentRect.width,y));
            };
        }
    }
}
