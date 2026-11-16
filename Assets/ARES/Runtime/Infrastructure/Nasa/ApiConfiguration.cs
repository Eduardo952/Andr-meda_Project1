using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Infrastructure.Nasa
{
    [Serializable] public sealed class NasaConfig
    {
        public string NasaBaseUrl="https://api.nasa.gov", MarsWeatherBaseUrl="https://api.maas2.apollorion.com", ImageLibraryBaseUrl="https://images-api.nasa.gov";
        public string GroqBaseUrl="https://api.groq.com/openai/v1/chat/completions", NasaProxyBaseUrl="", GroqProxyBaseUrl="";
        public string GroqModel="openai/gpt-oss-120b", formulaVersion="";
        public double GroqTemperature=.7; public int GroqMaxTokens=1500;
        public bool PrivateTrustedBuild=true, UseLegacyN8n=false;
        public int HttpTimeoutSeconds=10, FlareTimeoutSeconds=20, RetryCount=2, CacheFreshSeconds=86400, ImageMemoryLimit=8;
        public static bool Https(string url) => Uri.TryCreate(url,UriKind.Absolute,out var u) && u.Scheme=="https" && string.IsNullOrEmpty(u.UserInfo);
        public static NasaConfig Load(string path) => File.Exists(path)?NasaJson.Read<NasaConfig>(File.ReadAllText(path))??new NasaConfig():new NasaConfig();
    }
    [Serializable] public sealed class ApiCredentials { public string NASA_API_KEY, GROQ_API_KEY; public override string ToString()=>"ApiCredentials [REDACTED]"; }
    public interface ICredentialProvider { Task<ApiCredentials> LoadAsync(CancellationToken cancellation); }
    public sealed class LocalSecretsProvider : ICredentialProvider
    {
        readonly string streamingPath,persistentPath;readonly bool developmentEnvironment;
        public LocalSecretsProvider(string streamingPath,string persistentPath,bool developmentEnvironment=false)
        {this.streamingPath=streamingPath;this.persistentPath=persistentPath;this.developmentEnvironment=developmentEnvironment;}
        public Task<ApiCredentials> LoadAsync(CancellationToken cancellation) => Task.Run(()=> {
            cancellation.ThrowIfCancellationRequested();var credentials=new ApiCredentials();
            // Persistent overrides are useful when rotating keys without rebuilding the private client.
            string path=File.Exists(persistentPath)?persistentPath:streamingPath;
            try {if(File.Exists(path))credentials=NasaJson.Read<ApiCredentials>(File.ReadAllText(path))??credentials;}
            catch(IOException){}catch(UnauthorizedAccessException){}catch(Newtonsoft.Json.JsonException){}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(developmentEnvironment && string.IsNullOrEmpty(credentials.GROQ_API_KEY))credentials.GROQ_API_KEY=Environment.GetEnvironmentVariable("GROQ_API_KEY");
#endif
            return credentials;
        },cancellation);
    }
}
