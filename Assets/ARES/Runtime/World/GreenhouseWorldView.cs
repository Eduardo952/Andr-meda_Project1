using System;
using System.Collections.Generic;
using Ares.Domain;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Generated 2.5D can, world markers and watering stream; imported crop meshes are preserved.</summary>
    public sealed class GreenhouseWorldView : IDisposable
    {
        readonly Transform root,astronaut,can;
        readonly AstronautAnimator actor;
        readonly SpriteRenderer canRenderer;
        readonly Sprite canSprite;
        readonly Material markerMaterial;
        readonly LineRenderer[] zones=new LineRenderer[4];
        readonly TextMesh[] labels=new TextMesh[6];
        readonly LineRenderer[] drops=new LineRenderer[8];
        readonly List<(Renderer renderer,Color original,int shelf)> foliage=new List<(Renderer,Color,int)>();
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        public Vector3 GripPosition => can.position;
        public Vector3 HandPosition => actor.rightArm.TransformPoint(new Vector3(0,-.55f,0));
        public Vector3 SpoutPosition => can.TransformPoint(new Vector3((canRenderer.flipX?-1:1)*.76f,-.27f,0));
        public GreenhouseWorldView(Transform parent,Transform astronaut)
        {
            this.astronaut=astronaut;
            actor=astronaut!=null?astronaut.GetComponent<AstronautAnimator>():null;
            var imported=parent.GetComponentsInChildren<MeshRenderer>();
            root=new GameObject("Greenhouse manual care").transform; root.SetParent(parent,false);
            markerMaterial=new Material(Resources.Load<Shader>("ARES/Effects/SolarDust"));
            markerMaterial.SetColor("_BaseColor",new Color(.3f,.76f,.85f));
            can=new GameObject("Watering can - generated sprite").transform; can.SetParent(root,false);
            var texture=Resources.Load<Texture2D>("ARES/UI/Greenhouse/watering-can");
            // The pivot is the top carry handle, so the palm grips the handle rather than the can's body.
            canSprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.37f,.91f),texture.width/1.4f);
            canRenderer=can.gameObject.AddComponent<SpriteRenderer>(); canRenderer.sprite=canSprite;
            for(int i=0;i<4;i++)
            {
                Vector3 p=GreenhouseCare.ShelfPositions[i];
                zones[i]=Line("Shelf "+(i+1)+" watering area",.055f);
                zones[i].loop=true; zones[i].positionCount=4;
                zones[i].SetPositions(new[]{p+new Vector3(-2.1f,.13f,-.9f),p+new Vector3(2.1f,.13f,-.9f),p+new Vector3(2.1f,.13f,.9f),p+new Vector3(-2.1f,.13f,.9f)});
                labels[i]=Label(p+new Vector3(0,2.2f,i<2?-1.8f:1.8f));
                foreach(var mesh in imported)
                {
                    var c=mesh.bounds.center;
                    if(Mathf.Abs(c.x-p.x)>2.6f || Mathf.Abs(c.z-(i<2?-7:7))>1.8f || c.y<.9f || c.y>4.3f || !mesh.sharedMaterial.HasProperty("_BaseColor"))continue;
                    Color color=mesh.sharedMaterial.GetColor("_BaseColor");
                    if(color.g>color.r*1.05f && color.g>color.b*1.15f) foliage.Add((mesh,color,i));
                }
            }
            labels[4]=Label(GreenhouseCare.PickupPosition+Vector3.up*1.8f);
            labels[5]=Label(GreenhouseCare.RefillPosition+Vector3.up*2.1f);
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
        public void Update(GreenhouseWatering water,GreenhouseSimulation game,bool inside,int selected,bool reducedMotion)
        {
            root.gameObject.SetActive(water!=null); if(water==null)return;
            var camera=Camera.main; if(camera==null)return;
            bool pouring=water.ActiveShelf>=0 && inside;
            canRenderer.enabled=!water.HasCan || inside;
            Vector3 facing=astronaut!=null?-astronaut.forward:Vector3.forward;
            if(water.HasCan && actor!=null && inside)
            {
                if(pouring)
                {
                    facing=GreenhouseCare.ShelfPositions[water.ActiveShelf]+new Vector3(0,0,water.ActiveShelf<2?-2.5f:2.5f)-astronaut.position;
                    facing.y=0;
                    if(facing.sqrMagnitude>.01f)astronaut.rotation=Quaternion.LookRotation(-facing.normalized);
                }
                actor.rightArm.localRotation=ReferenceSpace.Euler(pouring?-2f:-.4f,0,0);
                can.position=HandPosition;
            }
            else can.position=GreenhouseCare.PickupPosition+Vector3.up*1;
            canRenderer.flipX=Vector3.Dot(facing,camera.transform.right)<0;
            can.rotation=camera.transform.rotation*Quaternion.Euler(0,0,pouring?(canRenderer.flipX?24:-24):0);
            for(int i=0;i<4;i++)
            {
                zones[i].enabled=inside;
                Color color=water.IsWatered(i)?new Color(.4f,.8f,.4f):selected==i?new Color(.2f,.9f,.85f):new Color(.86f,.73f,.4f,.6f);
                block.SetColor("_BaseColor",color); zones[i].SetPropertyBlock(block);
                labels[i].gameObject.SetActive(inside);
                labels[i].text=$"{i+1} · {game.Shelves[i].Crop}"+(water.HasPlan?$"\n{water.Progress(i)*100:F0}%":"");
            }
            labels[4].gameObject.SetActive(inside); labels[4].text=water.HasCan?"[E] DEVOLVER REGADOR":"[E] PEGAR REGADOR";
            labels[5].gameObject.SetActive(inside); labels[5].text=$"[E] ÁGUA\n{Ares.UI.GreenhouseNarrator.Litres(water.ReservoirMl)} L";
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
                Vector3 target=GreenhouseCare.ShelfPositions[water.ActiveShelf]+new Vector3((i%3-1)*.18f,.93f,water.ActiveShelf<2?-1.7f:1.7f);
                float t=Mathf.Repeat(Time.time*1.4f+i*.13f,1);
                Vector3 point=Vector3.Lerp(origin,target,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.25f;
                drops[i].SetPosition(0,point); drops[i].SetPosition(1,point+Vector3.up*.08f);
            }
        }
        public void Dispose()
        {
            foreach(var item in foliage) if(item.renderer!=null)item.renderer.SetPropertyBlock(null);
            UnityEngine.Object.Destroy(canSprite);UnityEngine.Object.Destroy(markerMaterial);UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
