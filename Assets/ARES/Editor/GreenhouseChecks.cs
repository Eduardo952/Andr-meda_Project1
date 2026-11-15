using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class GreenhouseChecks
    {
        const string Request="Temp/ares-greenhouse-check.request", Pending="ARES.GreenhouseChecks";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameFrontend ui;
        static Keyboard keyboard;
        static int stage, frame;
        static double started, next;
        static bool background;
        static GreenhouseChecks() { EditorApplication.update+=Tick; }
        static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
        static void ModelChecks()
        {
            var game=new GreenhouseSimulation();
            Require(game.WaterMl==11000 && game.Soil==SoilMoisture.Moderate && game.Light==GrowLight.Moderate,"Initial state");
            Require(game.Shelves.Select(s=>s.Crop).SequenceEqual(new[]{"Fruta","Legume","Verdura","Verdura"}),"Four shelf crops");
            var expected=new[]{SoilMoisture.Dry,SoilMoisture.Moderate,SoilMoisture.Moderate,SoilMoisture.Dry,SoilMoisture.Moderate,SoilMoisture.Wet,SoilMoisture.Moderate,SoilMoisture.Wet,SoilMoisture.Wet};
            for(int soil=0;soil<3;soil++)for(int level=0;level<3;level++)
                Require(GreenhouseSimulation.SoilAfter((SoilMoisture)soil,(IrrigationLevel)level)==expected[soil*3+level],"Irrigation table");
            Require(game.Decide(IrrigationLevel.Medium,GreenhouseAdjustment.MoreLight)==GreenhouseDecisionError.InvalidAdjustment && game.WaterMl==11000,"Irrelevant adjustment spent water");
            game.Decide(IrrigationLevel.Medium,GreenhouseAdjustment.None);
            Require(game.WaterMl==8960 && game.LastResult.Health==ShelfHealth.Healthy && game.Light==GrowLight.Low,"First balance/weather");
            game.Decide(IrrigationLevel.Medium,GreenhouseAdjustment.MoreLight);
            Require(game.WaterMl==6920 && game.LastResult.Light==GrowLight.Moderate && game.Soil==SoilMoisture.Dry && game.Light==GrowLight.Excessive,"Light correction/sun");
            Require(game.CanAdjust(IrrigationLevel.Low,GreenhouseAdjustment.MoreWater),"Dry water adjustment missing");
            game.Decide(IrrigationLevel.Low,GreenhouseAdjustment.MoreWater);
            Require(game.WaterMl==4880 && game.LastResult.UsedMl==2040 && game.LastResult.Soil==SoilMoisture.Moderate,"Final level accounting");
            var wet=new GreenhouseSimulation(); wet.Decide(IrrigationLevel.High,GreenhouseAdjustment.None); wet.Decide(IrrigationLevel.High,GreenhouseAdjustment.None);
            Require(wet.LastResult.Waterlogged && wet.LastResult.Health==ShelfHealth.AtRisk,"Waterlogging and risk");
            var repeated=new GreenhouseSimulation(); repeated.Decide(IrrigationLevel.High,GreenhouseAdjustment.None); repeated.Decide(IrrigationLevel.Medium,GreenhouseAdjustment.MoreLight);
            Require(repeated.LastResult.Health==ShelfHealth.AtRisk,"Repeated single-factor problems must become risk");
            var shortage=new GreenhouseSimulation(); for(int i=0;i<4;i++)shortage.Decide(IrrigationLevel.High,GreenhouseAdjustment.None);
            Require(shortage.WaterMl==1720,"Four high cycles arithmetic"); int cycle=shortage.Cycle; var last=shortage.LastResult;
            Require(shortage.Decide(IrrigationLevel.Medium,GreenhouseAdjustment.None)==GreenhouseDecisionError.InsufficientWater,"Unaffordable decision accepted");
            Require(shortage.WaterMl==1720 && shortage.Cycle==cycle && shortage.LastResult==last,"Rejected decision mutated state");
            shortage.Decide(IrrigationLevel.Low,GreenhouseAdjustment.None);
            Require(shortage.Finished && shortage.WaterMl==360,"Residual-water ending");
            Require(shortage.Decide(IrrigationLevel.Low,GreenhouseAdjustment.None)==GreenhouseDecisionError.Finished && shortage.WaterMl==360,"Ended game changed");
            Require(GreenhouseNarrator.Ending(shortage).Contains("0,36") && GreenhouseNarrator.Result(shortage.LastResult).Contains("1,72 − 1,36 = 0,36"),"Portuguese arithmetic/narration");
            var definition=AssetDatabase.LoadAssetAtPath<Ares.Data.GameDefinition>("Assets/ARES/Generated/GameDefinition.asset");
            var campaign=new Campaign(definition.campaign,definition.rules); campaign.CloseReport(); campaign.SetAllocation(new Ares.Data.Allocation{shield=campaign.EnergyPool}); campaign.StartSol();
            string id=campaign.Current.tasks.First(t=>t.room=="greenhouse").id;
            campaign.BeginTask(id,"greenhouse"); Require(!campaign.FinishGreenhouseTask() && !campaign.FinishTask(id,true,1,0),"Premature/bypass reward");
            GreenhouseCheckFixture.ManualCycle(campaign,IrrigationLevel.Low,GreenhouseAdjustment.None); campaign.CancelTask(); campaign.BeginTask(id,"greenhouse");
            Require(campaign.Greenhouse.WaterMl==9640,"Reopening refilled reservoir");
            Require(GreenhouseCheckFixture.Complete(campaign,id) && !campaign.FinishGreenhouseTask(),"Completion/duplicate guard");
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token=File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if(SessionState.GetString(Pending+".refresh","")!=token) { SessionState.SetString(Pending+".refresh",token); AssetDatabase.Refresh(); return; }
                File.Delete(Request);
                try { ModelChecks(); } catch(Exception error) { File.WriteAllText("Logs/ares-greenhouse.txt","FAIL model: "+error);return; }
                SessionState.SetBool(Pending,true); stage=0; frame=-1; started=EditorApplication.timeSinceStartup; next=started+1;
                background=Application.runInBackground; Application.runInBackground=true;
                File.WriteAllText("Logs/ares-greenhouse.txt","RUNNING "+DateTime.UtcNow.ToString("O")); EditorApplication.EnterPlaymode(); return;
            }
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||Time.frameCount==frame||EditorApplication.timeSinceStartup<next)return;
            frame=Time.frameCount; next=EditorApplication.timeSinceStartup+.3;
            try
            {
                Require(EditorApplication.timeSinceStartup-started<60,"Play test timeout");
                if(keyboard==null)
                {
                    ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); if(ui==null)return;
                    var explorer=ui.GetComponent<ReferenceExplorer>();
                    var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer); if(player==null)return;
                    ui.StartMission(); ui.Campaign.CloseReport(); Call("CloseOverlay"); ui.Campaign.SetAllocation(new Ares.Data.Allocation{shield=ui.Campaign.EnergyPool}); ui.Campaign.StartSol();
                    player.X=52; player.Z=0; player.VelocityX=player.VelocityZ=player.Speed=0;
                    keyboard=InputSystem.AddDevice<Keyboard>("ARES greenhouse keyboard");
                }
                var root=ui.GetComponent<UIDocument>().rootVisualElement; var game=ui.Campaign.Greenhouse;
                keyboard.MakeCurrent(); var keys=new KeyboardState();
                switch(stage)
                {
                    case 0: keys=new KeyboardState(Key.E); break;
                    case 1:
                        Require(root.Q("greenhouse-view")!=null && ui.BlocksWorld,"E did not open new activity");
                        Require(root.Q<Image>().image!=null && root.Query(className:"greenhouse-shelf").ToList().Count==4,"Art/four shelves missing");
                        Require(!root.Q<Button>("greenhouse-confirm").enabledInHierarchy,"Choice preselected");
                        ScreenCapture.CaptureScreenshot("Logs/ares-greenhouse-start.png"); break;
                    case 2: GreenhouseCheckFixture.Press(root,"greenhouse-level-1"); GreenhouseCheckFixture.Press(root,"greenhouse-confirm"); GreenhouseCheckFixture.ManualCycle(ui.Campaign,IrrigationLevel.Medium,GreenhouseAdjustment.None); GreenhouseCheckFixture.Reopen(ui); break;
                    case 3:
                        Require(game.WaterMl==8960 && root.Q<Label>("greenhouse-result").text.Contains("8,96"),"Native choice/result");
                        GreenhouseCheckFixture.Press(root,"greenhouse-level-1");
                        Require(root.Q<Button>("greenhouse-adjust-MoreLight")!=null && root.Q<Button>("greenhouse-adjust-LessLight")==null,"Context adjustment visibility");
                        ScreenCapture.CaptureScreenshot("Logs/ares-greenhouse-weather.png"); break;
                    case 4: keys=new KeyboardState(Key.Escape); break;
                    case 5: Require(!ui.BlocksWorld && ui.Campaign.OpenTask==null,"Close did not release task"); keys=new KeyboardState(Key.E); break;
                    case 6:
                        Require(game.WaterMl==8960 && game.Cycle==2,"Reopening reset state");
                        GreenhouseCheckFixture.Press(root,"greenhouse-level-1"); GreenhouseCheckFixture.Press(root,"greenhouse-adjust-MoreLight"); GreenhouseCheckFixture.Press(root,"greenhouse-confirm");
                        GreenhouseCheckFixture.ManualCycle(ui.Campaign,IrrigationLevel.Medium,GreenhouseAdjustment.MoreLight);GreenhouseCheckFixture.Reopen(ui);
                        Require(game.LastResult.Light==GrowLight.Moderate,"Light button did not correct light"); break;
                    case 7: GreenhouseCheckFixture.CompleteThroughControls(root,ui); break;
                    case 8:
                        Require(root.Q("greenhouse-ending")!=null && root.Q<Button>("greenhouse-confirm")==null,"Ending still accepts cycles");
                        root.Q<TextField>("greenhouse-reflection").value="A água é limitada e a luz ajuda as plantas.";
                        Require(game.Reflection.Contains("limitada"),"Reflection not saved");
                        var footer=root.Q(className:"mini-footer"); Require(footer.worldBound.yMax<=root.worldBound.yMax+1,"Footer outside viewport");
                        ScreenCapture.CaptureScreenshot("Logs/ares-greenhouse-end.png"); break;
                    case 9: GreenhouseCheckFixture.Press(root,"greenhouse-accept"); break;
                    case 10:
                        Require(!ui.BlocksWorld && ui.Campaign.Completed("SmartFarmCare"),"Lesson not recorded in campaign");
                        Finish("PASS: initial/crop states; all 9 soil transitions; exact ml and final-level cost; weather and light correction; repeated-problem risk; insufficiency without mutation; residual ending; reward guards; actual E/Escape/reopen; native decisions/reflection; generated art and four shelves; screenshots inspected separately."); return;
                }
                stage++; InputSystem.QueueStateEvent(keyboard,keys);
            }
            catch(Exception error) { Finish("FAIL stage "+stage+": "+error); }
        }
        static void Call(string name)=>typeof(GameFrontend).GetMethod(name,Private).Invoke(ui,null);
        static void Finish(string result)
        {
            File.WriteAllText("Logs/ares-greenhouse.txt",DateTime.UtcNow.ToString("O")+" "+result); SessionState.SetBool(Pending,false);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard); keyboard=null; Application.runInBackground=background; EditorApplication.ExitPlaymode();
        }
    }
}
