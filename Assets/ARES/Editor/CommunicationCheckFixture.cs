using System;
using Ares.Domain;

namespace Ares.Editor
{
    /// <summary>Explicit prerequisite for historical campaign fixtures; uses the same report gate as players.</summary>
    internal static class CommunicationCheckFixture
    {
        public static void SendRequiredReport(Campaign campaign)
        {
            if (campaign.Phase != Phase.Summary || campaign.CanAdvanceDay) return;
            var radio = campaign.Communications;
            for (int i = 0; i < 3 && !radio.Connected; i++) { radio.StartRepairStep(); radio.Tick(2); }
            if (!campaign.SendDailyReport()) throw new InvalidOperationException("Fixture could not send required daily report.");
        }
    }
}
