using System;
using System.IO;
using Ares.Infrastructure;
using Ares.Infrastructure.Hazards;
using Ares.Infrastructure.Nasa;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    public static class HazardCatalogMenu
    {
        static bool busy;
        [MenuItem("ARES/Hazards/Rebuild NASA catalog (Play Mode)")]
        static async void Build()
        {
            if(busy)return;var integration=UnityEngine.Object.FindAnyObjectByType<MissionIntegration>();
            if(!EditorApplication.isPlaying || integration==null){Debug.Log("ARES: enter Play Mode, then rebuild the catalog. This reuses the existing NASA service and local credential configuration.");return;}
            busy=true;
            try{await integration.Ready;var profile=Resources.Load<ScenarioProfile>("ARES/Hazards/ScenarioProfile");var catalog=await new HazardCatalogBuilder(integration.Nasa,"Temp/HazardCatalogScan",profile?.provisionalBalance).BuildAsync(DateTime.UtcNow,message=>Debug.Log("ARES catalog: "+message));File.WriteAllText("Assets/ARES/Resources/ARES/Hazards/catalog.json",NasaJson.Write(catalog));AssetDatabase.Refresh();Debug.Log("ARES catalog: "+catalog.days.Length+" verified dates saved.");}
            catch(Exception e){Debug.LogWarning("ARES catalog interrupted: "+e.GetType().Name+". Cached blocks remain resumable.");}finally{busy=false;}
        }
        [MenuItem("ARES/Checks/Hazard domain and 200 missions")]
        static async void Check(){try{Debug.Log(await HazardChecks.RunAsync(Directory.GetCurrentDirectory()));}catch(Exception e){Debug.LogException(e);}}
    }
}
