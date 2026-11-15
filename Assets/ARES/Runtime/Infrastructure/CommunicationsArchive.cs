using System;
using System.Collections.Generic;
using System.IO;
using Ares.Domain;
using Ares.Domain.Communications;
using UnityEngine;

namespace Ares.Infrastructure
{
    /// <summary>Versioned local communication archive. Not a mission checkpoint and never uploads to a server.</summary>
    public sealed class CommunicationsArchive
    {
        [Serializable] public sealed class MessageData { public int id, day, replyTo; public string direction, delivery, body; public bool replied; public float energyKwh; }
        [Serializable] public sealed class ShelfData { public string crop, soil, health; }
        [Serializable] public sealed class ReportData
        {
            public int day, reservoirMl, wateringCanMl, waterUsedMl, dirtyPanels, shieldAllocated, shieldNeeded, radiationGameIndex, integrity, oxygenPercent;
            public float availableEnergyKwh; public bool sent, restEnvironmentStable, scenarioUsesSampleData; public string delivery, shieldStatus;
            public List<ShelfData> shelves = new List<ShelfData>();
            public string demandEvent; public bool shieldRegulated, antennaConnected; public int buriedPanels;
        }
        [Serializable] public sealed class ArchiveData
        {
            public int version = 1; public string missionId; public bool connected; public float reportBatteryKwh;
            public List<MessageData> messages = new List<MessageData>(); public List<ReportData> reports = new List<ReportData>();
        }
        readonly string path, missionId = Guid.NewGuid().ToString("N");
        int revision = -1;
        public string Error { get; private set; }
        public CommunicationsArchive(string directory = null)
        {
            string folder = directory ?? (Application.isEditor ? Path.Combine(Application.dataPath, "../UserSettings/Communications") : Path.Combine(Application.persistentDataPath, "Communications"));
            path = Path.Combine(folder, "mission-" + missionId + ".json");
        }
        public void Save(CommunicationsSystem radio)
        {
            if (radio.Revision == revision) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(Capture(radio, missionId), true));
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                File.Copy(path + ".tmp", path, true); File.Delete(path + ".tmp"); revision = radio.Revision; Error = null;
            }
            catch (IOException) { Error = "Não foi possível salvar o histórico no disco. Ele continua disponível nesta missão."; }
            catch (UnauthorizedAccessException) { Error = "Sem acesso ao disco. O histórico continua disponível nesta missão."; }
        }
        public static ArchiveData Capture(CommunicationsSystem radio, string id)
        {
            var data = new ArchiveData { missionId = id, connected = radio.Connected, reportBatteryKwh = radio.ReportBatteryKwh };
            foreach (var m in radio.Messages) data.messages.Add(new MessageData { id = m.Id, day = m.Day, replyTo = m.ReplyTo, body = m.Body, direction = m.Direction.ToString(), delivery = m.Delivery.ToString(), replied = m.Replied, energyKwh = m.EnergyKwh });
            foreach (var r in radio.Reports)
            {
                var s = r.Snapshot;
                var report = new ReportData { day = s.Day, scenarioUsesSampleData = s.ScenarioUsesSampleData, sent = r.Sent, delivery = r.Delivery.ToString(), reservoirMl = s.ReservoirMl, wateringCanMl = s.WateringCanMl, waterUsedMl = s.WaterUsedMl,
                    availableEnergyKwh = s.AvailableEnergyKwh, dirtyPanels = s.DirtyPanels, shieldAllocated = s.ShieldAllocated, shieldNeeded = s.ShieldNeeded,
                    shieldStatus = s.ShieldStatus, radiationGameIndex = s.RadiationIndex, integrity = s.Integrity, oxygenPercent = s.OxygenPercent, restEnvironmentStable = s.RestEnvironmentStable,
                    demandEvent = s.DemandEvent.ToString(), shieldRegulated = s.ShieldRegulated, buriedPanels = s.BuriedPanels, antennaConnected = s.AntennaConnected };
                foreach (var shelf in s.Shelves) report.shelves.Add(new ShelfData { crop = shelf.Crop, soil = shelf.Soil.ToString(), health = shelf.Health.ToString() });
                data.reports.Add(report);
            }
            return data;
        }
    }
}
