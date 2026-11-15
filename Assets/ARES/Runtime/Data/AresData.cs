using System;
using UnityEngine;

namespace Ares.Data
{
    [Serializable] public sealed class LayoutData { public RoomData[] rooms; public PathData[] paths, corridors; }
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
        public int sol; public string date, capturedAt, alarm, raw; public bool sample, thermalMissing;
        public Allocation demand; public Assignment[] tasks;
    }
    [Serializable] public struct Allocation
    {
        public int shield, life, greenhouse;
        public int Total => shield + life + greenhouse;
    }
    [Serializable] public sealed class Assignment { public string id, room; }
}
