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
using MouseButton=UnityEngine.InputSystem.LowLevel.MouseButton;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class ManualWateringChecks
    {
        const string Request="Temp/ares-watering-check.request",Pending="ARES.ManualWatering";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static Keyboard keyboard;static Mouse mouse;static GameFrontend ui;static ReferenceExplorer explorer;
        static int stage,frame,shelf;static float elapsed,yaw;static int savedWater;static bool background;static double started;
        static ManualWateringChecks(){EditorApplication.update+=Tick;}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void ModelChecks()
        {
            foreach(IrrigationLevel level in Enum.GetValues(typeof(IrrigationLevel)))
            {
                var game=new GreenhouseSimulation();var water=new GreenhouseWatering(game);
                Require(!water.Fill(),"Fill without tool");water.Plan(level,GreenhouseAdjustment.None);
                water.Tick(0,true,false,4);Require(water.DeliveredMl==0,"Water without tool");
                water.PickUp();Require(water.Fill(),"Fill failed");int total=GreenhouseSimulation.ConsumptionMl(level);
                Require(water.ReservoirMl+water.CanMl==11000 && game.CompletedCycles==0,"Filling spent/created water");
                Require(!water.Fill(),"Overfilled planned dose");
                water.Tick(0,true,false,1);int applied=water.DeliveredMl;int held=water.CanMl;
                Require(applied>0 && applied<total/4,"Progressive flow");
                water.Tick(0,false,false,1);water.Tick(0,true,true,1);water.Tick(-1,true,false,1);
                Require(water.DeliveredMl==applied && water.CanMl==held,"Release/pause/outside lost water");
                water.Tick(0,true,false,3);held=water.CanMl;water.Tick(0,true,false,3);
                Require(water.CanMl==held && water.WateredShelves==1,"Duplicate shelf irrigation");
                Require(water.Plan(IrrigationLevel.High,GreenhouseAdjustment.None)==GreenhouseDecisionError.InvalidChoice,"Overwrote partial plan");
                for(int i=1;i<4;i++)water.Tick(i,true,false,3);
                Require(game.WaterMl==11000-total && water.CanMl==0 && water.ReservoirMl==game.WaterMl,"Cycle water conservation");
                Require(game.CompletedCycles==1 && water.CompletedManualCycles==1 && !water.HasPlan,"Atomic cycle completion");
            }
            var carriedGame=new GreenhouseSimulation();var carried=new GreenhouseWatering(carriedGame);carried.PickUp();carried.Fill();
            carried.Plan(IrrigationLevel.Low,GreenhouseAdjustment.None);for(int i=0;i<4;i++)carried.Tick(i,true,false,3);
            Require(carried.CanMl==960 && carried.ReservoirMl==8680 && carriedGame.WaterMl==9640,"Carry surplus forward");
            Require(CartoonText.GlyphCount==52,"Alphabet must contain 52 mapped glyphs");
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token=File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if(SessionState.GetString(Pending+".refresh","")!=token){SessionState.SetString(Pending+".refresh",token);AssetDatabase.Refresh();return;}
                File.Delete(Request);try{ModelChecks();}catch(Exception error){File.WriteAllText("Logs/ares-watering.txt","FAIL model: "+error);return;}
                SessionState.SetBool(Pending,true);stage=0;frame=-1;elapsed=0;started=EditorApplication.timeSinceStartup;
                background=Application.runInBackground;Application.runInBackground=true;File.WriteAllText("Logs/ares-watering.txt","RUNNING "+DateTime.UtcNow.ToString("O"));EditorApplication.EnterPlaymode();return;
            }
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||frame==Time.frameCount)return;
            frame=Time.frameCount;elapsed+=Mathf.Min(Time.deltaTime,.05f);
            try
            {
                Require(EditorApplication.timeSinceStartup-started<90,"Timeout at stage "+stage);
                if(keyboard==null)
                {
                    ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if(ui==null || explorer==null || typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer)==null)return;
                    ui.StartMission();ui.Campaign.CloseReport();Call("CloseOverlay");ui.Campaign.SetAllocation(new Ares.Data.Allocation{shield=ui.Campaign.EnergyPool});ui.Campaign.StartSol();
                    keyboard=InputSystem.AddDevice<Keyboard>("ARES watering keyboard");mouse=InputSystem.AddDevice<Mouse>("ARES watering mouse");SetPlayer(GreenhouseCare.PickupPosition);if(!explorer.Isometric)explorer.ToggleCamera();elapsed=0;
                }
                keyboard.MakeCurrent();mouse.MakeCurrent();var keys=new KeyboardState();var pointer=new MouseState{position=new Vector2(Screen.width*.58f,Screen.height*.48f)};
                var w=ui.Campaign.Watering;var g=ui.Campaign.Greenhouse;var root=ui.GetComponent<UIDocument>().rootVisualElement;
                if(stage==0 && elapsed>.4f){keys=new KeyboardState(Key.E);Next();}
                else if(stage==1 && elapsed>.3f){Require(w.HasCan && !ui.BlocksWorld,"E pickup intercepted by task");Require(ui.GreenhouseCare.GripError<.001f,"Can handle not attached to palm");ScreenCapture.CaptureScreenshot("Logs/ares-watering-grip.png");SetPlayer(GreenhouseCare.RefillPosition);Next();}
                else if(stage==2 && elapsed>.3f){keys=new KeyboardState(Key.E);Next();}
                else if(stage==3 && elapsed>.3f){Require(w.CanMl==2320 && w.ReservoirMl==8680,"Actual refill");SetPlayer(new Vector3(52,0,0));Next();}
                else if(stage==4 && elapsed>.3f){keys=new KeyboardState(Key.E);Next();}
                else if(stage==5 && elapsed>.3f)
                {
                    GreenhouseCheckFixture.Press(root,"greenhouse-level-1");GreenhouseCheckFixture.Press(root,"greenhouse-confirm");
                    Require(w.HasPlan && !ui.BlocksWorld && g.CompletedCycles==0 && g.WaterMl==11000,"Planning auto-irrigated or trapped movement");
                    SetPlayer(GreenhouseCare.ShelfPositions[0]);Next();
                }
                else if(stage==6 && elapsed>.5f){yaw=explorer.CameraTargetYaw;Next();}
                else if(stage==7)
                {
                    pointer=pointer.WithButton(MouseButton.Left);pointer.delta=new Vector2(3,0);
                    if(elapsed>1){Require(w.Progress(0)>.2f && w.Progress(0)<.6f,"Actual held click did not flow");Require(ui.GreenhouseCare.GripError<.001f,"Pouring detached handle from palm");Require(Mathf.Abs(yaw-explorer.CameraTargetYaw)<.001f,"Watering dragged camera");ScreenCapture.CaptureScreenshot("Logs/ares-watering-action.png");Next();}
                }
                else if(stage==8 && elapsed>.25f){savedWater=w.CanMl;keys=new KeyboardState(Key.Escape);Next();}
                else if(stage==9)
                {
                    pointer=pointer.WithButton(MouseButton.Left);
                    if(elapsed>.5f){Require(ui.BlocksWorld && w.CanMl==savedWater,"Menu used water");keys=new KeyboardState(Key.Escape);Next();}
                }
                else if(stage==10 && elapsed>.25f){shelf=0;Next();}
                else if(stage==11)
                {
                    pointer=pointer.WithButton(MouseButton.Left);
                    if(elapsed>2.8f)
                    {
                        if(shelf<3){Require(w.IsWatered(shelf),"Shelf not irrigated "+shelf);shelf++;SetPlayer(GreenhouseCare.ShelfPositions[shelf]);elapsed=0;}
                        else {Require(g.CompletedCycles==1 && g.WaterMl==8960 && w.CanMl==280 && w.ReservoirMl==8680,"Four physical shelves did not commit exact cycle");ScreenCapture.CaptureScreenshot("Logs/ares-watering-complete.png");Next();}
                    }
                }
                else if(stage==12 && elapsed>.4f)
                {
                    Require(root.Q("greenhouse-care-hud").resolvedStyle.display==DisplayStyle.Flex,"Care HUD hidden");
                    var preview=new VisualElement();preview.name="alphabet-preview";preview.style.position=Position.Absolute;preview.style.left=420;preview.style.top=160;preview.style.paddingLeft=24;preview.style.paddingRight=24;preview.style.paddingTop=18;preview.style.paddingBottom=18;preview.style.backgroundColor=new Color(.96f,.94f,.87f);root.Add(preview);
                    foreach(var text in new[]{"ABCDEF","GHIJKL","MNOPQR","STUVWX","YZ0123","456789","ÀÁÂÃ ÉÊÍÓ","ÔÕÚÜ Ç!?%"})preview.Add(new CartoonText(text,42));Next();
                }
                else if(stage==13 && elapsed>.4f){ScreenCapture.CaptureScreenshot("Logs/ares-cartoon-alphabet.png");Next();}
                else if(stage==14 && elapsed>.3f){Finish("PASS: all irrigation doses; water conservation and surplus; partial release/pause; no duplicate shelf/plan; 52 glyph mappings; actual E pickup/refill/planning; held mouse on all four world racks; camera capture; menu blocking; exact 11 - 2.04 = 8.96 L with 0.28 L carried; HUD and visual captures.");return;}
                InputSystem.QueueStateEvent(keyboard,keys);InputSystem.QueueStateEvent(mouse,pointer);
            }
            catch(Exception error){Finish("FAIL stage "+stage+": "+error);}
        }
        static void Next(){stage++;elapsed=0;}
        static void Call(string method)=>typeof(GameFrontend).GetMethod(method,Private).Invoke(ui,null);
        static void SetPlayer(Vector3 position){var p=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);p.X=position.x;p.Z=-position.z;p.VelocityX=p.VelocityZ=p.Speed=0;}
        static void Finish(string result)
        {
            File.WriteAllText("Logs/ares-watering.txt",DateTime.UtcNow.ToString("O")+" "+result);SessionState.SetBool(Pending,false);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
            Application.runInBackground=background;EditorApplication.ExitPlaymode();
        }
    }
}
