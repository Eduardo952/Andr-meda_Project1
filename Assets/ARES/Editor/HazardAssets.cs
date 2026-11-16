using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.Domain.Hazards;
using Ares.Infrastructure.Nasa;
using Ares.Infrastructure.Hazards;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class HazardAssets
    {
        const string Root="Assets/ARES/Resources/ARES/Hazards/";
        static HazardAssets(){EditorApplication.delayCall+=Ensure;}
        [MenuItem("ARES/Hazards/Create missing configuration assets")]
        public static void Ensure()
        {
            if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)return;
            Directory.CreateDirectory(Root);
            if(!File.Exists(Root+"ScenarioProfile.asset"))AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ScenarioProfile>(),Root+"ScenarioProfile.asset");
            if(!File.Exists(Root+"AudioProfile.asset"))AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Ares.World.HazardAudioProfile>(),Root+"AudioProfile.asset");
            if(!File.Exists(Root+"HazardMixer.mixer")) {
                try {
                    var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t=>t!=null);
                    var method=type?.GetMethod("CreateMixerControllerAtPath",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,new[]{typeof(string)},null);
                    if(method==null)Debug.LogWarning("ARES hazards: create HazardMixer through Assets/Create/Audio/Audio Mixer; internal creation API unavailable.");
                    else method.Invoke(null,new object[]{Root+"HazardMixer.mixer"});
                }catch(Exception e){Debug.LogWarning("ARES hazards mixer creation: "+e.GetType().Name);}
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("ARES/Hazards/Export PROVISIONAL balance")]
        static void Export(){var profile=AssetDatabase.LoadAssetAtPath<ScenarioProfile>(Root+"ScenarioProfile.asset");File.WriteAllText(Root+"provisional-balance.json",NasaJson.Write(profile==null?new HazardBalance():profile.provisionalBalance));AssetDatabase.Refresh();}
    }
}
