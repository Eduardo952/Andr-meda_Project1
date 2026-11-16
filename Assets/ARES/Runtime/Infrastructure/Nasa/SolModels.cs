using System;
// These DTOs are serialized by Newtonsoft, never by Unity's scene serializer.
#pragma warning disable UAC1001, UAC1009
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ares.Infrastructure.Nasa
{
    public static class NasaJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double,
            NullValueHandling = NullValueHandling.Include, Culture = System.Globalization.CultureInfo.InvariantCulture
        };
        public static string Write(object value) => JsonConvert.SerializeObject(value, Settings);
        public static T Read<T>(string text) => JsonConvert.DeserializeObject<T>(text, Settings);
        public static JToken Parse(string text) => JsonConvert.DeserializeObject<JToken>(text, Settings);
    }
    [Serializable, JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)] public sealed class DonkiFlare {
        public string classType, peakTime, beginTime;
        [JsonExtensionData] public System.Collections.Generic.IDictionary<string,JToken> extra;
    }
    [Serializable] public sealed class DonkiCme { public JToken speed; }
    [Serializable] public sealed class DonkiImpact { public string location, arrivalTime; public JToken isGlancingBlow; }
    [Serializable] public sealed class DonkiEnlil { public DonkiImpact[] impactList; }
    [Serializable] public sealed class NasaApodDto { public string date, media_type, hdurl, url, copyright; public JToken title, explanation; }
    [Serializable] public sealed class MarsWeatherDto { public JToken sol, min_temp, max_temp, pressure; public string atmo_opacity, terrestrial_date; }
    [Serializable] public sealed class ImageLibraryDto { public ImageCollection collection; }
    [Serializable] public sealed class ImageCollection { public ImageItem[] items; }
    [Serializable] public sealed class ImageItem { public ImageInfo[] data; public ImageLink[] links; }
    [Serializable] public sealed class ImageInfo { public string nasa_id, title, description, photographer, center; }
    [Serializable] public sealed class ImageLink { public string rel, href; }
    [Serializable] public sealed class MissionPlan { public string mission, block_start, block_end, apod_start, apod_end; public int sol, photo_page; }
    [Serializable] public sealed class MissionWindow { public string[] earth_dates; public int[] curiosity_sols; public string reason; }
    [Serializable] public sealed class SelectedWindow {
        public int sol, curiosity_sol; public string mission, earth_date, d_prev, d_next, dust_storm_risk;
        public double ls; public bool dust_season; public MissionWindow mission_window; public DonkiFlare[] flares_day;
    }
    [Serializable] public sealed class FlareFact { public string @class, peak_time, source = "DONKI/FLR", date; public double magnitude; }
    [Serializable] public sealed class CmeFact { public double speed_kms; public string source = "DONKI/CMEAnalysis", date; }
    [Serializable] public sealed class ImpactFact { public string level, arrival_time, source = "DONKI/WSAEnlilSimulations"; public string[] window; }
    [Serializable] public sealed class WeatherFact {
        public double temp_min_c, temp_max_c, temp_avg_c; public double? pressure_pa; public string opacity, source = "Curiosity/REMS", terrestrial_date;
        public int curiosity_sol; public bool is_historical;
    }
    [Serializable] public sealed class SolFacts { public FlareFact flare; public CmeFact cme; public ImpactFact mars_impact; public WeatherFact weather; public object wind; public string severity; }
    [Serializable] public sealed class SolEffects {
        public string rules_version = "effects-v2", comm_disruption, dust_storm_risk;
        public bool radiation_alert, dust_season; public double shield_load_factor, heating_factor, panel_factor, mars_season_ls;
    }
    [Serializable] public sealed class SolApod {
        public string date, title, explanation, media_type = "image", url;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string copyright;
        public bool random_archive_day = true;
    }
    [Serializable] public sealed class SolPhoto { public string id, title, description, credit, url; }
    [Serializable] public sealed class SolDataDates {
        public int sol, curiosity_sol; public string earth_date, flare, cme, weather, apod, note; public string[] mars_impact_window;
    }
    [Serializable] public sealed class SolResponse {
        public int sol; public string mission, earth_date; public MissionWindow mission_window; public SolDataDates data_dates;
        public SolFacts facts; public SolEffects effects; public SolApod apod; public SolPhoto photo;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public bool is_fallback;
    }
}
