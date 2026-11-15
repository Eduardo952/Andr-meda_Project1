using System;

namespace Ares.Domain
{
    public enum MissionCommandResult { StormStarted, Help, UnknownCommand, Usage, Unavailable, EventActive }
    /// <summary>Small allowlist of game commands. No shell, scripting, reflection or arbitrary evaluation.</summary>
    public static class MissionCommands
    {
        public static MissionCommandResult Execute(Campaign campaign, string text)
        {
            var command = (text ?? "").Trim();
            if (command.Equals("/help", StringComparison.OrdinalIgnoreCase)) return MissionCommandResult.Help;
            var parts = command.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !parts[0].Equals("/event", StringComparison.OrdinalIgnoreCase)) return MissionCommandResult.UnknownCommand;
            if (parts.Length != 2) return MissionCommandResult.Usage;
            var name = parts[1].Trim();
            if (name.StartsWith("(", StringComparison.Ordinal) && name.EndsWith(")", StringComparison.Ordinal)) name = name.Substring(1, name.Length - 2).Trim();
            if (!name.Equals("WindStorm", StringComparison.OrdinalIgnoreCase)) return MissionCommandResult.Usage;
            if (campaign?.Phase != Phase.Tasks) return MissionCommandResult.Unavailable;
            if (campaign.Demands.DustInAir || campaign.Demands.SolarActivity) return MissionCommandResult.EventActive;
            return campaign.TryStartWindStorm() ? MissionCommandResult.StormStarted : MissionCommandResult.Unavailable;
        }
    }
}
