using System;
using System.Collections.Generic;

namespace Ares.Domain.Communications
{
    public enum MessageDirection { FromEarth, ToEarth }
    public enum DeliveryState { InTransit, Delivered }
    public enum ReplyKind { Acknowledge, AskForHelp }

    public sealed class RadioMessage
    {
        public int Id { get; internal set; }
        public int Day { get; internal set; }
        public int ReplyTo { get; internal set; }
        public string Body { get; internal set; }
        public MessageDirection Direction { get; internal set; }
        public DeliveryState Delivery { get; internal set; }
        public bool Replied { get; internal set; }
        public float EnergyKwh { get; internal set; }
        internal float Remaining;
        internal bool RequestsHelp;
    }
    public sealed class MissionReport
    {
        public DailyBaseSnapshot Snapshot { get; internal set; }
        public bool Sent { get; internal set; }
        public DeliveryState Delivery { get; internal set; }
        internal float Remaining;
    }

    /// <summary>Local fictional correspondence, antenna state and immutable daily reports. No network or Unity APIs.</summary>
    public sealed class CommunicationsSystem
    {
        readonly List<RadioMessage> messages = new List<RadioMessage>();
        readonly List<MissionReport> reports = new List<MissionReport>();
        int nextId = 1;
        float repairRemaining;
        public IReadOnlyList<RadioMessage> Messages { get; }
        public IReadOnlyList<MissionReport> Reports { get; }
        public int Day { get; private set; }
        public bool Connected { get; private set; } = true;
        public int RepairStep { get; private set; }
        public bool Repairing => repairRemaining > 0;
        public float RepairProgress => Repairing ? 1 - repairRemaining / CommunicationsRules.RepairStepSeconds : 0;
        public float ReportBatteryKwh { get; private set; } = CommunicationsRules.ReportBatteryKwh;
        public int Revision { get; private set; }
        public CommunicationsSystem()
        { Messages = messages.AsReadOnly(); Reports = reports.AsReadOnly(); BeginDay(1); }
        public void BeginDay(int day)
        {
            if (day != Day + 1 || day > CommunicationsRules.MissionDays) return;
            Day = day;
            QueueEarth(day, CommunicationsRules.Guidance(day)); Revision++;
        }
        public void DamageAntenna()
        { Connected = false; RepairStep = 0; repairRemaining = 0; Revision++; }
        void QueueEarth(int day, string body)
        {
            messages.Add(new RadioMessage { Id = nextId++, Day = day, Body = body, Direction = MessageDirection.FromEarth,
                Delivery = DeliveryState.InTransit, Remaining = CommunicationsRules.OneWayGameSeconds });
        }
        public bool StartRepairStep()
        {
            if (Connected || Repairing) return false;
            repairRemaining = CommunicationsRules.RepairStepSeconds; Revision++; return true;
        }
        public bool CanReply(int id)
        {
            var message = messages.Find(m => m.Id == id);
            return Connected && message != null && message.Direction == MessageDirection.FromEarth && message.Delivery == DeliveryState.Delivered && !message.Replied;
        }
        public static float ReplyCost(ReplyKind kind) => kind == ReplyKind.AskForHelp ? CommunicationsRules.HelpReplyKwh : CommunicationsRules.ShortReplyKwh;
        // Only Campaign charges shared energy and then queues a reply. Consumers cannot bypass the debit.
        internal bool QueueReply(int id, ReplyKind kind)
        {
            if (!Enum.IsDefined(typeof(ReplyKind), kind) || !CanReply(id)) return false;
            messages.Find(m => m.Id == id).Replied = true;
            messages.Add(new RadioMessage { Id = nextId++, ReplyTo = id, Day = Day, Direction = MessageDirection.ToEarth,
                Body = kind == ReplyKind.AskForHelp ? "Preciso de uma orientação para cuidar da base." : "Recebido! Vou cuidar disso.",
                EnergyKwh = ReplyCost(kind), RequestsHelp = kind == ReplyKind.AskForHelp,
                Delivery = DeliveryState.InTransit, Remaining = CommunicationsRules.OneWayGameSeconds });
            Revision++; return true;
        }
        internal void PrepareReport(DailyBaseSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Day != Day || ReportForDay(snapshot.Day) != null) return;
            reports.Add(new MissionReport { Snapshot = snapshot, Delivery = DeliveryState.InTransit }); Revision++;
        }
        public MissionReport ReportForDay(int day) => reports.Find(r => r.Snapshot.Day == day);
        internal bool SendReport(int day)
        {
            var report = ReportForDay(day);
            if (!Connected || report == null || report.Sent || ReportBatteryKwh + .0001f < CommunicationsRules.ReportKwh) return false;
            ReportBatteryKwh = Math.Max(0, ReportBatteryKwh - CommunicationsRules.ReportKwh);
            report.Sent = true; report.Remaining = CommunicationsRules.OneWayGameSeconds; Revision++; return true;
        }
        public void Tick(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
            // Step processing keeps a reply plus the returning guidance from arriving in the same instant.
            while (seconds > 0)
            {
                float step = Math.Min(seconds, .25f); seconds -= step;
                if (Repairing)
                {
                    repairRemaining = Math.Max(0, repairRemaining - step);
                    if (!Repairing) { RepairStep++; if (RepairStep == 3) Connected = true; Revision++; }
                }
                int count = messages.Count;
                for (int i = 0; i < count; i++)
                {
                    var message = messages[i];
                    if (message.Delivery == DeliveryState.Delivered) continue;
                    message.Remaining = Math.Max(0, message.Remaining - step);
                    // Outgoing signals already emitted keep travelling even if the local antenna later fails.
                    if (message.Remaining > 0 || (message.Direction == MessageDirection.FromEarth && !Connected)) continue;
                    message.Delivery = DeliveryState.Delivered; Revision++;
                    if (message.Direction == MessageDirection.ToEarth && message.RequestsHelp) QueueEarth(message.Day, CommunicationsRules.Help(message.Day));
                }
                foreach (var report in reports)
                {
                    if (!report.Sent || report.Delivery == DeliveryState.Delivered) continue;
                    report.Remaining = Math.Max(0, report.Remaining - step);
                    if (report.Remaining <= 0) { report.Delivery = DeliveryState.Delivered; Revision++; }
                }
            }
        }
    }
}
