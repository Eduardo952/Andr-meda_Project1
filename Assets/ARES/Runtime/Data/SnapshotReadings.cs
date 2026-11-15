using System;
using UnityEngine;

namespace Ares.Data
{
    /// <summary>Read-only display adapter. Missing values remain missing; no campaign decisions here.</summary>
    public sealed class SnapshotReadings
    {
        [Serializable] sealed class Snapshot { public Donki donki; public Insight insight; }
        [Serializable] sealed class Donki { public Sep[] SEP; public Simulation[] WSAEnlilSimulations; }
        [Serializable] sealed class Sep { public string sepID; }
        [Serializable] sealed class Simulation { public Impact[] impactList; }
        [Serializable] sealed class Impact { public string location; public bool isGlancingBlow; }
        [Serializable] sealed class Insight { public Measurement PRE; public Wind WD; }
        [Serializable] sealed class Measurement { public float av; }
        [Serializable] sealed class Wind { public Direction most_common; }
        [Serializable] sealed class Direction { public string compass_point; }
        public bool SepDetected { get; }
        public bool ImpactKnown { get; }
        public bool MarsImpact { get; }
        public bool Glancing { get; }
        public float? Pressure { get; }
        public string WindDirection { get; }
        public SnapshotReadings(SolData sol)
        {
            var snapshot=JsonUtility.FromJson<Snapshot>(sol.raw);
            SepDetected=snapshot?.donki?.SEP?.Length>0;
            var simulations=snapshot?.donki?.WSAEnlilSimulations;
            ImpactKnown=simulations?.Length>0;
            if(simulations!=null) foreach(var simulation in simulations) if(simulation.impactList!=null) foreach(var impact in simulation.impactList)
                if(string.Equals(impact.location,"Mars",StringComparison.OrdinalIgnoreCase)) { MarsImpact=true; Glancing=impact.isGlancingBlow; }
            Pressure=snapshot?.insight?.PRE?.av;
            WindDirection=snapshot?.insight?.WD?.most_common?.compass_point;
        }
    }
}
