using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>HUD-only outline artwork traced in the visual language of Anexo/UX.png.</summary>
    internal sealed class HudGlyph : VisualElement
    {
        readonly string kind;
        public HudGlyph(string kind, float size = 24)
        {
            this.kind=kind; pickingMode=PickingMode.Ignore;
            style.width=size;style.height=size;style.flexShrink=0;
            generateVisualContent+=Draw;
        }
        void Draw(MeshGenerationContext context)
        {
            var p=context.painter2D;
            Vector2 P(float x,float y)=>new Vector2(x*contentRect.width/24,y*contentRect.height/24);
            p.strokeColor=new Color32(40,33,57,255);p.lineWidth=1.7f*contentRect.width/24;p.lineCap=LineCap.Round;p.lineJoin=LineJoin.Round;
            void Line(params Vector2[] points){p.BeginPath();p.MoveTo(points[0]);for(int i=1;i<points.Length;i++)p.LineTo(points[i]);p.Stroke();}
            void Circle(float x,float y,float radius){p.BeginPath();p.Arc(P(x,y),radius*contentRect.width/24,0,360);p.ClosePath();p.Stroke();}
            void Rect(float x,float y,float w,float h,float r=1.5f)
            {
                p.BeginPath();p.MoveTo(P(x+r,y));p.LineTo(P(x+w-r,y));p.BezierCurveTo(P(x+w,y),P(x+w,y),P(x+w,y+r));
                p.LineTo(P(x+w,y+h-r));p.BezierCurveTo(P(x+w,y+h),P(x+w,y+h),P(x+w-r,y+h));
                p.LineTo(P(x+r,y+h));p.BezierCurveTo(P(x,y+h),P(x,y+h),P(x,y+h-r));
                p.LineTo(P(x,y+r));p.BezierCurveTo(P(x,y),P(x,y),P(x+r,y));p.ClosePath();p.Stroke();
            }
            switch(kind)
            {
                case "rover":
                    Rect(3,9,18,7,2);Rect(8,5,8,4,1);Line(P(12,5),P(12,2),P(17,2));
                    Circle(6,18,2.5f);Circle(12,18,2.5f);Circle(18,18,2.5f);break;
                case "integrity":
                    p.BeginPath();p.MoveTo(P(12,20));p.BezierCurveTo(P(9,17),P(2.5f,12.5f),P(2.5f,8));
                    p.BezierCurveTo(P(2.5f,3),P(8.5f,2),P(12,6));p.BezierCurveTo(P(15.5f,2),P(21.5f,3),P(21.5f,8));
                    p.BezierCurveTo(P(21.5f,12.5f),P(15,17),P(12,20));p.ClosePath();p.Stroke();break;
                case "oxygen":
                    p.BeginPath();p.MoveTo(P(3,8));p.LineTo(P(14,8));p.BezierCurveTo(P(19,8),P(19,2),P(15,4));p.Stroke();
                    p.BeginPath();p.MoveTo(P(2,12));p.LineTo(P(19,12));p.BezierCurveTo(P(24,12),P(23,5),P(20,7));p.Stroke();
                    p.BeginPath();p.MoveTo(P(5,16));p.LineTo(P(14,16));p.BezierCurveTo(P(18,16),P(18,22),P(14,20));p.Stroke();break;
                case "dose":
                    Circle(12,12,2);
                    for(int i=0;i<3;i++){
                        float a=-120+i*120;
                        Vector2 R(float radius,float angle)=>P(12+radius*Mathf.Cos(angle*Mathf.Deg2Rad),12+radius*Mathf.Sin(angle*Mathf.Deg2Rad));
                        p.BeginPath();p.MoveTo(R(5,a));p.LineTo(R(10,a));
                        for(int j=1;j<=8;j++)p.LineTo(R(10,a+j*7.5f));p.LineTo(R(5,a+60));
                        for(int j=7;j>=0;j--)p.LineTo(R(5,a+j*7.5f));p.ClosePath();p.Stroke();
                    }break;
                case "energy":Line(P(13,2),P(4,13),P(11,13),P(10,22),P(21,10),P(14,10),P(15,2),P(13,2));break;
                case "roof":Rect(4,4,16,16);Line(P(4,9),P(20,9));Line(P(10,9),P(10,20));break;
                case "focus":
                    Line(P(8,3),P(4,3),P(3,4),P(3,8));Line(P(16,3),P(20,3),P(21,4),P(21,8));
                    Line(P(3,16),P(3,20),P(4,21),P(8,21));Line(P(16,21),P(20,21),P(21,20),P(21,16));Circle(12,12,3);break;
                case "report":Rect(5,4,14,18);Rect(9,2,6,4,1);for(int y=10;y<=18;y+=4){Line(P(8,y),P(9,y));Line(P(12,y),P(16,y));}break;
                case "camera":
                    p.BeginPath();p.MoveTo(P(3,7));p.LineTo(P(7,7));p.LineTo(P(9,4));p.LineTo(P(15,4));p.LineTo(P(17,7));p.LineTo(P(21,7));p.LineTo(P(21,20));p.LineTo(P(3,20));p.ClosePath();p.Stroke();Circle(12,13,3.5f);break;
                case "settings":
                    p.BeginPath();for(int i=0;i<32;i++){float a=(i*11.25f-5.625f)*Mathf.Deg2Rad;float r=(i%4==0||i%4==3)?7:9.8f;var point=P(12+Mathf.Cos(a)*r,12+Mathf.Sin(a)*r);if(i==0)p.MoveTo(point);else p.LineTo(point);}p.ClosePath();p.Stroke();Circle(12,12,3);break;
                case "observatory":Rect(4,3,16,18);Line(P(9,3),P(9,21));Line(P(12,8),P(17,8));Line(P(12,12),P(17,12));Line(P(12,16),P(15,16));break;
                case "info":Circle(12,12,8);Line(P(12,11),P(12,16));Circle(12,7.5f,.5f);break;
            }
        }
    }
}
