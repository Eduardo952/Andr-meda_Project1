using System;
using Ares.Data;
using UnityEngine;

namespace Ares.Tasks
{
    // Read-only snapshot adapter: practice fallbacks must not alter campaign demand.
    public sealed class TaskReadings
    {
        [Serializable] sealed class Snapshot { public Insight insight; public Picture image_library; }
        [Serializable] sealed class Insight { public Wind WD; }
        [Serializable] sealed class Wind { public Direction most_common; }
        [Serializable] sealed class Direction { public string compass_point; }
        [Serializable] sealed class Picture { public string illustration; }
        public float Speed, Temperature, CoilMotionSeconds;
        public int Heating, Tilt, CoilMarks, DustPatches;
        public string WindDirection, PictureKind, Flare;
        public bool CmePractice, TempPractice, WindPractice, FlarePractice;
        public TaskReadings(GameDefinition definition, SolData sol)
        {
            var cme = sol; var temp = sol;
            CmePractice = !sol.facts.hasCmeSpeed; TempPractice = !sol.facts.hasAirTempMean;
            if (CmePractice) foreach (var s in definition.campaign.sols) if (s.facts.hasCmeSpeed) { cme = s; break; }
            if (TempPractice) foreach (var s in definition.campaign.sols) if (s.facts.hasAirTempMean) { temp = s; break; }
            Speed = cme.facts.hasCmeSpeed ? cme.facts.cmeSpeed : 400;
            Temperature = temp.facts.airTempMean;
            Heating = Domain.Rules.Thermal(temp.facts.hasAirTempMean ? (double?)Temperature : null) ?? 25;
            float angle = Mathf.PI * 2 * (definition.rules.startSolFraction - .25f);
            float length = Mathf.Sqrt(1 + .35f * .35f);
            Tilt = Mathf.FloorToInt(Mathf.Atan2(Mathf.Abs(Mathf.Cos(angle) / length), Mathf.Max(.01f, Mathf.Sin(angle) / length)) * Mathf.Rad2Deg / definition.rules.miniGames.tiltStepDegrees + .5f) * definition.rules.miniGames.tiltStepDegrees;
            var snapshot = JsonUtility.FromJson<Snapshot>(sol.raw);
            PictureKind = snapshot?.image_library?.illustration ?? "rover";
            WindDirection = snapshot?.insight?.WD?.most_common?.compass_point;
            WindPractice = WindDirection == null;
            if (WindPractice) foreach (var s in definition.campaign.sols)
            { WindDirection = JsonUtility.FromJson<Snapshot>(s.raw)?.insight?.WD?.most_common?.compass_point; if (WindDirection != null) break; }
            WindDirection = WindDirection ?? "N";
            Flare = sol.facts.flareClass;
            FlarePractice = string.IsNullOrEmpty(Flare);
            if (string.IsNullOrEmpty(Flare)) foreach (var s in definition.campaign.sols) if (!string.IsNullOrEmpty(s.facts.flareClass)) { Flare = s.facts.flareClass; break; }
            int severity = !string.IsNullOrEmpty(sol.alarm) ? 2 : sol.facts.hasCmeSpeed || (!string.IsNullOrEmpty(sol.facts.flareClass) && "MX".Contains(sol.facts.flareClass.Substring(0, 1))) ? 1 : 0;
            CoilMarks = definition.rules.miniGames.coilMarks[severity];
            CoilMotionSeconds = definition.rules.miniGames.coilMotionSeconds[severity];
            DustPatches = definition.rules.miniGames.dustPatches;
            if (definition.rules.marsWeatherModifier && sol.facts.hasWindSpeed)
                DustPatches = Mathf.Min((int)definition.rules.miniGames.weatherDustCap, DustPatches + Mathf.FloorToInt(sol.facts.windSpeed + .5f));
        }
        public static double TravelHoursExact(float speed) => speed > 0 ? 228000000d / speed / 3600 : 0;
        public static int TravelHours(float speed) => speed > 0 ? Mathf.FloorToInt(228000000f / speed / 3600 + .5f) : 0;
    }
}
