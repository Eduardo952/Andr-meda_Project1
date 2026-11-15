using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Local SAMPLE illustrations from tasks/Illustration.tsx, never represented as NASA photographs.</summary>
    internal sealed class SnapshotArt : ChallengeArt
    {
        readonly string kind;
        public SnapshotArt(string kind) : base(680, 330) { this.kind = kind; }
        protected override void Draw(Painter2D p)
        {
            float scale = Mathf.Min(contentRect.width / 200, contentRect.height / 110);
            Vector2 offset = new Vector2((contentRect.width - 200 * scale) / 2, (contentRect.height - 110 * scale) / 2);
            Vector2 P(float x, float y) => offset + new Vector2(x, y) * scale;
            var milk = new Color32(248, 243, 229, 255);
            var rust = new Color32(185, 92, 58, 255);
            var coral = new Color32(219, 128, 92, 255);
            void Rect(float x, float y, float w, float h, Color fill) => Polygon(p, fill, fill, 0, P(x, y), P(x+w, y), P(x+w, y+h), P(x, y+h));
            Rect(0, 0, 200, 110, kind == "sunset" ? ChallengePalette.Ink : new Color(1, .8f, .25f, .6f));
            if (kind == "sunset")
            {
                Circle(p, P(100, 72), 26 * scale, new Color(.45f, .74f, .88f, .4f), Color.clear, 0);
                Circle(p, P(100, 72), 9 * scale, milk, Color.clear, 0);
            }
            if (kind == "olympus")
            {
                p.fillColor = rust; p.BeginPath(); p.MoveTo(P(10, 85));
                p.QuadraticCurveTo(P(100, 20), P(190, 85)); p.ClosePath(); p.Fill();
            }
            if (kind == "dunes") for (int i=0;i<3;i++)
            {
                float y=70+i*12;
                p.fillColor=i%2==1?coral:rust; p.BeginPath(); p.MoveTo(P(0,y));
                p.QuadraticCurveTo(P(50,55+i*12),P(100,y)); p.QuadraticCurveTo(P(150,85+i*12),P(200,y));
                p.LineTo(P(200,110)); p.LineTo(P(0,110)); p.ClosePath(); p.Fill();
            }
            Rect(0,85,200,25,coral);
            if(kind=="dustDevil")
            {
                p.fillColor=new Color(milk.r/255f,milk.g/255f,milk.b/255f,.7f);
                p.BeginPath(); p.MoveTo(P(120,85)); p.QuadraticCurveTo(P(110,60),P(125,45));
                p.QuadraticCurveTo(P(105,30),P(130,10)); p.LineTo(P(145,10));
                p.QuadraticCurveTo(P(125,30),P(140,45)); p.QuadraticCurveTo(P(125,60),P(130,85)); p.ClosePath(); p.Fill();
            }
            if(kind=="rover")
            {
                Rect(70,55,55,16,milk); Rect(110,35,4,20,milk); Rect(106,29,12,8,milk);
                foreach(int x in new[]{76,97,118}) Circle(p,P(x,81),7*scale,ChallengePalette.Ink,Color.clear,0);
            }
        }
    }
}
