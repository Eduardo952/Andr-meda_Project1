using Ares.Domain;
using Ares.Domain.Hazards;
using Ares.UI;
using UnityEngine;
using UnityEngine.Audio;

namespace Ares.World
{
    /// <summary>Original procedural loops, generated once. No downloads or per-frame audio allocations.</summary>
    public sealed class HazardAudioController:MonoBehaviour
    {
        readonly AudioSource[] sources=new AudioSource[7];readonly AudioClip[] clips=new AudioClip[7];
        GameFrontend frontend;ReferenceExplorer explorer;WeatherVfxController weather;
        HazardAudioProfile profile;Ares.Infrastructure.MissionIntegration integration;
        readonly bool[] generated=new bool[7];
        void Start()
        {
            frontend=GetComponent<GameFrontend>();explorer=GetComponent<ReferenceExplorer>();weather=GetComponent<WeatherVfxController>();
            profile=Resources.Load<HazardAudioProfile>("ARES/Hazards/AudioProfile");integration=GetComponent<Ares.Infrastructure.MissionIntegration>();
            var mixer=Resources.Load<AudioMixer>("ARES/Hazards/HazardMixer");var groups=mixer?.FindMatchingGroups("Hazards");if(groups==null||groups.Length==0)groups=mixer?.FindMatchingGroups("Master");
            for(int i=0;i<7;i++) {
                generated[i]=profile==null||profile.clips==null||profile.clips.Length<=i||profile.clips[i]==null;clips[i]=generated[i]?Synthesize((HazardKind)i):profile.clips[i];var source=gameObject.AddComponent<AudioSource>();sources[i]=source;
                source.clip=clips[i];source.playOnAwake=false;source.loop=true;source.volume=0;source.spatialBlend=0;
                if(groups!=null && groups.Length>0)source.outputAudioMixerGroup=groups[0];
            }
        }
        static AudioClip Synthesize(HazardKind kind)
        {
            const int rate=24000,length=rate*4;var samples=new float[length];var random=new System.Random(701+(int)kind);float low=0;int nextTick=0,lastTick=-1000;
            for(int i=0;i<length;i++) {
                float t=(float)i/rate,white=(float)random.NextDouble()*2-1;low=low*.96f+white*.04f;
                float fade=Mathf.Min(1,Mathf.Min(i,length-1-i)/240f);float value;
                switch(kind) {
                    case HazardKind.Dust:value=low*2.3f*(.7f+.2f*Mathf.Sin(t*Mathf.PI));break;
                    case HazardKind.Flare:value=white*.07f+low*.6f;break;
                    case HazardKind.Cme:value=Mathf.Sin(t*2*Mathf.PI*55)*.065f+low*.5f;break;
                    case HazardKind.Radiation:if(i>=nextTick){lastTick=i;nextTick=i+Mathf.Max(160,(int)(-System.Math.Log(System.Math.Max(.0001,random.NextDouble()))*rate/5));}value=i-lastTick<160?Mathf.Sin(i*1.7f)*.12f:0;break;
                    case HazardKind.Cold:value=(white-low)*.04f+Mathf.Sin(t*2*Mathf.PI*180)*.02f;break;
                    case HazardKind.LowOxygen:value=low*Mathf.Max(0,Mathf.Sin(t*Mathf.PI))*.9f;break;
                    default:value=Mathf.Sin(t*2*Mathf.PI*90)*.04f*(.7f+.3f*Mathf.Sin(t*2*Mathf.PI));break;
                }samples[i]=Mathf.Clamp(value*fade,-.5f,.5f);
            }
            var clip=AudioClip.Create("ARES procedural "+kind,length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void Update()
        {
            var c=frontend?.Campaign;bool enabled=c?.NativeHazards==true && c.Phase==Phase.Tasks && c.Condition.Stage!=CrewCondition.Incapacitated && !frontend.HazardPresentationPaused && PlayerPrefs.GetInt("ARES.HazardAudio",1)==1;
            float indoor=HabitatShelter.Contains(explorer.definition.layout,explorer.PlayerPosition)?(profile?.indoorWeatherGain??.35f):1;
            for(int i=0;i<sources.Length;i++) {
                var source=sources[i];if(source==null)continue;
                float level=i==(int)HazardKind.Dust?DustStormWeather.ForCampaign(c).Strength:weather.Strength((HazardKind)i);
                float target=enabled?level*(profile?.gain??.38f)*(i==(int)HazardKind.Dust||i==(int)HazardKind.Cold?indoor:1):0;
                if(frontend.BlocksWorld||integration?.Busy==true)target*=profile?.modalGain??.2f;
                if(i==(int)HazardKind.Radiation)source.pitch=Mathf.Clamp((c?.DoseRate??0)/8,.3f,2);
                source.volume=Mathf.MoveTowards(source.volume,target,Time.unscaledDeltaTime*.45f);
                if(source.volume>.001f && !source.isPlaying)source.Play();else if(source.volume<=.001f && source.isPlaying)source.Pause();
            }
        }
        void OnDisable(){foreach(var s in sources)if(s!=null)s.Stop();}
        void OnDestroy(){for(int i=0;i<clips.Length;i++)if(generated[i]&&clips[i]!=null)Destroy(clips[i]);foreach(var s in sources)if(s!=null)Destroy(s);}
    }
}
