using System;
using System.IO;
using System.Threading.Tasks;
using Ares.Infrastructure.Nasa;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class NasaImageChecks
    {
        static NasaImageChecks() { EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ARES.NasaChecks",false)){SessionState.SetBool("ARES.NasaChecks",false);Run();}}; }
        static void Check(bool value,string label) { if(!value)throw new Exception(label); }
        [MenuItem("ARES/Checks/Native NASA and Groq")]
        public static async void Run()
        {
            if(!EditorApplication.isPlaying){SessionState.SetBool("ARES.NasaChecks",true);EditorApplication.EnterPlaymode();return;}
            string root=Directory.GetCurrentDirectory();
            try {
                string report=await NasaMigrationTests.RunAsync(root);
                await Images();
                Directory.CreateDirectory("Logs");File.WriteAllText("Logs/ares-nasa-play.txt",report+"\nPASS: image memory/disk hits, HTTPS placeholder, live image download, leased texture retention, LRU eviction and disposal.");
                Debug.Log("ARES native NASA/Groq checks passed.");
            }catch(Exception error){Directory.CreateDirectory("Logs");File.WriteAllText("Logs/ares-nasa-play.txt","FAIL: "+SecretRedactor.Redact(error.Message));Debug.LogError("ARES native NASA/Groq check failed; see Logs/ares-nasa-play.txt");}
        }
        // Also used by the isolated Unity PlayMode harness so the user's open scene stays untouched.
        public static async Task Images()
        {
            var runner=new GameObject("Native image cache checks").AddComponent<NasaAsyncRunner>();
            var placeholder=new Texture2D(32,32);placeholder.Apply();
            string folder=Path.Combine(Application.temporaryCachePath,"NasaImageChecks",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            string a="https://example.invalid/fixture-a.png",b="https://example.invalid/fixture-b.png";
            byte[] fixture=placeholder.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(folder,SecretRedactor.Hash(a)+".image"),fixture);File.WriteAllBytes(Path.Combine(folder,SecretRedactor.Hash(b)+".image"),fixture);
            ImageLease one=null,two=null,three=null;
            var cache=new ImageLoader(runner,folder,placeholder,1);
            try {
                one=await cache.LoadAsync("image-test-mission",1,a);Check(!one.IsPlaceholder && cache.DiskHits==1 && cache.NetworkRequests==0,"first load from disk");
                two=await cache.LoadAsync("image-test-mission",1,a);Check(ReferenceEquals(one.Texture,two.Texture) && cache.MemoryHits==1,"second load hits memory");
                using(var invalid=await cache.LoadAsync("image-test-mission",1,"http://unsafe.invalid/image.png"))Check(invalid.IsPlaceholder && invalid.Texture==placeholder,"HTTPS-only placeholder");
                three=await cache.LoadAsync("image-test-mission",2,b);Check(cache.LiveTextures==2 && one.Texture!=null,"leased texture survives LRU pressure");
                Texture2D old=one.Texture;one.Dispose();two.Dispose();one=two=null;
                await Frame();await Frame();Check(cache.LiveTextures==1 && old==null,"released LRU texture destroyed");
                using(var cancellation=new System.Threading.CancellationTokenSource()) {
                    var abandoned=cache.LoadAsync("image-test-mission",1,a,cancellation.Token);cancellation.Cancel();
                    try{using(var ignored=await abandoned){}}catch(OperationCanceledException){}
                    for(int i=0;i<8;i++)await Frame();
                    Check(cache.LiveTextures==1 && three.Texture!=null,"abandoned image request respects LRU and active lease");
                }
                Texture2D last=three.Texture;three.Dispose();three=null;await Frame();cache.Dispose();await Frame();Check(cache.LiveTextures==0 && last==null,"all textures disposed");
                using(var restored=new ImageLoader(runner,folder,placeholder,1))using(var disk=await restored.LoadAsync("image-test-mission",1,a))Check(!disk.IsPlaceholder && restored.DiskHits==1 && restored.NetworkRequests==0,"disk cache survives new loader");
                // A real HTTPS image, publicly available without either credential.
                using(var live=new ImageLoader(runner,folder,placeholder,1)) {
                    using(var image=await live.LoadAsync("image-test-mission",3,"https://images-assets.nasa.gov/image/PIA16716/PIA16716~thumb.jpg")) {
                        Check(!image.IsPlaceholder && live.NetworkRequests==1,"live UnityWebRequestTexture download");
                        using(var hit=await live.LoadAsync("image-test-mission",3,"https://images-assets.nasa.gov/image/PIA16716/PIA16716~thumb.jpg"))Check(hit.Texture==image.Texture && live.NetworkRequests==1,"live image memory hit");
                    }
                }
            }finally{one?.Dispose();two?.Dispose();three?.Dispose();cache.Dispose();UnityEngine.Object.Destroy(placeholder);UnityEngine.Object.Destroy(runner.gameObject);}
        }
        static Task Frame()
        {
            var completion=new TaskCompletionSource<bool>();EditorApplication.delayCall+=()=>completion.TrySetResult(true);return completion.Task;
        }
    }
}
