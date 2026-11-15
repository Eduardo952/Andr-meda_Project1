using System;
using UnityEngine;

namespace Ares.Data
{
    /// <summary>Supports official single-item and feed responses, never substituting another date.</summary>
    public static class ApodResponse
    {
        [Serializable] sealed class Feed { public ApodRecord[] items; }
        public static ApodRecord Parse(string json, string date)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 2000000) return null;
            try
            {
                json = json.TrimStart('\uFEFF', ' ', '\r', '\n');
                var items = json.StartsWith("[", StringComparison.Ordinal)
                    ? JsonUtility.FromJson<Feed>("{\"items\":" + json + "}").items
                    : new[] { JsonUtility.FromJson<ApodRecord>(json) };
                if (items != null) foreach (var item in items) if (item != null && item.ValidFor(date)) return item;
            }
            catch (ArgumentException) { }
            return null;
        }
    }
}
