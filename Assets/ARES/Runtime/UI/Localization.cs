using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ares.UI
{
    // Exported verbatim from the reference locales by Tools/AresImport/export-ui.py.
    public sealed class Localization
    {
        public const string DefaultLanguage = "en";
        public static string CurrentLanguage => PlayerPrefs.GetString("ARES.Language", DefaultLanguage);
        [Serializable] sealed class Entry { public string key, value; }
        [Serializable] sealed class Catalog { public Entry[] entries; }
        readonly Dictionary<string, string> entries = new Dictionary<string, string>();
        public string Language { get; private set; }
        public Localization(string language) { Set(language); }
        public void Set(string language)
        {
            Language = language; entries.Clear();
            var asset = Resources.Load<TextAsset>("ARES/UI/" + language);
            if (asset == null) throw new InvalidOperationException("Missing ARES locale: " + language);
            foreach (var entry in JsonUtility.FromJson<Catalog>(asset.text).entries) entries[entry.key] = entry.value;
            // Unity-only features extend the catalog without modifying or being erased by reference exports.
            var additions = Resources.Load<TextAsset>("ARES/UI/" + language + ".unity");
            if (additions != null)
                foreach (var entry in JsonUtility.FromJson<Catalog>(additions.text).entries) entries[entry.key] = entry.value;
            // Reviewed teaching copy is independent of the original reference export.
            var child = Resources.Load<TextAsset>("ARES/UI/" + language + ".child");
            if (child != null)
                foreach (var entry in JsonUtility.FromJson<Catalog>(child.text).entries) entries[entry.key] = entry.value;
            var hazards=Resources.Load<TextAsset>("ARES/UI/"+language+".hazards");
            if(hazards!=null)foreach(var entry in JsonUtility.FromJson<Catalog>(hazards.text).entries)entries[entry.key]=entry.value;
        }
        public string T(string key, params object[] replacements)
        {
            string value = entries.TryGetValue(key, out var found) ? found : key;
            for (int i = 0; i + 1 < replacements.Length; i += 2)
                value = value.Replace("{{" + replacements[i] + "}}", Convert.ToString(replacements[i + 1]));
            return value;
        }
    }
}
