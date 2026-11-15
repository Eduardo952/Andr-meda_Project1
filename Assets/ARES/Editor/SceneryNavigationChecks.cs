using System;
using System.IO;
using Ares.Data;
using Ares.World;
using UnityEngine;

namespace Ares.Editor
{
    /// <summary>Scenery-only checks: source navigation oracle and reachability of every station.</summary>
    public static class SceneryNavigationChecks
    {
        [Serializable] sealed class Cases { public Point[] navigation; }
        [Serializable] sealed class Point { public float x,z;public bool walkable; }
        public static string Run(LayoutData layout)
        {
            var cases=JsonUtility.FromJson<Cases>(File.ReadAllText("Assets/ARES/Import/parity.json"));
            var navigation=new WorldNavigation(layout);
            foreach(var point in cases.navigation)
                if(navigation.Walkable(new Vector3(point.x,0,-point.z))!=point.walkable)
                    throw new InvalidOperationException($"Navigation differs from reference at {point.x}, {point.z}");
            var routes=new ReferenceRoute(layout);
            foreach(var room in layout.rooms)
                if(routes.Between(new Vector3(0,0,-4),room.Station).Count==0)
                    throw new InvalidOperationException("Station is disconnected: "+room.id);
            return $"Navigation: {cases.navigation.Length} source samples match; all {layout.rooms.Length} stations reachable.";
        }
    }
}
