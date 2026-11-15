using System;
using System.Collections.Generic;
using Ares.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.World
{
    /// <summary>Original Sky.tsx geometry and seeded stars; presentation reads the campaign clock.</summary>
    [DefaultExecutionOrder(220)]
    public sealed class ReferenceCelestials : MonoBehaviour
    {
        public const int SkyLayer = 30;
        public const int MapExcludedLayers = (1 << SkyLayer) | (1 << 31);
        [Serializable] sealed class Stars { public Layer[] layers; }
        [Serializable] sealed class Layer { public float[] positions; public float size,speed; }
        public TextAsset starData;
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Material> materials=new List<Material>();
        Transform bodies;
        Transform[] stars;
        Layer[] layers;
        ReferenceExplorer explorer;
        ReferenceEnvironment environment;
        GameFrontend frontend;
        static readonly float[] Radius={120,150,170},Speed={3.2f,.8f,.05f},Tilt={.5f,.3f,.6f},Phase={0,2,4};
        MeshRenderer[] planetRenderers;
        MaterialPropertyBlock properties;
        void OnEnable()
        {
            properties=new MaterialPropertyBlock();
            explorer=GetComponent<ReferenceExplorer>();environment=GetComponent<ReferenceEnvironment>();frontend=GetComponent<GameFrontend>();
            var imported=ReferenceRig.Find(transform,"sky");
            if(imported!=null)
            {
                PrepareImportedSky(imported);
                bodies=imported.Find("sky_0");
                if(bodies!=null && bodies.childCount>=4)
                    planetRenderers=bodies.GetChild(3).GetComponentsInChildren<MeshRenderer>(true);
                else bodies=null;
            }
            // Body placement must not depend on the optional star asset or its shader.
            UpdateBodies(Camera.main);
            var starShader=Resources.Load<Shader>("ARES/Effects/ReferenceStars");
            if(starData==null || starShader==null || !starShader.isSupported)return;
            layers=JsonUtility.FromJson<Stars>(starData.text)?.layers;
            if(layers==null)return;
            stars=new Transform[layers.Length];
            for(int j=0;j<layers.Length;j++)
            {
                var data=layers[j];int count=data.positions.Length/3;
                var vertices=new Vector3[count*4];var uv=new Vector2[count*4];var indices=new int[count*6];
                for(int i=0;i<count;i++)
                {
                    Vector3 p=new Vector3(data.positions[i*3],data.positions[i*3+1],-data.positions[i*3+2]);
                    for(int k=0;k<4;k++)vertices[i*4+k]=p;
                    uv[i*4]=new Vector2(-1,-1);uv[i*4+1]=new Vector2(-1,1);uv[i*4+2]=new Vector2(1,1);uv[i*4+3]=new Vector2(1,-1);
                    int v=i*4,n=i*6;indices[n]=v;indices[n+1]=v+1;indices[n+2]=v+2;indices[n+3]=v;indices[n+4]=v+2;indices[n+5]=v+3;
                }
                var mesh=new Mesh{name="Reference stars "+j,vertices=vertices,uv=uv,triangles=indices};mesh.RecalculateBounds();meshes.Add(mesh);
                var material=new Material(starShader);material.SetFloat("_Size",data.size);materials.Add(material);
                var go=new GameObject(mesh.name){layer=SkyLayer};go.transform.SetParent(transform,false);stars[j]=go.transform;
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;ConfigureRenderer(renderer);
            }
        }
        void LateUpdate()
        {
            var camera=Camera.main;if(camera==null)return;
            UpdateBodies(camera);
            if(stars==null)return;
            float day=environment!=null?environment.Daylight:1;
            for(int j=0;j<stars.Length;j++)
            {
                stars[j].position=new Vector3(camera.transform.position.x*layers[j].speed,0,camera.transform.position.z*layers[j].speed);
                if(explorer==null||!explorer.reducedMotion)stars[j].Rotate(0,-Time.deltaTime*.004f*(1+layers[j].speed*4)*Mathf.Rad2Deg,0);
                materials[j].SetFloat("_Opacity",1-day*.95f);
            }
        }
        void UpdateBodies(Camera camera)
        {
            if(bodies==null)return;
            PositionBodies(bodies,camera!=null?camera.transform.position:Vector3.zero,frontend?.Campaign?.SolFraction??.35f);
            float day=environment!=null?environment.Daylight:1;
            foreach(var renderer in planetRenderers)
            {
                if(renderer==null||renderer.sharedMaterial==null)continue;
                var color=renderer.sharedMaterial.GetColor("_BaseColor");color.a=.25f+(1-day)*.6f;
                properties.SetColor("_BaseColor",color);renderer.SetPropertyBlock(properties);
            }
        }
        // Used by both the importer and runtime: the saved prefab is safe even before Play.
        public static void PrepareImportedSky(Transform imported)
        {
            foreach(var child in imported.GetComponentsInChildren<Transform>(true))child.gameObject.layer=SkyLayer;
            foreach(var renderer in imported.GetComponentsInChildren<Renderer>(true))ConfigureRenderer(renderer);
            var root=imported.Find("sky_0");
            if(root!=null&&root.childCount>=4)PositionBodies(root,Vector3.zero,.35f);
        }
        static void ConfigureRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
        static void PositionBodies(Transform root,Vector3 eye,float time)
        {
            for(int i=0;i<3;i++)
            {
                float a=time*Mathf.PI*2*Speed[i]+Phase[i];
                root.GetChild(i).position=new Vector3(eye.x+Mathf.Cos(a)*Radius[i],40+Mathf.Sin(a)*Radius[i]*Tilt[i]+Radius[i]*.25f,eye.z-Mathf.Sin(a)*Radius[i]);
            }
            root.GetChild(3).position=new Vector3(eye.x-150,70,eye.z+120);
        }
        void OnDisable()
        {
            if(stars!=null)foreach(var star in stars)if(star!=null)Destroy(star.gameObject);
            foreach(var mesh in meshes)Destroy(mesh);foreach(var material in materials)Destroy(material);
            meshes.Clear();materials.Clear();stars=null;
        }
    }
}
