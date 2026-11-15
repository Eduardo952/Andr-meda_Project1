using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Resolution-independent tank illustration; reads telemetry and never mutates it.</summary>
    internal sealed class OxygenTankArt : VisualElement
    {
        public float Fill { get; set; } = 1;
        public bool Critical { get; set; }
        public OxygenTankArt()
        {
            AddToClassList("oxygen-tank-art"); pickingMode = PickingMode.Ignore;
            generateVisualContent += context => Draw(context.painter2D);
        }
        static void Rect(Painter2D p, float x,float y,float w,float h,Color color)
        { ChallengeArt.Polygon(p,color,color,0,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)); }
        void Draw(Painter2D p)
        {
            float width=contentRect.width, height=contentRect.height;
            if(width<1 || height<1)return;
            float x=width*.5f, top=height*.18f, bottom=height*.87f, half=width*.25f;
            var ink=new Color32(49,69,76,255); var teal=Critical?new Color32(226,142,95,255):new Color32(67,186,173,255);
            ChallengeArt.Circle(p,new Vector2(x,height*.53f),width*.43f,new Color32(221,238,224,255),Color.clear,0);
            Rect(p,x-half+5,bottom,half*2,9,new Color32(171,195,180,255));
            Rect(p,x-6,top-27,12,32,ink);Rect(p,x-27,top-28,54,10,new Color32(239,156,112,255));
            Rect(p,x-half,top,half*2,bottom-top,new Color32(249,252,238,255));
            float level=Mathf.Lerp(bottom-5,top+5,Mathf.Clamp01(Fill));
            Rect(p,x-half+5,level,half*2-10,bottom-level-4,teal);
            ChallengeArt.Line(p,ink,5,new Vector2(x-half,top+15),new Vector2(x-half,bottom),new Vector2(x+half,bottom),new Vector2(x+half,top+15));
            ChallengeArt.Line(p,ink,5,new Vector2(x-half,top+15),new Vector2(x-half+16,top),new Vector2(x+half-16,top),new Vector2(x+half,top+15));
            Rect(p,x-half+12,top+30,11,bottom-top-50,new Color(1,1,1,.3f));
            for(int i=1;i<=5;i++)ChallengeArt.Line(p,new Color(1,1,1,.65f),3,new Vector2(x+half-23,top+i*(bottom-top)/6),new Vector2(x+half-8,top+i*(bottom-top)/6));
            for(int i=0;i<4;i++)
            {
                float y=bottom-25-i*37;
                if(y>level+10)ChallengeArt.Circle(p,new Vector2(x-12+(i%2)*21,y),4+i%2,new Color(1,1,1,.4f),Color.clear,0);
            }
            ChallengeArt.Line(p,ink,7,new Vector2(x+half,bottom-24),new Vector2(x+half+24,bottom-24),new Vector2(x+half+24,top+28));
            ChallengeArt.Circle(p,new Vector2(x+half+24,top+24),14,new Color32(248,204,93,255),ink,3);
            ChallengeArt.Line(p,ink,3,new Vector2(x+half+24,top+24),new Vector2(x+half+30,top+18));
        }
    }
}
