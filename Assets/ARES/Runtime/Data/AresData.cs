using System;
using UnityEngine;

namespace Ares.Data
{
    [Serializable] public sealed class LayoutData
    {
        public RoomData[] rooms; public PathData[] paths, corridors;
        public float solarYaw; public LandmarkData[] landmarks;
        public GardenLayoutData garden;
        public RoomData Room(string id) => Array.Find(rooms, r => r.id == id);
        public Quaternion SolarRotation => Quaternion.Euler(0, -solarYaw * Mathf.Rad2Deg, 0);
        public Vector3 SolarPoint(float x, float y, float z) => Room("solar").Center + SolarRotation * new Vector3(x, y, -(z - 82));
        public Vector3 RoomPoint(string id, float x, float y, float z) => Room(id).Center + new Vector3(x, y, -z);
    }
    [Serializable] public sealed class LandmarkData
    {
        public string id; public float x, z, radius;
        public Vector3 Center => new Vector3(x, 0, -z);
    }
    [Serializable] public sealed class GardenLayoutData { public GardenPoint pickup,refill; public GardenRow[] rows; }
    [Serializable] public sealed class GardenPoint { public float x,z,height; public Vector3 Position=>new Vector3(x,0,-z); }
    [Serializable] public sealed class GardenRow
    {
        public float x,z,width,depth; public int containers;
        public Vector3 Center=>new Vector3(x,0,-z);
        public Vector3 Approach=>Center+Vector3.forward*Mathf.Sign(z)*1.7f;
    }
    [Serializable] public sealed class RoomData
    {
        public string id, shape, color; public float x, z, width, depth, height, stationX, stationZ; public PointData[] ports;
        public Vector3 Center => new Vector3(x, 0, -z);
        public Vector3 Station => new Vector3(stationX, 0, -stationZ);
    }
    [Serializable] public sealed class PointData { public float x, z; }
    [Serializable] public sealed class PathData { public PointData a, b; public float width; }
    [Serializable] public sealed class CampaignData { public SolData[] sols; }
    [Serializable] public sealed class SolData
    {
        public int sol; public string date, capturedAt, alarm, raw; public bool sample, thermalMissing, native;
        public Allocation demand; public Assignment[] tasks;
        public ConsequenceFacts facts = new ConsequenceFacts();
    }
    [Serializable] public struct Allocation
    {
        public int shield, life, greenhouse;
        public int Total => shield + life + greenhouse;
    }
    [Serializable] public sealed class Assignment { public string id, room; }
}
