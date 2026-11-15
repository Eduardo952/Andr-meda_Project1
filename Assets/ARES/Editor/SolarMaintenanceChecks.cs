using System;
using System.IO;
using System.Reflection;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace Ares.Editor
{
    /// <summary>Fixed local request: pure energy/hold checks, then virtual E and mouse events in the actual scene.</summary>
    [InitializeOnLoad]
    public static class SolarMaintenanceChecks
    {
        const string Request = "Temp/ares-solar-check.request", Pending = "ARES.SolarChecks";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static Keyboard keyboard;
        static Mouse mouse;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static SolarMaintenance solar;
        static float elapsed, yaw;
        static int stage, frame;
        static double started;
        static bool background;
        static SolarMaintenanceChecks() { EditorApplication.update += Tick; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void VerifyModel()
        {
            var settings = new SolarMaintenanceSettings();
            var plant = new SolarPlant(settings);
            Require(Math.Abs(plant.GeneratedKwh - 12.8f) < .001f && !plant.IsCritical && plant.DirtyPanels == 0, "Initial production/reserve");
            plant.Tick(179); Require(plant.DirtyPanels == 0, "Dust appeared before grace time");
            plant.Tick(40); Require(plant.DirtyPanels > 0 && plant.DirtyPanels < 32, "Dust must accumulate across panels gradually");
            plant.Tick(700); Require(plant.IsCritical && plant.DirtyPanels == 32 && plant.GeneratedKwh == 0 && plant.ReserveKwh >= 0, "Neglect must cause critical reserve and 32 dirty panels");
            var cleaning = new SolarCleaningSession(plant);
            Require(!cleaning.Tick(0, true, false, 5) && plant.Dust(0) == 1, "Cleaning without tool");
            cleaning.PickUpBroom(); cleaning.Tick(0, true, false, 2);
            Require(cleaning.Progress == .5f, "Hold progress");
            cleaning.Tick(0, false, false, 1); Require(cleaning.Progress == 0, "Release must cancel");
            cleaning.Tick(0, true, false, 2); cleaning.Tick(1, true, false, 1);
            Require(cleaning.Progress == .25f, "Switching panels must reset hold");
            cleaning.Tick(-1, true, false, 1); Require(cleaning.Progress == 0, "Leaving area must cancel");
            cleaning.Tick(0, true, false, 2); cleaning.Tick(0, true, true, 3);
            Require(cleaning.Progress == 0 && plant.Dust(0) == 1, "Blocked input must cancel");
            for (int i=0;i<32;i++) Require(cleaning.Tick(i,true,false,4), "Clean panel " + i);
            Require(plant.DirtyPanels == 0 && plant.CleanedPanels == 32 && Math.Abs(plant.GeneratedKwh-12.8f)<.001f, "Restore generation");
            Require(!cleaning.Tick(0,true,false,5) && plant.CleanedPanels == 32, "No duplicate cleaning");
            float reserve = plant.ReserveKwh; plant.Tick(30);
            Require(plant.ReserveKwh > reserve && plant.DirtyPanels == 0, "Cleaning must recharge over time and reset dust age");
            var boundary = new SolarPlant(new SolarMaintenanceSettings {initialReserveKwh=100,consumptionKwhPerCycle=13});
            Require(!boundary.IsCritical, "Exactly 100 is not below threshold"); boundary.Tick(1); Require(boundary.IsCritical, "Below 100 must alert");
            float before = boundary.ReserveKwh; boundary.Tick(float.NaN); boundary.Tick(-1); Require(boundary.ReserveKwh==before, "Invalid delta");
            var recovery = new SolarPlant(new SolarMaintenanceSettings {initialReserveKwh=99});
            Require(recovery.IsCritical,"Recovery fixture"); recovery.Tick(30);
            Require(!recovery.IsCritical && recovery.ReserveKwh>100,"Recharging must clear critical state");
            var definition = AssetDatabase.LoadAssetAtPath<Ares.Data.GameDefinition>("Assets/ARES/Generated/GameDefinition.asset");
            var campaign = new Campaign(definition.campaign,definition.rules);
            campaign.Tick(1000,false); Require(campaign.Solar.DirtyPanels==0, "Report must pause maintenance");
            campaign.CloseReport(); campaign.SetAllocation(new Ares.Data.Allocation {shield=campaign.EnergyPool}); campaign.StartSol();
            campaign.Tick(1000,true); Require(campaign.Solar.DirtyPanels==0, "Overlay must pause maintenance");
            campaign.Tick(1000,false); Require(campaign.Solar.IsCritical, "Campaign must advance maintenance during gameplay");
        }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token = File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if (SessionState.GetString(Pending+".refresh", "") != token) { SessionState.SetString(Pending+".refresh",token); AssetDatabase.Refresh(); return; }
                File.Delete(Request);
                try { VerifyModel(); }
                catch (Exception error) { File.WriteAllText("Logs/ares-solar.txt", "FAIL model: "+error); return; }
                SessionState.SetBool(Pending,true); stage=0; frame=-1; elapsed=0; started=EditorApplication.timeSinceStartup;
                background=Application.runInBackground; Application.runInBackground=true;
                File.WriteAllText("Logs/ares-solar.txt", "RUNNING "+DateTime.UtcNow.ToString("O"));
                EditorApplication.isPaused=false; EditorApplication.EnterPlaymode(); return;
            }
            if (!SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying) return;
            try
            {
                Require(EditorApplication.timeSinceStartup-started<70,"Runtime timeout at stage "+stage);
                if(frame==Time.frameCount)return; frame=Time.frameCount; elapsed+=Mathf.Min(Time.deltaTime,.05f);
                if(keyboard==null)
                {
                    ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if(ui==null || explorer==null || ui.Solar==null)return;
                    solar=ui.Solar; Require(solar.BoundPanels==32,"Scene panel binding");
                    Require(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("ARES/Effects/SolarDust")),"Solar shader errors");
                    keyboard=InputSystem.AddDevice<Keyboard>("ARES solar keyboard"); mouse=InputSystem.AddDevice<Mouse>("ARES solar mouse");
                    ui.StartMission(); ui.Campaign.CloseReport(); Call("CloseOverlay");
                    ui.Campaign.SetAllocation(new Ares.Data.Allocation {shield=ui.Campaign.EnergyPool}); ui.Campaign.StartSol();
                    SetPlayer(solar.PanelCenter(0)+Vector3.forward*2.3f); explorer.RecenterCamera();
                    elapsed=0;
                }
                keyboard.MakeCurrent(); mouse.MakeCurrent();
                var keys=new KeyboardState(); var pointer=new MouseState {position=new Vector2(Screen.width*.52f,Screen.height*.5f)};
                if(stage==0 && elapsed>.8f)
                {
                    ScreenCapture.CaptureScreenshot("Logs/ares-solar-clean.png"); stage=20; elapsed=0;
                }
                else if(stage==20 && elapsed>.25f) { solar.Plant.Tick(650); stage=1; elapsed=0; }
                else if(stage==1)
                {
                    pointer=pointer.WithButton(MouseButton.Left);
                    if(elapsed>.5f) { Require(solar.Cleaning.Progress==0,"World cleaning without broom");
                        Require(solar.Plant.IsCritical,"Critical fixture");
                        Require(ui.GetComponent<UIDocument>().rootVisualElement.Q<Label>("solar-critical").resolvedStyle.display==DisplayStyle.Flex,"Critical alert not visible");
                        ScreenCapture.CaptureScreenshot("Logs/ares-solar-dirty.png"); stage=21; elapsed=0; }
                }
                else if(stage==21 && elapsed>.25f) { SetPlayer(SolarBroomView.PickupPosition+Vector3.back); stage=2; elapsed=0; }
                else if(stage==2 && elapsed>.4f) { Require(solar.CanPickUp,"Pickup zone"); keys=new KeyboardState(Key.E); Next(); }
                else if(stage==3 && elapsed>.2f)
                {
                    Require(solar.Cleaning.HasBroom && !ui.BlocksWorld,"E must pick up broom before station task");
                    SetPlayer(solar.PanelCenter(0)+Vector3.forward*2.3f); Next();
                }
                else if(stage==4 && elapsed>.4f) { yaw=explorer.CameraTargetYaw; Next(); }
                else if(stage==5)
                {
                    pointer=pointer.WithButton(MouseButton.Left); pointer.delta=new Vector2(4,0);
                    if(elapsed>1.2f) { Require(solar.Cleaning.Progress>.2f,"Actual mouse hold did not progress; focused="+Application.isFocused); Require(Mathf.Abs(yaw-explorer.CameraTargetYaw)<.001f,"Cleaning rotated camera"); ScreenCapture.CaptureScreenshot("Logs/ares-solar-cleaning.png"); Next(); }
                }
                else if(stage==6 && elapsed>.25f) { Require(solar.Cleaning.Progress==0,"Mouse release must cancel in scene"); Next(); }
                else if(stage==7)
                {
                    pointer=pointer.WithButton(MouseButton.Left);
                    if(elapsed>4.3f) { Require(solar.Plant.Dust(0)==0 && solar.Plant.CleanedPanels==1,"Held click must clean exactly one panel"); ScreenCapture.CaptureScreenshot("Logs/ares-solar-restored.png"); Next(); }
                }
                else if(stage==8 && elapsed>.15f)
                {
                    ui.StartMission(); Require(solar.Plant.CleanedPanels==0 && !solar.Cleaning.HasBroom && !solar.Plant.IsCritical,"Restart reset");
                    Finish("PASS: 32 cell bindings; 0.4 kWh each; gradual dust/neglect/critical reserve; recharge; hold cancellation and tool requirement; actual E pickup and mouse cleaning; camera capture; HUD alert; mission reset. Screenshots in Logs/ares-solar-*.png."); return;
                }
                InputSystem.QueueStateEvent(keyboard,keys); InputSystem.QueueStateEvent(mouse,pointer);
            }
            catch(Exception error) { Finish("FAIL runtime stage "+stage+": "+error); }
        }
        static void Next() { stage++; elapsed=0; }
        static void Call(string method) => typeof(GameFrontend).GetMethod(method,Private).Invoke(ui,null);
        static void SetPlayer(Vector3 position)
        {
            var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);
            player.X=position.x; player.Z=-position.z; player.VelocityX=player.VelocityZ=player.Speed=0;
        }
        static void Finish(string message)
        {
            File.WriteAllText("Logs/ares-solar.txt",DateTime.UtcNow.ToString("O")+" "+message);
            SessionState.SetBool(Pending,false);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard); if(mouse!=null)InputSystem.RemoveDevice(mouse);
            keyboard=null; mouse=null; Application.runInBackground=background; EditorApplication.ExitPlaymode();
        }
    }
}
