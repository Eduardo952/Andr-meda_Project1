using Ares.Data;
using Ares.Domain;
using UnityEngine;

namespace Ares.World
{
    public enum StationAction { None, PlanEnergy, OpenTask, SealSol, NeedsPlan, Completed }

    public readonly struct StationOffer
    {
        public readonly RoomData Room;
        public readonly StationAction Action;
        public readonly string TaskId;
        public bool CanUse => Action == StationAction.PlanEnergy || Action == StationAction.OpenTask || Action == StationAction.SealSol;
        public StationOffer(RoomData room, StationAction action, string taskId = null)
        { Room = room; Action = action; TaskId = taskId; }
    }

    /// <summary>Single interaction policy for keyboard, HUD and seal confirmation. Never awards rewards.</summary>
    public sealed class StationInteraction
    {
        readonly LayoutData layout;
        readonly WorldNavigation navigation;
        readonly float radius;
        public StationInteraction(LayoutData layout, float radius)
        { this.layout = layout; navigation = new WorldNavigation(layout); this.radius = radius; }

        public StationOffer Resolve(Campaign campaign, Vector3 position)
        {
            RoomData room = null;
            // A station can be used throughout its interior, not through walls or from an adjacent corridor.
            foreach (var candidate in layout.rooms)
                if (candidate.id != "airlock" && WorldNavigation.Inside(position.x, -position.z, candidate, -.6f))
                { room = candidate; break; }
            if (room == null) room = navigation.Near(position, radius);
            if (room == null || room.id == "airlock" || campaign == null) return default;
            if (campaign.Phase == Phase.Plan)
                return new StationOffer(room, room.id == "control" ? StationAction.PlanEnergy : StationAction.NeedsPlan);
            if (campaign.Phase != Phase.Tasks || campaign.OpenTask != null) return new StationOffer(room, StationAction.None);
            if (room.id == "control")
                return new StationOffer(room, campaign.AllTasksDone ? StationAction.SealSol : StationAction.PlanEnergy);
            foreach (var task in campaign.Current.tasks)
                if (task.room == room.id)
                    return new StationOffer(room, campaign.Completed(task.id) ? StationAction.Completed : StationAction.OpenTask, task.id);
            return new StationOffer(room, StationAction.None);
        }
    }
}
