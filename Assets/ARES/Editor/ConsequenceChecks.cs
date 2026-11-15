using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Ares.Data;
using Ares.Domain;
using UnityEngine;

namespace Ares.Editor
{
    public static class ConsequenceChecks
    {
        [Serializable] sealed class Fixtures { public Case[] cases; public CleaningCase[] cleaning; public TierCase[] tiers; public Trace[] traces; }
        [Serializable] sealed class Case
        {
            public int solIndex, currentDose, pool, nextPool;
            public float wind, loss;
            public bool hasWind, cleaned, shelterMissed;
            public float[] panelDust, nextDust;
            public Allocation allocation, demand;
            public ConsequenceFacts facts;
            public DustStorm storm;
            public SolOutcome outcome;
            public ConsequenceEvent[] events;
            public ConsequenceEffects totals;
        }
        [Serializable] sealed class CleaningCase { public float dust; public int passes; }
        [Serializable] sealed class TierCase { public int integrity, oxygen, dose; public string tier; }
        [Serializable] sealed class Trace { public string scenario; public Step[] steps; }
        [Serializable] sealed class Step { public string action, id, room; public float seconds; public Allocation allocation; public State expected; }
        [Serializable] sealed class State
        {
            public string phase, alarm, failureCause;
            public int solIndex, integrity, oxygen, dose, bonusStars, energyPool;
            public float alarmSeconds;
            public float[] panelDust;
            public bool cleanedThisSol, shelterMissed;
            public ConsequenceEvent[] events;
        }
        public static string Run(CampaignData data, CampaignRules rules)
        {
            var f = JsonUtility.FromJson<Fixtures>(File.ReadAllText("Assets/ARES/Import/consequence-parity.json"));
            var engine = new Consequences(rules);
            for (int i = 0; i < f.cases.Length; i++)
            {
                var c = f.cases[i]; string label = "consequence case " + i;
                var storm = engine.StormForSol(c.solIndex, c.hasWind ? (float?)c.wind : null);
                Equal(c.storm, storm, label + " storm");
                var outcome = engine.EvaluateSol(c.allocation, c.demand);
                Equal(c.outcome, outcome, label + " outcome");
                var events = engine.Evaluate(outcome, c.facts, c.shelterMissed, c.currentDose, c.panelDust, storm, c.cleaned);
                Equal(c.events, events, label + " events");
                Equal(c.totals, Consequences.Totals(events), label + " totals");
                var dust = engine.PanelDustAfterCleaning(c.panelDust, storm, c.cleaned);
                Equal(c.nextDust, dust, label + " dust");
                Equal(c.loss, engine.EnergyLossPct(c.panelDust), label + " loss");
                Equal(c.pool, engine.EffectiveEnergyPool(c.panelDust), label + " pool");
                Equal(c.nextPool, engine.EffectiveEnergyPool(dust), label + " next pool");
            }
            foreach (var c in f.cleaning) Equal(c.passes, engine.CleaningPasses(c.dust), "cleaning boundary");
            foreach (var c in f.tiers) Equal(c.tier, engine.SuccessTier(c.integrity, c.oxygen, c.dose), "success tier");
            Equal(0, engine.CleaningPasses(float.NaN), "NaN dust");
            Equal(0, engine.CleaningPasses(float.PositiveInfinity), "infinite dust");
            int steps = 0;
            foreach (var trace in f.traces)
            {
                var campaign = new Campaign(data, rules);
                foreach (var step in trace.steps)
                {
                    switch (step.action)
                    {
                        case "closeReport": campaign.CloseReport(); break;
                        case "allocate": Equal(true, campaign.SetAllocation(step.allocation), "valid allocation"); break;
                        case "startSol": Equal(true, campaign.StartSol(), "start Sol"); break;
                        case "tick": campaign.Tick(step.seconds, false); break;
                        case "shelter": campaign.ReachShelter(); break;
                        case "begin": Equal(true, campaign.BeginTask(step.id, step.room), "begin task"); break;
                        case "finish": Equal(true, GreenhouseCheckFixture.Complete(campaign, step.id), "finish task"); break;
                        case "duplicate": Equal(false, campaign.FinishTask(step.id, true, 10, 0), "duplicate reward"); break;
                        case "seal": Equal(true, campaign.Seal("control", step.allocation), "seal"); break;
                        case "doubleSeal": Equal(false, campaign.Seal("control", step.allocation), "double seal"); break;
                        case "next": campaign.NextSol(); break;
                    }
                    var dust = new float[campaign.PanelDust.Count]; for (int i = 0; i < dust.Length; i++) dust[i] = campaign.PanelDust[i];
                    var events = new ConsequenceEvent[campaign.Events.Count]; for (int i = 0; i < events.Length; i++) events[i] = campaign.Events[i];
                    Equal(step.expected, new State { phase = campaign.Phase.ToString().ToLowerInvariant(), solIndex = campaign.SolIndex,
                        integrity = campaign.Integrity, oxygen = campaign.Oxygen, dose = campaign.Dose, bonusStars = campaign.BonusStars,
                        energyPool = campaign.EnergyPool, panelDust = dust, cleanedThisSol = campaign.CleanedThisSol, shelterMissed = campaign.ShelterMissed,
                        alarm = campaign.Alarm, alarmSeconds = campaign.AlarmSeconds, failureCause = campaign.FailureCause, events = events }, trace.scenario + "/" + steps + "/" + step.action);
                    steps++;
                }
            }
            return $"{f.cases.Length} consequence cases, {f.cleaning.Length} cleaning boundaries, {f.tiers.Length} success tiers, {steps} original campaign transitions";
        }
        static void Equal(object expected, object actual, string path)
        {
            if (expected is string || actual is string) { Require((expected as string ?? "") == (actual as string ?? ""), path + ": " + expected + " != " + actual); return; }
            if (expected == null || actual == null) { Require(expected == actual, path + ": null mismatch"); return; }
            if (expected is float number) { Require(Math.Abs(number - (float)actual) < .0001f, path + ": " + expected + " != " + actual); return; }
            if (expected.GetType().IsPrimitive) { Require(expected.Equals(actual), path + ": " + expected + " != " + actual); return; }
            if (expected is IList list) { var other = (IList)actual; Require(list.Count == other.Count, path + ": length"); for (int i = 0; i < list.Count; i++) Equal(list[i], other[i], path + "[" + i + "]"); return; }
            foreach (var field in expected.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)) Equal(field.GetValue(expected), field.GetValue(actual), path + "." + field.Name);
        }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
