using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Infrastructure.Nasa
{
    public static class SecretRedactor
    {
        public static string Redact(string value) {
            string s=Regex.Replace(value??"",@"(?i)(api_key=)[^&\s""']+","$1[REDACTED]");
            s=Regex.Replace(s,@"(?im)(Authorization\s*[:=]\s*)[^\r\n]+","$1[REDACTED]");
            return Regex.Replace(s,@"gsk_[A-Za-z0-9_-]+","[REDACTED]");
        }
        public static string Hash(string value) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
    }
    public sealed class HttpRequest
    {
        public string Url, Method="GET", Body;
        public int TimeoutSeconds=10;public int MaxBytes=4*1024*1024;
        public Dictionary<string,string> Headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public override string ToString()=>Method+" "+SecretRedactor.Redact(Url);
        public string Identity { get {
            var headers=new List<string>();foreach(var pair in Headers)headers.Add(pair.Key.ToLowerInvariant()+":"+pair.Value);headers.Sort(StringComparer.Ordinal);
            return SecretRedactor.Hash(Method+"\n"+Url+"\n"+Body+"\n"+string.Join("\n",headers)+"\n"+TimeoutSeconds);
        }}
    }
    public sealed class HttpResponse
    {
        public int Status;public byte[] Data=Array.Empty<byte>();public string Failure;
        public Dictionary<string,string> Headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public bool Success=>Status>=200 && Status<300 && Failure==null;
        public string Text=>Encoding.UTF8.GetString(Data??Array.Empty<byte>());
        public static HttpResponse Error(string kind)=>new HttpResponse {Failure=kind};
    }
    public interface IHttpTransport { Task<HttpResponse> SendAsync(HttpRequest request,CancellationToken cancellation); }
    public sealed class HttpJsonClient : IDisposable
    {
        sealed class Flight { public readonly TaskCompletionSource<HttpResponse> result=new TaskCompletionSource<HttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);public readonly CancellationTokenSource cancel=new CancellationTokenSource();public int users; }
        readonly object gate=new object();readonly Dictionary<string,Flight> flights=new Dictionary<string,Flight>();
        readonly IHttpTransport transport;readonly int retries;readonly Func<TimeSpan,CancellationToken,Task> delay;bool disposed;
        public HttpJsonClient(IHttpTransport transport,int retries=2,Func<TimeSpan,CancellationToken,Task> delay=null){this.transport=transport;this.retries=Math.Max(0,retries);this.delay=delay??Task.Delay;}
        public async Task<HttpResponse> SendAsync(HttpRequest request,CancellationToken cancellation=default)
        {
            cancellation.ThrowIfCancellationRequested();string key=request.Identity;Flight flight;bool start=false;
            lock(gate){if(disposed)throw new ObjectDisposedException(nameof(HttpJsonClient));if(!flights.TryGetValue(key,out flight)){flight=new Flight();flights.Add(key,flight);start=true;}flight.users++;}
            if(start)_=Run(key,flight,request);
            try{return await Wait(flight.result.Task,cancellation).ConfigureAwait(false);}
            finally {lock(gate){flight.users--;if(flight.users==0 && !flight.result.Task.IsCompleted){flights.Remove(key);flight.cancel.Cancel();}}}
        }
        async Task Run(string key,Flight flight,HttpRequest request)
        {
            try {
                HttpResponse response=null;
                for(int attempt=0;attempt<=retries;attempt++) {
                    response=await transport.SendAsync(request,flight.cancel.Token).ConfigureAwait(false);
                    if(attempt==retries || !(response.Status==429 || response.Status>=500 && response.Status<=599))break;
                    await delay(RetryDelay(response,attempt,DateTimeOffset.UtcNow),flight.cancel.Token).ConfigureAwait(false);
                }
                flight.result.TrySetResult(response??HttpResponse.Error("empty_transport"));
            }catch(OperationCanceledException){flight.result.TrySetCanceled();}catch{flight.result.TrySetResult(HttpResponse.Error("transport"));}
            finally{lock(gate){if(flights.TryGetValue(key,out var current) && ReferenceEquals(current,flight))flights.Remove(key);flight.cancel.Dispose();}}
        }
        public static TimeSpan RetryDelay(HttpResponse response,int attempt,DateTimeOffset now)
        {
            if(response.Headers.TryGetValue("Retry-After",out var header)) {
                if(double.TryParse(header,NumberStyles.Float,CultureInfo.InvariantCulture,out var seconds) && seconds>=0 && !double.IsInfinity(seconds))return TimeSpan.FromSeconds(seconds);
                if(DateTimeOffset.TryParse(header,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date))return date>now?date-now:TimeSpan.Zero;
            }
            return TimeSpan.FromSeconds(Math.Pow(2,attempt));
        }
        public static async Task<T> Wait<T>(Task<T> task,CancellationToken cancellation)
        {
            if(!cancellation.CanBeCanceled)return await task.ConfigureAwait(false);
            var cancelled=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(cancellation.Register(()=>cancelled.TrySetResult(true))) {
                if(await Task.WhenAny(task,cancelled.Task).ConfigureAwait(false)!=task)throw new OperationCanceledException(cancellation);
                cancellation.ThrowIfCancellationRequested();return await task.ConfigureAwait(false);
            }
        }
        public void Dispose(){lock(gate){disposed=true;foreach(var flight in flights.Values)flight.cancel.Cancel();flights.Clear();}}
    }
}
