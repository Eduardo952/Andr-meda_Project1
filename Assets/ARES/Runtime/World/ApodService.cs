using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Data;
using Ares.Infrastructure;
using Ares.Infrastructure.Nasa;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Native mission/Sol imagery is independent of climate snapshots; local images retain their credits offline.</summary>
    public sealed class ApodService : MonoBehaviour
    {
        readonly ImageLease[] downloaded = new ImageLease[5], photos = new ImageLease[5];
        readonly SolPhoto[] photoInfo = new SolPhoto[5];
        readonly bool[] attempted = new bool[5], loading = new bool[5];
        readonly string[] status = new string[5];
        readonly ApodRecord[] records = new ApodRecord[5];
        CancellationTokenSource mission = new CancellationTokenSource();
        int epoch;
        public event Action Changed;
        public ObservatoryCollection Collection { get; private set; }
        public bool OfflineOnly { get; set; }
        public int RequestCount { get; private set; }
        string collectionJson;
        public const string HistoryKey="ARES.Observatory.ImageHistory.v1";
        [Serializable] sealed class ViewingHistory { public string[] previous=Array.Empty<string>(),seen=Array.Empty<string>(); }
        ViewingHistory history;
        readonly string[] selectedUrls=new string[5];
        string T(string key)=>new Ares.UI.Localization(Ares.UI.Localization.CurrentLanguage).T(key);
        void Awake()
        {
            collectionJson = Resources.Load<TextAsset>("ARES/Observatory/collection").text.TrimStart('\uFEFF');
            Collection = JsonUtility.FromJson<ObservatoryCollection>(collectionJson);
            if (Collection.entries == null || Collection.entries.Length != 5) throw new InvalidOperationException("The observatory requires five curated APOD entries.");
            try{history=JsonUtility.FromJson<ViewingHistory>(PlayerPrefs.GetString(HistoryKey,"{}"));}catch(ArgumentException){}
            history=history??new ViewingHistory();history.previous=history.previous??Array.Empty<string>();history.seen=history.seen??Array.Empty<string>();
        }
        public void BeginMission(int? seed = null)
        {
            epoch++; mission.Cancel(); mission.Dispose(); mission = new CancellationTokenSource();
            for (int i = 0; i < 5; i++) {
                downloaded[i]?.Dispose(); photos[i]?.Dispose(); downloaded[i] = photos[i] = null;
                attempted[i] = loading[i] = false; status[i] = null; records[i] = null; photoInfo[i] = null;
                selectedUrls[i]=null;
            }
            var source = JsonUtility.FromJson<ObservatoryCollection>(collectionJson);
            int Index(string resource)=>Array.FindIndex(source.entries,e=>e.resource==resource);
            var order = seed.HasValue?Ares.Domain.DiscoveryDeck.Shuffle(5,seed.Value):Ares.Domain.DiscoveryDeck.FreshOrder(5,Guid.NewGuid().GetHashCode(),history.previous.Select(Index).ToArray(),history.seen.Select(Index).Where(i=>i>=0).ToArray());
            Collection = JsonUtility.FromJson<ObservatoryCollection>(collectionJson);
            for (int i = 0; i < 5; i++) Collection.entries[i] = source.entries[order[i]];
            if(!seed.HasValue){history.previous=Collection.entries.Select(e=>e.resource).ToArray();SaveHistory();}
            RequestCount = 0; Changed?.Invoke();
        }
        void SaveHistory(){PlayerPrefs.SetString(HistoryKey,JsonUtility.ToJson(history));PlayerPrefs.Save();}
        public void RememberViewed(int day)
        {
            if(day<0||day>=5)return;string key=selectedUrls[day]??Collection.entries[day].resource;
            if(history.seen.LastOrDefault()==key)return;
            var seen=new List<string>(history.seen);seen.Remove(key);seen.Add(key);history.seen=seen.Skip(Math.Max(0,seen.Count-32)).ToArray();SaveHistory();
        }
        public void EndMission(){epoch++;mission.Cancel();for(int i=0;i<5;i++)loading[i]=false;}
        public Texture2D Image(int day) => downloaded[day]?.Texture ?? Resources.Load<Texture2D>(Collection.entries[day].resource);
        public Texture2D ExtraPhoto(int day) => photos[day]?.Texture;
        public SolPhoto ExtraPhotoInfo(int day) => photoInfo[day];
        public string Status(int day) => status[day] ?? T("observatory.local");
        public bool Loading(int day) => loading[day];
        public ApodRecord Record(int day) => records[day];
        public void Load(int day, bool retry = false)
        {
            if (day < 0 || day >= 5 || loading[day] || (attempted[day] && !retry)) return;
            attempted[day] = true;
            if (OfflineOnly) { status[day] = T("observatory.offline"); Changed?.Invoke(); return; }
            _ = Fetch(day, epoch, mission.Token,retry);
        }
        async Task Fetch(int day, int version, CancellationToken cancellation,bool retry)
        {
            loading[day] = true; status[day] = T("observatory.loading"); Changed?.Invoke();
            ImageLease image = null, photo = null;
            try {
                var integration = GetComponent<MissionIntegration>();
                string id = integration.MissionId; RequestCount++;
                var excluded=history.seen.Where(s=>s.StartsWith("https://",StringComparison.Ordinal)).Concat(selectedUrls.Where(s=>s!=null)).TakeLast(16).ToArray();
                var data = await integration.GetImageryAsync(day + 1, cancellation,excluded,retry);
                if (version != epoch || cancellation.IsCancellationRequested) return;
                var apodTask=data.apod==null?Task.FromResult<ImageLease>(null):integration.Images.LoadAsync(id,day+1,data.apod.url,cancellation);
                var photoTask=data.photo==null?Task.FromResult<ImageLease>(null):integration.Images.LoadAsync(id,day+1,data.photo.url,cancellation);
                // Observe and release both tasks even when a mission is cancelled during either download.
                try{await Task.WhenAll(apodTask,photoTask);}finally{if(apodTask.Status==TaskStatus.RanToCompletion)image=apodTask.Result;if(photoTask.Status==TaskStatus.RanToCompletion)photo=photoTask.Result;}
                if (version != epoch || cancellation.IsCancellationRequested) return;
                // Update metadata and texture together. Never relabel the local fallback as a different NASA image.
                if (image != null && !image.IsPlaceholder) {
                    downloaded[day]?.Dispose(); downloaded[day] = image; image = null;
                    selectedUrls[day]=data.apod.url;
                    var a = data.apod;
                    records[day] = new ApodRecord { date = a.date, title = a.title, explanation = a.explanation, media_type = a.media_type, url = a.url, copyright = a.copyright };
                    var entry = Collection.entries[day]; entry.date = a.date; entry.title = ApodRecord.PlainText(a.title);
                    entry.summary = ApodRecord.PlainText(a.explanation); entry.credit = ApodRecord.PlainText(a.copyright ?? "NASA / APOD");
                    entry.sourceUrl = a.url; entry.imageUrl = a.url; entry.licenseUrl = null;
                    entry.question = "Que detalhes você consegue observar nesta imagem?";
                    status[day] = T("observatory.apod")+" · "+a.date;
                } else if(photo!=null && !photo.IsPlaceholder) {
                    downloaded[day]?.Dispose();downloaded[day]=photo;photo=null;records[day]=null;
                    selectedUrls[day]=data.photo.url;
                    var p=data.photo;var entry=Collection.entries[day];entry.date="";entry.title=ApodRecord.PlainText(p.title);entry.summary=ApodRecord.PlainText(p.description);entry.credit=ApodRecord.PlainText(p.credit);entry.sourceUrl="https://images.nasa.gov/details/"+Uri.EscapeDataString(p.id??"");entry.imageUrl=p.url;entry.licenseUrl=null;
                    entry.question=Ares.UI.Localization.CurrentLanguage=="en"?"What details can you observe in this image?":"Que detalhes você consegue observar nesta imagem?";
                    status[day]=new Ares.UI.Localization(Ares.UI.Localization.CurrentLanguage).T("hazard.photoFallback");
                } else status[day] = T("observatory.failed");
                if (photo != null && !photo.IsPlaceholder) {
                    photos[day]?.Dispose(); photos[day] = photo; photo = null; photoInfo[day] = data.photo;
                }
            } catch (OperationCanceledException) { }
            catch { if (version == epoch) status[day] = T("observatory.failed"); }
            finally {
                image?.Dispose(); photo?.Dispose();
                if (version == epoch) { loading[day] = false; Changed?.Invoke(); }
            }
        }
        void OnDestroy()
        {
            epoch++; mission.Cancel(); mission.Dispose();
            foreach (var lease in downloaded) lease?.Dispose();
            foreach (var lease in photos) lease?.Dispose();
        }
    }
}
