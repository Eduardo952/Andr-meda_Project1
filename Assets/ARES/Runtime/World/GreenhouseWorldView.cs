using System;
using System.Collections.Generic;
using Ares.Domain;
using Ares.Data;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Imported can, shelf markers and watering stream driven by the exported garden layout.</summary>
    public sealed class GreenhouseWorldView : IDisposable
    {
        readonly Transform root,astronaut;
        readonly GreenhouseCanView can;
        readonly GardenLayoutData layout;
        readonly AstronautAnimator actor;
        readonly Material markerMaterial;
        readonly LineRenderer[] zones=new LineRenderer[4];
        readonly TextMesh[] labels=new TextMesh[6];
        readonly LineRenderer[] drops=new LineRenderer[8];
        readonly List<(Renderer renderer,Color original,int shelf)> foliage=new List<(Renderer,Color,int)>();
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        public Vector3 GripPosition => can.GripPosition;
        public Vector3 HandPosition => actor.rightArm.TransformPoint(new Vector3(0,-.55f,0));
        public Vector3 SpoutPosition => can.SpoutPosition;
        public GreenhouseWorldView(Transform parent,Transform astronaut,GardenLayoutData layout)
        {
            this.astronaut=astronaut;this.layout=layout;
            actor=astronaut!=null?astronaut.GetComponent<AstronautAnimator>():null;
            var imported=parent.GetComponentsInChildren<MeshRenderer>();
            root=new GameObject("Greenhouse manual care").transform; root.SetParent(parent,false);
            markerMaterial=new Material(Resources.Load<Shader>("ARES/Effects/SolarDust"));
            markerMaterial.SetColor("_BaseColor",new Color(.3f,.76f,.85f));
            can=new GreenhouseCanView(parent,root,layout.pickup);
            for(int i=0;i<4;i++)
            {
                var row=layout.rows[i];Vector3 p=row.Approach;
                zones[i]=Line("Shelf "+(i+1)+" watering area",.055f);
                zones[i].loop=true; zones[i].positionCount=4;
                zones[i].SetPositions(new[]{p+new Vector3(-row.width*.5f,.13f,-.6f),p+new Vector3(row.width*.5f,.13f,-.6f),p+new Vector3(row.width*.5f,.13f,.6f),p+new Vector3(-row.width*.5f,.13f,.6f)});
                labels[i]=Label(row.Center+Vector3.up*4);
                foreach(var mesh in imported)
                {
                    var c=mesh.bounds.center;
                    if(Mathf.Abs(c.x-row.x)>row.width*.5f || Mathf.Abs(c.z-row.Center.z)>row.depth || c.y<.9f || c.y>4.3f || !mesh.sharedMaterial.HasProperty("_BaseColor"))continue;
                    Color color=mesh.sharedMaterial.GetColor("_BaseColor");
                    if(color.g>color.r*1.05f && color.g>color.b*1.15f) foliage.Add((mesh,color,i));
                }
            }
            labels[4]=Label(layout.pickup.Position+Vector3.up*1.8f);
            labels[5]=Label(layout.refill.Position+Vector3.up*2.1f);
            for(int i=0;i<drops.Length;i++) { drops[i]=Line("Water drop "+i,.035f); drops[i].positionCount=2; }
        }
        LineRenderer Line(string name,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=markerMaterial;line.widthMultiplier=width;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; return line;
        }
        TextMesh Label(Vector3 position)
        {
            var go=new GameObject("Care label");go.transform.SetParent(root,false);go.transform.position=position;
            var text=go.AddComponent<TextMesh>();text.fontSize=64;text.characterSize=.026f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.14f,.17f,.15f);
            var font=Resources.Load<Font>("ARES/UI/Fonts/Nunito");
            if(font!=null){text.font=font;go.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
            return text;
        }
        public void Update(GreenhouseWatering water,GreenhouseSimulation game,bool inside,int selected,bool reducedMotion,int? tutorialTarget=null)
        {
            root.gameObject.SetActive(water!=null); if(water==null)return;
            var camera=Camera.main; if(camera==null)return;
            bool pouring=water.ActiveShelf>=0 && inside;
            Vector3 facing=astronaut!=null?-astronaut.forward:Vector3.forward;
            if(water.HasCan && actor!=null && inside)
            {
                if(pouring)
                {
                    facing=PourTarget(water.ActiveShelf)-astronaut.position;
                    facing.y=0;
                    if(facing.sqrMagnitude>.01f)astronaut.rotation=Quaternion.LookRotation(-facing.normalized);
                }
                actor.rightArm.localRotation=ReferenceSpace.Euler(pouring?-2f:-.4f,0,0);
            }
            can.Update(actor,water.HasCan,inside,pouring,facing);
            for(int i=0;i<4;i++)
            {
                bool show = inside && (!tutorialTarget.HasValue || tutorialTarget.Value == i);
                zones[i].enabled=show;
                Color color=water.IsWatered(i)?new Color(.4f,.8f,.4f):selected==i?new Color(.2f,.9f,.85f):new Color(.86f,.73f,.4f,.6f);
                block.SetColor("_BaseColor",color); zones[i].SetPropertyBlock(block);
                labels[i].gameObject.SetActive(show);
                labels[i].text=$"{i+1} · {game.Shelves[i].Crop} · 10 recipientes\n{Ares.UI.GreenhouseNarrator.Soil(game.Shelves[i].Soil)} · {game.Shelves[i].DeliveredDoses}/{game.Shelves[i].RequiredDoses} regas";
            }
            labels[4].gameObject.SetActive(inside && (!tutorialTarget.HasValue || tutorialTarget.Value == -2)); labels[4].text=water.HasCan?"CLIQUE / E · DEVOLVER":"CLIQUE / E · PEGAR\n510 mL";
            labels[5].gameObject.SetActive(inside && (!tutorialTarget.HasValue || tutorialTarget.Value == -3)); labels[5].text=$"CLIQUE / E · ÁGUA\n{Ares.UI.GreenhouseNarrator.Litres(water.ReservoirMl)} / 11 L";
            foreach(var label in labels)label.text=Ares.UI.PresentationLocalizer.Text(label.text);
            foreach(var label in labels)label.transform.rotation=camera.transform.rotation;
            foreach(var item in foliage)
            {
                var health=water.PreviewHealth(item.shelf);
                block.SetColor("_BaseColor",Color.Lerp(item.original,new Color(.78f,.56f,.25f),health==ShelfHealth.AtRisk?.32f:health==ShelfHealth.Problem?.12f:0));
                item.renderer.SetPropertyBlock(block);
            }
            for(int i=0;i<drops.Length;i++)
            {
                drops[i].enabled=pouring && (!reducedMotion || i<3); if(!pouring)continue;
                Vector3 origin=SpoutPosition;
                Vector3 target=PourTarget(water.ActiveShelf)+new Vector3((i%3-1)*.18f,1.55f,0);
                float t=Mathf.Repeat(Time.time*1.4f+i*.13f,1);
                Vector3 point=Vector3.Lerp(origin,target,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.25f;
                drops[i].SetPosition(0,point); drops[i].SetPosition(1,point+Vector3.up*.08f);
            }
        }
        Vector3 PourTarget(int index)
        { var row=layout.rows[index];return new Vector3(Mathf.Clamp(astronaut.position.x,row.x-row.width*.5f+1.2f,row.x+row.width*.5f-1.2f),0,-row.z); }
        public void Dispose()
        {
            foreach(var item in foliage) if(item.renderer!=null)item.renderer.SetPropertyBlock(null);
            can.Dispose();UnityEngine.Object.Destroy(markerMaterial);UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
