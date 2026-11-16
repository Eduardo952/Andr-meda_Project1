using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ares.Infrastructure.Nasa
{
    public sealed class ImageLease : IDisposable
    {
        public Texture2D Texture {get;}public bool IsPlaceholder {get;}Action release;
        internal ImageLease(Texture2D texture,bool placeholder,Action release=null){Texture=texture;IsPlaceholder=placeholder;this.release=release;}
        public void Dispose(){Interlocked.Exchange(ref release,null)?.Invoke();}
    }
    /// <summary>Main-thread texture ownership. LRU eviction never destroys a texture still leased by a view.</summary>
    public sealed class ImageLoader : IDisposable
    {
        sealed class Entry {public Texture2D texture;public int users;public long touched;}
        readonly Dictionary<string,Entry> memory=new Dictionary<string,Entry>();readonly Dictionary<string,Task<Entry>> inFlight=new Dictionary<string,Task<Entry>>();
        readonly Dictionary<string,int> waiting=new Dictionary<string,int>();
        readonly string folder;readonly MonoBehaviour runner;readonly SynchronizationContext main;readonly int capacity;readonly Texture2D placeholder;
        readonly CancellationTokenSource lifetime=new CancellationTokenSource();bool disposed;long clock;
        public int NetworkRequests {get;private set;}public int MemoryHits {get;private set;}public int DiskHits {get;private set;}
        public int LiveTextures=>memory.Count;
        public ImageLoader(MonoBehaviour runner,string folder,Texture2D placeholder,int capacity=8)
        {this.runner=runner;this.folder=folder;this.placeholder=placeholder;this.capacity=Math.Max(1,capacity);main=SynchronizationContext.Current??throw new InvalidOperationException("Create ImageLoader on main thread");}
        public async Task<ImageLease> LoadAsync(string mission,int sol,string url,CancellationToken cancellation=default)
        {
            if(disposed || !NasaConfig.Https(url))return new ImageLease(placeholder,true);
            // Identity is persisted separately so a URL hit cannot silently associate the wrong Sol.
            string key=SecretRedactor.Hash(url),binding=SecretRedactor.Hash(mission+"|"+sol+"|"+url);
            if(memory.TryGetValue(key,out var entry)){MemoryHits++;return Lease(key,entry);}
            waiting.TryGetValue(key,out int users);waiting[key]=users+1;
            if(!inFlight.TryGetValue(key,out var task)){task=Fetch(key,url);inFlight[key]=task;_=CleanupFlight(key,task);}
            try {
                entry=await HttpJsonClient.Wait(task,cancellation);
                cancellation.ThrowIfCancellationRequested();
                if(disposed || entry==null)return new ImageLease(placeholder,true);
                await Save(binding+".json",System.Text.Encoding.UTF8.GetBytes(NasaJson.Write(new {mission,sol,urlHash=key})));
                return Lease(key,entry);
            }finally{if(--waiting[key]==0){waiting.Remove(key);if(task.IsCompleted)inFlight.Remove(key);}Trim();}
        }
        async Task CleanupFlight(string key,Task<Entry> task)
        {
            try{await task;}catch{/* LoadAsync owns the result/error. Also observe abandoned requests. */}
            finally {
                if(!waiting.ContainsKey(key) && inFlight.TryGetValue(key,out var current) && ReferenceEquals(current,task))inFlight.Remove(key);
                Trim();
            }
        }
        ImageLease Lease(string key,Entry entry)
        {
            entry.users++;entry.touched=++clock;
            return new ImageLease(entry.texture,false,()=>main.Post(_=>{entry.users=Math.Max(0,entry.users-1);Trim();},null));
        }
        async Task<Entry> Fetch(string key,string url)
        {
            byte[] bytes=await Task.Run(()=>{try{string path=Path.Combine(folder,key+".image");return File.Exists(path)?File.ReadAllBytes(path):null;}catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}});
            Texture2D texture=null;
            if(disposed)return null;
            if(bytes!=null && bytes.Length<=32*1024*1024) {
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(!texture.LoadImage(bytes,true) || !Valid(texture)){UnityEngine.Object.Destroy(texture);texture=null;}else DiskHits++;
            }
            if(texture==null) {
                var completion=new TaskCompletionSource<Texture2D>();
                runner.StartCoroutine(Download(url,key,completion));texture=await completion.Task;
            }
            if(texture==null)return null;
            if(disposed){UnityEngine.Object.Destroy(texture);return null;}
            var entry=new Entry {texture=texture,touched=++clock};memory[key]=entry;return entry;
        }
        IEnumerator Download(string url,string key,TaskCompletionSource<Texture2D> completion)
        {
            using(var registration=lifetime.Token.Register(()=>completion.TrySetCanceled()))
            using(var request=UnityWebRequestTexture.GetTexture(url,true)) {
                request.timeout=10;request.redirectLimit=4;NetworkRequests++;
                var operation=request.SendWebRequest();
                while(!operation.isDone){if(disposed || request.downloadedBytes>32*1024*1024){request.Abort();break;}yield return null;}
                if(disposed || request.result!=UnityWebRequest.Result.Success){completion.TrySetResult(null);yield break;}
                var texture=DownloadHandlerTexture.GetContent(request);
                if(!Valid(texture)){UnityEngine.Object.Destroy(texture);completion.TrySetResult(null);yield break;}
                var saving=Save(key+".image",request.downloadHandler.data);while(!saving.IsCompleted)yield return null;
                if(!completion.TrySetResult(texture))UnityEngine.Object.Destroy(texture);
            }
        }
        static bool Valid(Texture2D texture)=>texture!=null && texture.width>=16 && texture.height>=16 && texture.width<=8192 && texture.height<=8192;
        Task Save(string name,byte[] bytes)=>Task.Run(()=>{try{Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name),bytes);}catch(IOException){}catch(UnauthorizedAccessException){}});
        void Trim()
        {
            foreach(var pair in memory.OrderBy(p=>p.Value.touched).ToArray()) {
                if(!disposed && memory.Count<=capacity)break;if(!disposed && (pair.Value.users>0 || waiting.ContainsKey(pair.Key)))continue;
                UnityEngine.Object.Destroy(pair.Value.texture);memory.Remove(pair.Key);
            }
        }
        public static void Apply(UnityEngine.UIElements.Image target,ImageLease lease){target.image=lease?.Texture;}
        public void Dispose(){disposed=true;lifetime.Cancel();Trim();}
    }
}
