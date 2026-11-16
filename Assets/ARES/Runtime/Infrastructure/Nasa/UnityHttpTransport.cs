using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ares.Infrastructure.Nasa
{
    /// <summary>Every Unity API call stays on the captured Unity main-thread context.</summary>
    public sealed class UnityHttpTransport : IHttpTransport
    {
        readonly SynchronizationContext main;readonly MonoBehaviour runner;
        public UnityHttpTransport(MonoBehaviour runner){this.runner=runner;main=SynchronizationContext.Current??throw new InvalidOperationException("Create transport on Unity main thread");}
        public Task<HttpResponse> SendAsync(HttpRequest request,CancellationToken cancellation)
        {
            var result=new TaskCompletionSource<HttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration=cancellation.Register(()=>result.TrySetCanceled());
            _=result.Task.ContinueWith(_=>registration.Dispose(),TaskScheduler.Default);
            main.Post(_=>{if(runner==null){result.TrySetCanceled();return;}runner.StartCoroutine(Send(request,cancellation,result));},null);
            return result.Task;
        }
        IEnumerator Send(HttpRequest spec,CancellationToken cancellation,TaskCompletionSource<HttpResponse> result)
        {
            if(cancellation.IsCancellationRequested){result.TrySetCanceled();yield break;}
            if(!NasaConfig.Https(spec.Url)){result.TrySetResult(HttpResponse.Error("https_required"));yield break;}
            using(var request=new UnityWebRequest(spec.Url,spec.Method)) {
                request.downloadHandler=new DownloadHandlerBuffer();request.timeout=spec.TimeoutSeconds;request.redirectLimit=0;
                if(spec.Body!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(spec.Body));request.SetRequestHeader("Content-Type","application/json");}
                foreach(var header in spec.Headers)request.SetRequestHeader(header.Key,header.Value);
                var operation=request.SendWebRequest();
                while(!operation.isDone) {
                    if(cancellation.IsCancellationRequested || request.downloadedBytes>(ulong)spec.MaxBytes){request.Abort();break;}
                    yield return null;
                }
                if(cancellation.IsCancellationRequested){result.TrySetCanceled();yield break;}
                var bytes=request.downloadHandler.data;
                if(bytes!=null && bytes.Length>spec.MaxBytes){result.TrySetResult(HttpResponse.Error("response_too_large"));yield break;}
                var response=new HttpResponse {Status=(int)request.responseCode,Data=bytes??Array.Empty<byte>()};
                if(request.result==UnityWebRequest.Result.ConnectionError || request.result==UnityWebRequest.Result.DataProcessingError)response.Failure="connection_or_timeout";
                var headers=request.GetResponseHeaders();if(headers!=null)foreach(var pair in headers)response.Headers[pair.Key]=pair.Value;
                result.TrySetResult(response);
            }
        }
    }
}
