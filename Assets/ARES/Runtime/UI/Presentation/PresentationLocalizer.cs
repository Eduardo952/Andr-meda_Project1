using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Localizes legacy presentation copy, including changing telemetry, without rewriting saved mission data.</summary>
    public static class PresentationLocalizer
    {
        [Serializable] sealed class Pair { public string source,translation; }
        [Serializable] sealed class Pairs { public Pair[] entries; }
        [Serializable] sealed class Entry { public string key,value; }
        [Serializable] sealed class Catalog { public Entry[] entries; }
        sealed class Pattern { public Regex expression; public string target; }
        sealed class State { public string source,rendered,language; public State(){} }
        static readonly ConditionalWeakTable<TextElement,State> states=new ConditionalWeakTable<TextElement,State>();
        static readonly ConditionalWeakTable<VisualElement,State> tips=new ConditionalWeakTable<VisualElement,State>();
        static readonly Dictionary<string,string> english=new Dictionary<string,string>(),portuguese=new Dictionary<string,string>();
        static readonly List<Pattern> toEnglish=new List<Pattern>(),toPortuguese=new List<Pattern>();
        static bool initialized;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCatalog(){initialized=false;english.Clear();portuguese.Clear();toEnglish.Clear();toPortuguese.Clear();}
        public static string Language=>PlayerPrefs.GetString("ARES.Language","pt-BR");
        public static string Brand(string value)=>value?.Replace("CASA MARTE  /  BASE ARES","ARES BASE").Replace("CASA MARTE / ARES","ARES BASE").Replace("CASA MARTE","ARES BASE").Replace("Casa Marte","Ares Base").Replace("casamarte","Ares Base").Replace("Mission Mars","Ares Base");
        static void Initialize()
        {
            if(initialized)return;initialized=true;
            Dictionary<string,string> Read(string language)
            {
                var result=new Dictionary<string,string>();
                foreach(string suffix in new[]{"",".unity",".child"})
                {var asset=Resources.Load<TextAsset>("ARES/UI/"+language+suffix);if(asset!=null)foreach(var e in JsonUtility.FromJson<Catalog>(asset.text).entries)result[e.key]=e.value;}
                return result;
            }
            var pt=Read("pt-BR");var en=Read("en");
            foreach(var entry in pt)if(en.TryGetValue(entry.Key,out var target)&&!entry.Value.Contains("{{"))Add(entry.Value,target);
            var extra=Resources.Load<TextAsset>("ARES/UI/presentation-translations");
            if(extra!=null)foreach(var pair in JsonUtility.FromJson<Pairs>(extra.text).entries)Add(pair.source,pair.translation);
        }
        static void Add(string source,string target)
        {
            source=Brand(source);target=Brand(target);
            if(source.Contains("{0}")){AddPattern(toEnglish,source,target);AddPattern(toPortuguese,target,source);}
            else {english[source]=target;portuguese[target]=source;}
        }
        static void AddPattern(List<Pattern> list,string source,string target)
        {
            string pattern=Regex.Escape(source);
            for(int i=0;i<10;i++)pattern=pattern.Replace(Regex.Escape("{"+i+"}"),"(?<p"+i+">.+?)");
            list.Add(new Pattern{expression=new Regex("^"+pattern+"$",RegexOptions.Singleline|RegexOptions.CultureInvariant),target=target});
        }
        public static string Text(string source)=>Translate(source,Language=="en",0);
        static string Translate(string source,bool en,int depth)
        {
            if(string.IsNullOrEmpty(source))return source;Initialize();source=Brand(source);
            var direct=en?english:portuguese;if(direct.TryGetValue(source,out var result))return result;
            if(depth>3)return source;
            foreach(var pattern in en?toEnglish:toPortuguese)
            {
                var match=pattern.expression.Match(source);if(!match.Success)continue;
                result=pattern.target;
                for(int i=0;i<10;i++)if(match.Groups["p"+i].Success)result=result.Replace("{"+i+"}",Translate(match.Groups["p"+i].Value,en,depth+1));
                return result;
            }
            foreach(string separator in new[]{"\n"," · ",": "})
            {
                if(!source.Contains(separator))continue;
                var pieces=source.Split(new[]{separator},StringSplitOptions.None);
                for(int i=0;i<pieces.Length;i++)pieces[i]=Translate(pieces[i],en,depth+1);
                return string.Join(separator,pieces);
            }
            return source;
        }
        public static void Attach(VisualElement root)
        {
            root.schedule.Execute(()=>Refresh(root)).Every(100);Refresh(root);
        }
        public static void Refresh(VisualElement root)
        {
            string language=Language;
            root.Query<TextElement>().ForEach(element=>
            {
                // Editable journal content belongs to the player, never to the translation catalog.
                if(element.GetFirstAncestorOfType<TextField>()!=null)return;
                var state=states.GetOrCreateValue(element);
                if(element.text==state.rendered&&state.language==language)return;
                if(element.text==state.source&&state.language==language){element.text=state.rendered;return;}
                if(element.text!=state.rendered)state.source=element.text;
                state.rendered=Text(state.source);state.language=language;
                if(element.text!=state.rendered)element.text=state.rendered;
            });
            root.Query<VisualElement>().ForEach(element=>
            {
                if(string.IsNullOrEmpty(element.tooltip))return;
                var state=tips.GetOrCreateValue(element);if(element.tooltip==state.rendered&&state.language==language)return;
                if(element.tooltip==state.source&&state.language==language){element.tooltip=state.rendered;return;}
                if(element.tooltip!=state.rendered)state.source=element.tooltip;
                state.rendered=Text(state.source);state.language=language;element.tooltip=state.rendered;
            });
        }
    }
}
