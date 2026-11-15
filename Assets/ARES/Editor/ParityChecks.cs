using System;
using System.IO;
using Ares.Data;
using Ares.Domain;
using Ares.World;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    public static class ParityChecks
    {
        [Serializable] sealed class Fixtures { public NavigationCase[] navigation; public RuleCase[] rules; public CameraCase[] camera; }
        [Serializable] sealed class CameraCase { public float dx,dy,factor,dt,yaw,pitch,distance,x,y,z; public bool isometric; }
        [Serializable] sealed class NavigationCase { public float x,z; public bool walkable; }
        [Serializable] sealed class RuleCase { public string flare; public float magnitude, speed, temperature; public int shield, thermal; }
        [MenuItem("ARES/Validate port against reference")]
        public static void Run()
        {
            var fixtures = JsonUtility.FromJson<Fixtures>(File.ReadAllText("Assets/ARES/Import/parity.json"));
            var layout = JsonUtility.FromJson<LayoutData>(File.ReadAllText("Assets/ARES/Import/layout.json"));
            var navigation = new WorldNavigation(layout);
            var config = JsonUtility.FromJson<WorldConfiguration>(File.ReadAllText("Assets/ARES/Import/balance.json"));
            var rig = new OrbitCamera(config.camera, layout);
            foreach (var item in fixtures.camera)
            {
                rig.Orbit(item.dx,item.dy,1,false,item.isometric); rig.Zoom(item.factor); rig.Step(item.dt,item.isometric);
                Require(Mathf.Abs(rig.Yaw-item.yaw)<.0001f && Mathf.Abs(rig.Pitch-item.pitch)<.0001f && Mathf.Abs(rig.Distance-item.distance)<.0001f,"Camera state mismatch");
                Require(Vector3.Distance(rig.Position(0,4,true),new Vector3(item.x,item.y,-item.z))<.0001f,"Camera position mismatch");
            }
            foreach(var item in fixtures.navigation) Require(navigation.Walkable(new Vector3(item.x,0,-item.z)) == item.walkable, "Navigation mismatch at " + item.x + "," + item.z);
            foreach(var item in fixtures.rules)
            {
                Require(Rules.Shield(item.flare.Length == 0 ? (char?)null : item.flare[0], item.magnitude, item.speed < 0 ? (double?)null : item.speed) == item.shield, "Shield mismatch");
                Require(Rules.Thermal(item.temperature) == item.thermal, "Thermal mismatch");
            }
            var data = JsonUtility.FromJson<CampaignData>(File.ReadAllText("Assets/ARES/Import/campaign.json"));
            var campaign = new Campaign(data);
            Require(!campaign.StartSol(), "Cannot skip report");
            for(int i=0;i<data.sols.Length;i++)
            {
                campaign.CloseReport(); var allocation = new Allocation {shield=40,life=40,greenhouse=20};
                Require(!campaign.SetAllocation(new Allocation {shield=101}), "Invalid budget accepted");
                Require(campaign.SetAllocation(allocation) && campaign.StartSol(), "Cannot start Sol");
                Require(!campaign.Seal("control",allocation), "Unfinished tasks allowed sealing");
                float before=campaign.SolFraction; campaign.Tick(5,true); Require(before==campaign.SolFraction,"Clock did not pause");
                foreach(var task in campaign.Current.tasks)
                {
                    Require(!campaign.BeginTask(task.id,"wrong-room"),"Remote task accepted");
                    Require(campaign.BeginTask(task.id,task.room),"Assigned task refused");
                    Require(!campaign.FinishTask(task.id,false,10,0),"Unsolved puzzle accepted");
                    Require(campaign.FinishTask(task.id,true,10,0),"Solved puzzle refused");
                    Require(!campaign.FinishTask(task.id,true,10,0),"Duplicate reward accepted");
                }
                Require(!campaign.Seal("solar",allocation),"Remote sealing accepted");
                Require(campaign.Seal("control",allocation),"Sealing failed");
                Require(!campaign.Seal("control",allocation),"Double sealing accepted"); campaign.NextSol();
            }
            Require(campaign.Phase==Phase.Victory,"Campaign did not finish");
            Require(Rules.Score(float.NaN,0)==0 && Rules.Score(61,0)==0 && Rules.Score(30,0)==3 && Rules.Score(45,2)==2,"Score boundary mismatch");
            string characters = CharacterAnimationChecks.Run();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ares-parity.txt",$"PASS: {fixtures.navigation.Length} navigation, {fixtures.rules.Length} rule fixtures, {fixtures.camera.Length} camera frames; {characters}; campaign lifecycle and duplicate/sealing guards.\n");
            Debug.Log("ARES parity checks passed.");
        }
        static void Require(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
    }
}
